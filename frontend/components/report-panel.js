import { LitElement, html, css } from 'https://esm.sh/lit@3';

// ─── Card definitions ─────────────────────────────────────────────────────────

const CARDS = [
  {
    key:    'components',
    title:  'Components',
    desc:   'Identified services, databases, queues, and their relationships',
    color:  'var(--accent)',
    bg:     'var(--accent-subtle)',
    border: 'var(--accent-border)',
    icon: html`<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="2" width="9" height="9" rx="1"/><rect x="13" y="2" width="9" height="9" rx="1"/><rect x="2" y="13" width="9" height="9" rx="1"/><path d="M17.5 17.5 22 22M13 17.5h4.5v4.5"/></svg>`,
  },
  {
    key:    'risks',
    title:  'Risks',
    desc:   'Architectural risks, single points of failure, and security concerns',
    color:  'var(--error)',
    bg:     'var(--error-subtle)',
    border: 'var(--error-border)',
    icon: html`<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.46 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3"/><path d="M12 9v4"/><path d="M12 17h.01"/></svg>`,
  },
  {
    key:    'recommendations',
    title:  'Recommendations',
    desc:   'Concrete improvements, best practices, and architectural guidance',
    color:  'var(--success)',
    bg:     'var(--success-subtle)',
    border: 'var(--success-border)',
    icon: html`<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"/></svg>`,
  },
  {
    key:    'feedback',
    title:  'AI Feedback',
    desc:   'Overall expert assessment of architecture quality and maturity',
    color:  'var(--warning)',
    bg:     'var(--warning-subtle)',
    border: 'var(--warning-border)',
    icon: html`<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>`,
  },
];

const iconCopy = html`<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>`;
const iconCheck = html`<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>`;

// ─── Component ────────────────────────────────────────────────────────────────

class ReportPanel extends LitElement {
  static styles = css`
    :host {
      display: block;
    }

    /* ── Grid ───────────────────────────────────────────────────────────────── */
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: var(--space-5);
    }

    /* ── Card ───────────────────────────────────────────────────────────────── */
    .card {
      background: var(--bg-surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-xl);
      overflow: hidden;
      box-shadow: var(--shadow-sm);
      display: flex;
      flex-direction: column;
      transition: box-shadow var(--trans-base), transform var(--trans-base);
    }

    .card:hover {
      box-shadow: var(--shadow-md);
      transform: translateY(-2px);
    }

    .card-header {
      display: flex;
      align-items: flex-start;
      gap: var(--space-3);
      padding: var(--space-5) var(--space-5) var(--space-4);
      border-bottom: 1px solid var(--border);
    }

    .card-icon {
      width: 40px;
      height: 40px;
      border-radius: var(--radius-lg);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .card-titles { flex: 1; min-width: 0; }

    .card-title {
      font-size: var(--text-base);
      font-weight: 700;
      letter-spacing: -0.01em;
      color: var(--text-primary);
      line-height: 1.3;
    }

    .card-desc {
      font-size: var(--text-xs);
      color: var(--text-muted);
      margin-top: 2px;
      line-height: 1.4;
    }

    /* ── Copy button ────────────────────────────────────────────────────────── */
    .copy-btn {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      background: var(--bg-elevated);
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      padding: 5px 10px;
      font-size: var(--text-xs);
      font-weight: 500;
      color: var(--text-muted);
      cursor: pointer;
      transition:
        background var(--trans-fast),
        border-color var(--trans-fast),
        color var(--trans-fast);
      flex-shrink: 0;
      align-self: flex-start;
    }

    .copy-btn:hover {
      background: var(--bg-inset);
      border-color: var(--border-strong);
      color: var(--text-primary);
    }

    .copy-btn.copied {
      color: var(--success);
      border-color: var(--success-border);
      background: var(--success-subtle);
    }

    /* ── Body ───────────────────────────────────────────────────────────────── */
    .card-body {
      padding: var(--space-5);
      flex: 1;
      overflow: auto;
    }

    .card-content {
      font-size: var(--text-sm);
      color: var(--text-secondary);
      line-height: 1.75;
      white-space: pre-wrap;
      word-break: break-word;
    }

    /* ── Footer ─────────────────────────────────────────────────────────────── */
    .panel-footer {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: var(--space-2);
      padding-top: var(--space-2);
      color: var(--text-muted);
      font-size: var(--text-xs);
    }

    .panel-footer svg { flex-shrink: 0; }

    @media (max-width: 640px) {
      .grid { grid-template-columns: 1fr; }
    }
  `;

  static properties = {
    report:        { type: Object },
    _copiedKey:    { state: true },
  };

  constructor() {
    super();
    this.report     = null;
    this._copiedKey = null;
  }

  render() {
    if (!this.report) return html``;

    const { components, risks, recommendations, generatedAt } = this.report;
    const generated = new Date(generatedAt);

    return html`
      <div class="grid">
        ${CARDS
          .filter((card) => card.key !== 'feedback' || this.report.feedback)
          .map((card) => this._renderCard(card, this.report[card.key] ?? ''))}
      </div>

      <div class="panel-footer">
        <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
        Report generated ${generated.toLocaleString()}
      </div>
    `;
  }

  _renderCard(card, content) {
    const copied = this._copiedKey === card.key;

    return html`
      <div class="card">
        <div class="card-header">
          <div class="card-icon" style="background:${card.bg};color:${card.color};border:1px solid ${card.border}">
            ${card.icon}
          </div>
          <div class="card-titles">
            <h3 class="card-title">${card.title}</h3>
            <p class="card-desc">${card.desc}</p>
          </div>
          <button
            class="copy-btn ${copied ? 'copied' : ''}"
            @click=${() => this._copy(card.key, content)}
            title="Copy ${card.title}"
          >
            ${copied ? iconCheck : iconCopy}
            ${copied ? 'Copied' : 'Copy'}
          </button>
        </div>

        <div class="card-body">
          <p class="card-content">${content}</p>
        </div>
      </div>
    `;
  }

  async _copy(key, text) {
    try {
      await navigator.clipboard.writeText(text);
      this._copiedKey = key;
      window.dispatchEvent(
        new CustomEvent('app-toast', { detail: { message: 'Copied to clipboard!', type: 'success' } }),
      );
      setTimeout(() => {
        if (this._copiedKey === key) this._copiedKey = null;
      }, 2000);
    } catch {
      window.dispatchEvent(
        new CustomEvent('app-toast', { detail: { message: 'Failed to copy.', type: 'error' } }),
      );
    }
  }
}

customElements.define('report-panel', ReportPanel);
