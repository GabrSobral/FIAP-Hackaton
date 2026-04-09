import { LitElement, html, css } from 'https://esm.sh/lit@3';
import { API_BASE } from '../config.js';

// ─── Status badge config ──────────────────────────────────────────────────────

const STATUS_CONFIG = {
  Received:   { label: 'Received',   color: 'var(--info)',    bg: 'var(--info-subtle)',    border: 'var(--info-border)',    text: 'var(--info-text)' },
  Processing: { label: 'Processing', color: 'var(--accent)',  bg: 'var(--accent-subtle)',  border: 'var(--accent-border)',  text: 'var(--accent)' },
  Processed:  { label: 'Complete',   color: 'var(--success)', bg: 'var(--success-subtle)', border: 'var(--success-border)', text: 'var(--success-text)' },
  Error:      { label: 'Error',      color: 'var(--error)',   bg: 'var(--error-subtle)',   border: 'var(--error-border)',   text: 'var(--error-text)' },
};

const TERMINAL = new Set(['Processed', 'Error']);

// ─── Icons ────────────────────────────────────────────────────────────────────

const iconHistory = html`<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/><path d="M12 7v5l4 2"/></svg>`;
const iconEmpty   = html`<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="5" width="20" height="14" rx="2"/><path d="M2 10h20"/></svg>`;

// ─── Component ────────────────────────────────────────────────────────────────

class HistoryPanel extends LitElement {
  static styles = css`
    :host { display: block; }

    /* ── Wrapper card ───────────────────────────────────────────────────────── */
    .card {
      background: var(--bg-surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-xl);
      box-shadow: var(--shadow-sm);
      overflow: hidden;
    }

    /* ── Header ─────────────────────────────────────────────────────────────── */
    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: var(--space-4) var(--space-5);
      border-bottom: 1px solid var(--border);
      background: var(--bg-elevated);
      gap: var(--space-3);
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      color: var(--text-secondary);
      font-size: var(--text-sm);
      font-weight: 600;
    }

    .count-badge {
      background: var(--accent-subtle);
      color: var(--accent);
      border: 1px solid var(--accent-border);
      border-radius: var(--radius-full);
      font-size: var(--text-xs);
      font-weight: 700;
      padding: 1px 8px;
      min-width: 24px;
      text-align: center;
    }

    .live-dot {
      width: 8px;
      height: 8px;
      border-radius: var(--radius-full);
      background: var(--success);
      animation: pulse 2s ease-in-out infinite;
      flex-shrink: 0;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50%       { opacity: 0.3; }
    }

    .live-label {
      font-size: var(--text-xs);
      color: var(--text-muted);
      font-weight: 400;
    }

    /* ── Table ──────────────────────────────────────────────────────────────── */
    .table-wrap {
      overflow-x: auto;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      font-size: var(--text-sm);
    }

    thead {
      background: var(--bg-elevated);
    }

    th {
      padding: var(--space-3) var(--space-4);
      text-align: left;
      font-size: var(--text-xs);
      font-weight: 600;
      color: var(--text-muted);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      white-space: nowrap;
    }

    td {
      padding: var(--space-3) var(--space-4);
      color: var(--text-secondary);
      border-top: 1px solid var(--border);
      vertical-align: middle;
    }

    tr:hover td {
      background: var(--bg-elevated);
    }

    /* ── Filename cell ──────────────────────────────────────────────────────── */
    .filename {
      font-weight: 500;
      color: var(--text-primary);
      max-width: 200px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    /* ── Status badge ───────────────────────────────────────────────────────── */
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      padding: 3px 10px;
      border-radius: var(--radius-full);
      font-size: var(--text-xs);
      font-weight: 600;
      white-space: nowrap;
      border: 1px solid;
    }

    .badge-dot {
      width: 6px;
      height: 6px;
      border-radius: var(--radius-full);
      background: currentColor;
    }

    .badge-dot.spinning {
      animation: spin 800ms linear infinite;
      border-radius: 0;
      background: none;
      border: 2px solid currentColor;
      border-top-color: transparent;
      width: 8px;
      height: 8px;
    }

    @keyframes spin { to { transform: rotate(360deg); } }

    /* ── Timestamp ──────────────────────────────────────────────────────────── */
    .timestamp {
      font-family: var(--font-mono);
      font-size: var(--text-xs);
      color: var(--text-muted);
      white-space: nowrap;
    }

    /* ── ID cell ────────────────────────────────────────────────────────────── */
    .id-cell {
      font-family: var(--font-mono);
      font-size: var(--text-xs);
      color: var(--text-muted);
      max-width: 120px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    /* ── View button ────────────────────────────────────────────────────────── */
    .view-btn {
      display: inline-flex;
      align-items: center;
      padding: 4px 10px;
      border-radius: var(--radius-md);
      font-size: var(--text-xs);
      font-weight: 500;
      background: var(--bg-elevated);
      border: 1px solid var(--border);
      color: var(--text-secondary);
      cursor: pointer;
      transition: background var(--trans-fast), border-color var(--trans-fast), color var(--trans-fast);
      text-decoration: none;
    }

    .view-btn:hover {
      background: var(--bg-inset);
      border-color: var(--border-strong);
      color: var(--text-primary);
    }

    /* ── Empty state ────────────────────────────────────────────────────────── */
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-10) var(--space-6);
      color: var(--text-muted);
      text-align: center;
    }

    .empty p {
      font-size: var(--text-sm);
      margin: 0;
    }
  `;

  static properties = {
    items:       { type: Array },
    _connected:  { state: true },
  };

  constructor() {
    super();
    this.items      = [];
    this._connected = false;
    this._es        = null;
  }

  connectedCallback() {
    super.connectedCallback();
    this._openStream();
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    this._closeStream();
  }

  _openStream() {
    this._es = new EventSource(`${API_BASE}/api/v1/analyses/stream`);
    this._connected = true;

    this._es.onmessage = (e) => {
      try {
        const data = JSON.parse(e.data);
        this.items = data.items ?? [];
      } catch { /* malformed event — ignore */ }
    };

    this._es.onerror = () => {
      this._connected = false;
      // EventSource auto-reconnects; update indicator while disconnected
    };

    this._es.onopen = () => {
      this._connected = true;
    };
  }

  _closeStream() {
    this._es?.close();
    this._es = null;
  }

  _dispatchView(item) {
    this.dispatchEvent(new CustomEvent('history-view', { detail: item, bubbles: true, composed: true }));
  }

  render() {
    return html`
      <div class="card">
        <div class="header">
          <div class="header-left">
            ${iconHistory}
            Analysis History
            <span class="count-badge">${this.items.length}</span>
          </div>
          <div class="header-left">
            <span class="live-dot" style="${this._connected ? '' : 'background: var(--warning); animation: none;'}"></span>
            <span class="live-label">${this._connected ? 'Live' : 'Reconnecting…'}</span>
          </div>
        </div>

        ${this.items.length === 0
          ? html`
            <div class="empty">
              ${iconEmpty}
              <p>No analyses yet. Upload a diagram to get started.</p>
            </div>
          `
          : html`
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>File</th>
                    <th>Status</th>
                    <th>Created</th>
                    <th>Updated</th>
                    <th>ID</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  ${this.items.map(item => this._renderRow(item))}
                </tbody>
              </table>
            </div>
          `}
      </div>
    `;
  }

  _renderRow(item) {
    const cfg       = STATUS_CONFIG[item.status] ?? STATUS_CONFIG['Error'];
    const isActive  = !TERMINAL.has(item.status);
    const canView   = item.status === 'Processed';
    const created   = new Date(item.createdAt).toLocaleString();
    const updated   = new Date(item.updatedAt).toLocaleString();

    return html`
      <tr>
        <td>
          <span class="filename" title="${item.fileName}">${item.fileName}</span>
        </td>
        <td>
          <span class="badge" style="
            background: ${cfg.bg};
            border-color: ${cfg.border};
            color: ${cfg.text};
          ">
            <span class="badge-dot ${isActive ? 'spinning' : ''}"></span>
            ${cfg.label}
          </span>
        </td>
        <td><span class="timestamp">${created}</span></td>
        <td><span class="timestamp">${updated}</span></td>
        <td><span class="id-cell" title="${item.id}">${item.id.slice(0, 8)}…</span></td>
        <td>
          ${canView ? html`
            <button class="view-btn" @click=${() => this._dispatchView(item)}>
              View report
            </button>
          ` : ''}
        </td>
      </tr>
    `;
  }
}

customElements.define('history-panel', HistoryPanel);
