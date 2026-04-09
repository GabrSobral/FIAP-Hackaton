import { LitElement, html, css } from 'https://esm.sh/lit@3';
import { theme } from '../modules/theme.js';

// ─── Icons ────────────────────────────────────────────────────────────────────

const iconSun = html`
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
       fill="none" stroke="currentColor" stroke-width="2"
       stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <circle cx="12" cy="12" r="4"/>
    <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41
             M2 12h2M20 12h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41"/>
  </svg>`;

const iconMoon = html`
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
       fill="none" stroke="currentColor" stroke-width="2"
       stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
  </svg>`;

// ─── Component ────────────────────────────────────────────────────────────────

class ThemeToggle extends LitElement {
  static styles = css`
    :host { display: inline-flex; }

    button {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 7px 14px;
      border: 1px solid var(--border);
      border-radius: var(--radius-full);
      background: var(--bg-surface);
      color: var(--text-secondary);
      font-size: var(--text-sm);
      font-weight: 500;
      cursor: pointer;
      transition:
        background var(--trans-fast),
        border-color var(--trans-fast),
        color var(--trans-fast),
        box-shadow var(--trans-fast);
      white-space: nowrap;
      user-select: none;
    }

    button:hover {
      background: var(--bg-elevated);
      border-color: var(--border-strong);
      color: var(--text-primary);
      box-shadow: var(--shadow-sm);
    }

    button:active { transform: scale(0.97); }

    .label {
      display: none;
    }

    @media (min-width: 480px) {
      .label { display: inline; }
    }
  `;

  static properties = {
    _dark: { state: true },
  };

  constructor() {
    super();
    this._dark = false;
    this._onThemeChange = (e) => { this._dark = e.detail === 'dark'; };
  }

  connectedCallback() {
    super.connectedCallback();
    this._dark = theme.get() === 'dark';
    window.addEventListener('theme-changed', this._onThemeChange);
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    window.removeEventListener('theme-changed', this._onThemeChange);
  }

  render() {
    return html`
      <button
        @click=${() => theme.toggle()}
        aria-label=${this._dark ? 'Switch to light mode' : 'Switch to dark mode'}
        title=${this._dark ? 'Switch to light mode' : 'Switch to dark mode'}
      >
        ${this._dark ? iconSun : iconMoon}
        <span class="label">${this._dark ? 'Light mode' : 'Dark mode'}</span>
      </button>
    `;
  }
}

customElements.define('theme-toggle', ThemeToggle);
