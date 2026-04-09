import { LitElement, html, css } from 'https://esm.sh/lit@3';

// ─── Step definitions ─────────────────────────────────────────────────────────

/** Ordered progress steps (Error shares the slot of Processing). */
const STEPS = [
  { key: 'Received',   label: 'Received',   desc: 'Diagram uploaded and queued' },
  { key: 'Processing', label: 'Processing', desc: 'AI model is analysing the diagram' },
  { key: 'Processed',  label: 'Complete',   desc: 'Report is ready' },
];

const STATUS_INDEX = { Received: 0, Processing: 1, Processed: 2, Error: 2 };

// ─── Icons ────────────────────────────────────────────────────────────────────

const iconCheck = html`<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>`;
const iconX     = html`<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><path d="M18 6 6 18M6 6l12 12"/></svg>`;
const iconCopy  = html`<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>`;

// ─── Component ────────────────────────────────────────────────────────────────

class StatusTracker extends LitElement {
  static styles = css`
    :host {
      display: block;
    }

    .card {
      background: var(--bg-surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-xl);
      padding: var(--space-6);
      box-shadow: var(--shadow-sm);
      display: flex;
      flex-direction: column;
      gap: var(--space-5);
    }

    /* ── Step track ─────────────────────────────────────────────────────────── */
    .steps {
      display: flex;
      align-items: flex-start;
    }

    .step {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-2);
      flex: 1;
      position: relative;
    }

    /* connector line between steps */
    .step:not(:last-child)::after {
      content: '';
      position: absolute;
      top: 17px;
      left: calc(50% + 17px);
      right: calc(-50% + 17px);
      height: 2px;
      background: var(--border);
      transition: background var(--trans-slow);
    }

    .step.done:not(:last-child)::after,
    .step.active:not(:last-child)::after {
      background: var(--accent);
    }

    .step.error:not(:last-child)::after {
      background: var(--error);
    }

    /* dot */
    .dot {
      width: 34px;
      height: 34px;
      border-radius: var(--radius-full);
      border: 2px solid var(--border);
      background: var(--bg-surface);
      display: flex;
      align-items: center;
      justify-content: center;
      transition:
        border-color var(--trans-base),
        background var(--trans-base),
        box-shadow var(--trans-base);
      position: relative;
      z-index: 1;
      color: var(--text-muted);
    }

    .step.active .dot {
      border-color: var(--accent);
      box-shadow: 0 0 0 4px var(--accent-subtle);
      color: var(--accent);
    }

    .step.done .dot {
      border-color: var(--success);
      background: var(--success);
      color: white;
      box-shadow: none;
    }

    .step.error .dot {
      border-color: var(--error);
      background: var(--error);
      color: white;
    }

    /* spinning indicator for the active step */
    .spinner {
      width: 16px;
      height: 16px;
      border: 2.5px solid var(--accent-border);
      border-top-color: var(--accent);
      border-radius: var(--radius-full);
      animation: spin 600ms linear infinite;
    }

    @keyframes spin { to { transform: rotate(360deg); } }

    .step-label {
      font-size: var(--text-xs);
      font-weight: 600;
      color: var(--text-muted);
      text-align: center;
      transition: color var(--trans-base);
    }

    .step.active .step-label { color: var(--accent); }
    .step.done   .step-label { color: var(--success); }
    .step.error  .step-label { color: var(--error); }

    .step-desc {
      font-size: var(--text-xs);
      color: var(--text-muted);
      text-align: center;
      line-height: 1.4;
    }

    /* ── Meta row ───────────────────────────────────────────────────────────── */
    .meta {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      background: var(--bg-elevated);
      border-radius: var(--radius-md);
      flex-wrap: wrap;
    }

    .meta-label {
      font-size: var(--text-xs);
      font-weight: 500;
      color: var(--text-muted);
      white-space: nowrap;
    }

    .meta-id {
      font-family: var(--font-mono);
      font-size: var(--text-xs);
      color: var(--text-secondary);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      flex: 1;
      text-align: right;
    }

    .copy-btn {
      display: inline-flex;
      align-items: center;
      background: none;
      border: none;
      padding: 3px;
      border-radius: var(--radius-sm);
      color: var(--text-muted);
      cursor: pointer;
      transition: color var(--trans-fast), background var(--trans-fast);
      flex-shrink: 0;
    }

    .copy-btn:hover {
      color: var(--text-primary);
      background: var(--bg-inset);
    }

    /* ── Error box ──────────────────────────────────────────────────────────── */
    .error-box {
      display: flex;
      align-items: flex-start;
      gap: var(--space-3);
      padding: var(--space-4);
      background: var(--error-subtle);
      border: 1px solid var(--error-border);
      border-radius: var(--radius-md);
      color: var(--error-text);
      font-size: var(--text-sm);
      line-height: 1.5;
    }

    .error-box svg { flex-shrink: 0; margin-top: 1px; }

    /* ── Activity log ───────────────────────────────────────────────────────── */
    .log {
      background: var(--bg-elevated);
      border-radius: var(--radius-md);
      padding: var(--space-4);
      overflow-y: auto;
      max-height: 220px;
    }

    .log-title {
      font-size: var(--text-xs);
      font-weight: 600;
      color: var(--text-muted);
      margin-bottom: var(--space-3);
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .log-entry {
      display: flex;
      align-items: baseline;
      gap: var(--space-2);
      padding: var(--space-1) 0;
      font-size: var(--text-xs);
      border-bottom: 1px solid var(--border);
      line-height: 1.4;
    }

    .log-entry:last-child { border-bottom: none; }

    .log-time {
      font-family: var(--font-mono);
      color: var(--text-muted);
      white-space: nowrap;
      flex-shrink: 0;
    }

    .log-level {
      font-weight: 700;
      font-size: 10px;
      text-transform: uppercase;
      padding: 1px 5px;
      border-radius: var(--radius-sm);
      flex-shrink: 0;
    }

    .log-level.info  { background: var(--accent-subtle); color: var(--accent); }
    .log-level.warn  { background: #fef3c7; color: #92400e; }
    .log-level.error { background: var(--error-subtle); color: var(--error-text); }

    .log-msg {
      color: var(--text-secondary);
      word-break: break-word;
    }
  `;

  static properties = {
    status:       { type: String },
    analysisId:   { type: String },
    errorMessage: { type: String },
    logs:         { type: Array },
  };

  constructor() {
    super();
    this.status       = null;
    this.analysisId   = null;
    this.errorMessage = null;
    this.logs         = [];
  }

  /** Reset to initial state (call before a new upload). */
  reset() {
    this.status       = null;
    this.analysisId   = null;
    this.errorMessage = null;
    this.logs         = [];
  }

  render() {
    const currentIdx = this.status ? STATUS_INDEX[this.status] ?? 0 : -1;
    const isActive   = currentIdx >= 0;
    const isError    = this.status === 'Error';

    return html`
      <div class="card">

        <!-- Step track — hidden until a status is set -->
        <div class="steps" ?hidden=${!isActive}>
          ${STEPS.map((step, i) => {
            const done   = i < currentIdx || (i === currentIdx && this.status === 'Processed');
            const active = i === currentIdx && !done;
            const error  = isError && i === currentIdx;

            return html`
              <div class="step ${done ? 'done' : ''} ${active && !error ? 'active' : ''} ${error ? 'error' : ''}">
                <div class="dot">
                  ${done
                    ? iconCheck
                    : error
                    ? iconX
                    : active
                    ? html`<div class="spinner"></div>`
                    : html`<span style="width:8px;height:8px;border-radius:50%;background:var(--border)"></span>`}
                </div>
                <span class="step-label">${step.label}</span>
                <span class="step-desc">${step.desc}</span>
              </div>
            `;
          })}
        </div>

        <!-- Analysis ID row -->
        ${this.analysisId ? html`
          <div class="meta">
            <span class="meta-label">Analysis ID</span>
            <span class="meta-id" title=${this.analysisId}>${this.analysisId}</span>
            <button class="copy-btn" @click=${this._copyId} title="Copy ID">${iconCopy}</button>
          </div>
        ` : ''}

        <!-- Error message -->
        ${isError && this.errorMessage ? html`
          <div class="error-box" role="alert">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
            ${this.errorMessage}
          </div>
        ` : ''}

        <!-- Activity log -->
        ${this.logs?.length ? html`
          <div class="log">
            <div class="log-title">Activity log</div>
            ${this.logs.map(log => {
              const time = new Date(log.timestamp).toLocaleTimeString();
              const level = log.level.toLowerCase();
              return html`
                <div class="log-entry">
                  <span class="log-time">${time}</span>
                  <span class="log-level ${level}">${level}</span>
                  <span class="log-msg">${log.message}</span>
                </div>
              `;
            })}
          </div>
        ` : ''}

      </div>
    `;
  }

  async _copyId() {
    if (!this.analysisId) return;
    try {
      await navigator.clipboard.writeText(this.analysisId);
      // Dispatch a toast via the event bus
      window.dispatchEvent(
        new CustomEvent('app-toast', { detail: { message: 'Analysis ID copied!', type: 'success' } }),
      );
    } catch {
      /* clipboard not available */
    }
  }
}

customElements.define('status-tracker', StatusTracker);
