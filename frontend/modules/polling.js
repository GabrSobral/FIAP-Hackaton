import { getAnalysisStatus } from './api.js';

/** Statuses that end the polling loop. */
const TERMINAL = new Set(['Processed', 'Error']);

/** Interval between polls in milliseconds. */
const INTERVAL_MS = 2000;

/**
 * Poll `/analyses/{id}/status` every 2 s until a terminal state is reached.
 *
 * @param {string} analysisId
 * @param {(status: { id: string, status: string, errorMessage: string|null }) => void} onUpdate
 *   Called after every successful response, including the final one.
 * @param {AbortSignal} [signal]  Pass an AbortController signal to stop early.
 * @returns {Promise<{ id: string, status: string, errorMessage: string|null }>}
 *   Resolves with the final status response.
 */
export async function pollUntilDone(analysisId, onUpdate, signal) {
  while (true) {
    if (signal?.aborted) throw new DOMException('Polling aborted', 'AbortError');

    const status = await getAnalysisStatus(analysisId);
    onUpdate(status);

    if (TERMINAL.has(status.status)) return status;

    await sleep(INTERVAL_MS, signal);
  }
}

function sleep(ms, signal) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(resolve, ms);

    signal?.addEventListener(
      'abort',
      () => {
        clearTimeout(timer);
        reject(new DOMException('Polling aborted', 'AbortError'));
      },
      { once: true },
    );
  });
}
