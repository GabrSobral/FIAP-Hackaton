import { API_BASE } from '../config.js';

// ─── Error type ──────────────────────────────────────────────────────────────

export class ApiError extends Error {
  /** @param {string} message @param {number} status */
  constructor(message, status) {
    super(message);
    this.name   = 'ApiError';
    this.status = status;
  }
}

// ─── Core fetch wrapper ───────────────────────────────────────────────────────

/**
 * @param {'GET'|'POST'} method
 * @param {string} path
 * @param {BodyInit|null} body
 * @returns {Promise<any>}
 */
async function request(method, path, body = null) {
  const init = { method, headers: { Accept: 'application/json' } };
  if (body) init.body = body;

  const res = await fetch(`${API_BASE}${path}`, init);

  if (!res.ok) {
    let message = `Request failed (HTTP ${res.status})`;
    try {
      const data = await res.json();
      message = data.error ?? data.title ?? message;
    } catch { /* non-JSON error body — keep default message */ }
    throw new ApiError(message, res.status);
  }

  const text = await res.text();
  return text.length ? JSON.parse(text) : null;
}

// ─── Endpoints ───────────────────────────────────────────────────────────────

/**
 * Upload an architecture diagram for AI analysis.
 * @param {File} file
 * @returns {Promise<{ analysisId: string, status: string, createdAt: string }>}
 */
export function createAnalysis(file) {
  const form = new FormData();
  form.append('file', file);
  return request('POST', '/api/v1/analyses', form);
}

/**
 * Poll the lightweight status endpoint.
 * @param {string} id  Analysis UUID
 * @returns {Promise<{ id: string, status: string, updatedAt: string, errorMessage: string|null }>}
 */
export function getAnalysisStatus(id) {
  return request('GET', `/api/v1/analyses/${id}/status`);
}

/**
 * Retrieve the AI-generated report (only available when status === 'Processed').
 * @param {string} id  Analysis UUID
 * @returns {Promise<{ analysisId: string, components: string, risks: string, recommendations: string, generatedAt: string }>}
 */
export function getAnalysisReport(id) {
  return request('GET', `/api/v1/reports/${id}`);
}

/**
 * List all analyses ordered newest-first.
 * @returns {Promise<{ items: Array<{ id: string, fileName: string, contentType: string, status: string, createdAt: string, updatedAt: string, errorMessage: string|null }> }>}
 */
export function listAnalyses() {
  return request('GET', '/api/v1/analyses');
}
