import { LitElement, html, css } from 'https://esm.sh/lit@3';

// ─── Icons ────────────────────────────────────────────────────────────────────

const icons = {
  success: html`<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>`,
  error:   html`<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="m15 9-6 6M9 9l6 6"/></svg>`,
  warning: html`<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.46 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3"/><path d="M12 9v4M12 17h.01"/></svg>`,
  info:    html`<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/></svg>`,
};

const closeIcon = html`<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M18 6 6 18M6 6l12 12"/></svg>`;

// ─── Component ────────────────────────────────────────────────────────────────

class AppToast extends LitElement {
  static styles = css`
    :host {
      position: fixed;
      bottom: var(--space-6);
      right: var(--space-6);
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      pointer-events: none;
      max-width: min(380px, calc(100vw - 48px));
    }

    .toast {
      display: flex;
      align-items: flex-start;
      gap: var(--space-3);
      padding: var(--space-3) var(--space-4);
      border-radius: var(--radius-lg);
      border: 1px solid;
      background: var(--bg-surface);
      box-shadow: var(--shadow-xl);
      pointer-events: all;
      animation: toast-in 250ms var(--ease) both;
      font-size: var(--text-sm);
      line-height: 1.5;
    }

    @keyframes toast-in {
      from { opacity: 0; transform: translateX(24px) scale(0.96); }
      to   { opacity: 1; transform: translateX(0) scale(1); }
    }

    .toast.leaving {
      animation: toast-out 200ms var(--ease) both;
    }

    @keyframes toast-out {
      to { opacity: 0; transform: translateX(24px) scale(0.96); max-height: 0; padding: 0; margin: 0; overflow: hidden; }
    }

    .toast--success { border-color: var(--success-border); color: var(--success-text); }
    .toast--success .icon { color: var(--success); }

    .toast--error   { border-color: var(--error-border);   color: var(--error-text); }
    .toast--error   .icon { color: var(--error); }

    .toast--warning { border-color: var(--warning-border); color: var(--warning-text); }
    .toast--warning .icon { color: var(--warning); }

    .toast--info    { border-color: var(--info-border);    color: var(--info-text); }
    .toast--info    .icon { color: var(--info); }

    .icon {
      display: flex;
      align-items: center;
      flex-shrink: 0;
      margin-top: 1px;
    }

    .message {
      flex: 1;
      color: var(--text-primary);
    }

    .dismiss {
      display: flex;
      align-items: center;
      background: none;
      border: none;
      padding: 2px;
      border-radius: var(--radius-sm);
      color: var(--text-muted);
      cursor: pointer;
      flex-shrink: 0;
      transition: color var(--trans-fast), background var(--trans-fast);
    }

    .dismiss:hover {
      color: var(--text-primary);
      background: var(--bg-elevated);
    }
  `;

  static properties = {
    _toasts: { state: true },
  };

  constructor() {
    super();
    this._toasts  = [];
    this._counter = 0;
    this._handler = (e) => this._add(e.detail.message, e.detail.type);
  }

  connectedCallback() {
    super.connectedCallback();
    window.addEventListener('app-toast', this._handler);
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.removeEventListener('app-toast', this._handler);
  }

  _add(message, type = 'info') {
    const id = ++this._counter;
    this._toasts = [...this._toasts, { id, message, type }];
    setTimeout(() => this._dismiss(id), 5000);
  }

  _dismiss(id) {
    this._toasts = this._toasts.filter((t) => t.id !== id);
  }

  render() {
    return html`
      ${this._toasts.map(
        (t) => html`
          <div class="toast toast--${t.type}" role="alert" aria-live="polite">
            <span class="icon">${icons[t.type] ?? icons.info}</span>
            <span class="message">${t.message}</span>
            <button class="dismiss" @click=${() => this._dismiss(t.id)} aria-label="Dismiss">
              ${closeIcon}
            </button>
          </div>
        `,
      )}
    `;
  }
}

customElements.define('app-toast', AppToast);
