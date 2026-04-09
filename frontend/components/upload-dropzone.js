import { LitElement, html, css } from 'https://esm.sh/lit@3';

// ─── Constants ────────────────────────────────────────────────────────────────

const ACCEPTED_TYPES = new Set([
  'image/jpeg',
  'image/png',
  'image/gif',
  'image/webp',
  'application/pdf',
]);

const MAX_BYTES = 10 * 1024 * 1024; // 10 MB

// ─── Helpers ──────────────────────────────────────────────────────────────────

function formatBytes(bytes) {
  if (bytes < 1024)             return `${bytes} B`;
  if (bytes < 1024 * 1024)      return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

// ─── Icons ────────────────────────────────────────────────────────────────────

const iconUpload = html`
  <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" viewBox="0 0 24 24"
       fill="none" stroke="currentColor" stroke-width="1.5"
       stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
    <polyline points="17 8 12 3 7 8"/>
    <line x1="12" y1="3" x2="12" y2="15"/>
  </svg>`;

const iconImage = html`
  <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24"
       fill="none" stroke="currentColor" stroke-width="1.5"
       stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
    <circle cx="8.5" cy="8.5" r="1.5"/>
    <polyline points="21 15 16 10 5 21"/>
  </svg>`;

const iconPdf = html`
  <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24"
       fill="none" stroke="currentColor" stroke-width="1.5"
       stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
    <polyline points="14 2 14 8 20 8"/>
    <line x1="16" y1="13" x2="8" y2="13"/>
    <line x1="16" y1="17" x2="8" y2="17"/>
    <polyline points="10 9 9 9 8 9"/>
  </svg>`;

const iconSpinner = html`
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
       fill="none" stroke="currentColor" stroke-width="2.5"
       stroke-linecap="round" aria-hidden="true" class="spinner-icon">
    <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
  </svg>`;

const iconSend = html`
  <svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24"
       fill="none" stroke="currentColor" stroke-width="2.5"
       stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <path d="m22 2-7 20-4-9-9-4Z"/><path d="M22 2 11 13"/>
  </svg>`;

// ─── Component ────────────────────────────────────────────────────────────────

class UploadDropzone extends LitElement {
  static styles = css`
    :host {
      display: block;
    }

    /* ── Drop zone ──────────────────────────────────────────────────────────── */
    .zone {
      position: relative;
      border: 2px dashed var(--border-strong);
      border-radius: var(--radius-xl);
      background: var(--bg-surface);
      padding: var(--space-10) var(--space-6);
      text-align: center;
      cursor: pointer;
      transition:
        border-color var(--trans-fast),
        background var(--trans-fast),
        box-shadow var(--trans-fast);
      user-select: none;
    }

    .zone:hover,
    .zone.drag-active {
      border-color: var(--accent);
      background: var(--accent-subtle);
      box-shadow: 0 0 0 4px var(--accent-subtle);
    }

    .zone.has-file {
      border-style: solid;
      border-color: var(--border);
      cursor: default;
      padding: var(--space-5) var(--space-6);
    }

    .zone.has-file:hover { border-color: var(--border-strong); }

    :host([uploading]) .zone {
      pointer-events: none;
      opacity: 0.7;
    }

    /* ── Empty state ────────────────────────────────────────────────────────── */
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-3);
    }

    .upload-icon {
      width: 72px;
      height: 72px;
      border-radius: var(--radius-xl);
      background: var(--bg-elevated);
      border: 1px solid var(--border);
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--accent);
      transition: background var(--trans-fast), transform var(--trans-fast);
      margin-bottom: var(--space-1);
    }

    .zone:hover .upload-icon,
    .zone.drag-active .upload-icon {
      background: var(--accent-subtle);
      transform: translateY(-3px);
    }

    .hint-primary {
      font-size: var(--text-base);
      font-weight: 600;
      color: var(--text-primary);
    }

    .hint-secondary {
      font-size: var(--text-sm);
      color: var(--text-muted);
      margin-top: -4px;
    }

    .badge-row {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: var(--space-1);
      margin-top: var(--space-1);
    }

    .badge {
      font-size: var(--text-xs);
      font-weight: 600;
      padding: 2px 8px;
      border-radius: var(--radius-full);
      border: 1px solid var(--border);
      color: var(--text-secondary);
      background: var(--bg-elevated);
      letter-spacing: 0.03em;
    }

    .size-note {
      font-size: var(--text-xs);
      color: var(--text-muted);
      margin-top: var(--space-1);
    }

    /* ── File preview ───────────────────────────────────────────────────────── */
    .preview {
      display: flex;
      align-items: center;
      gap: var(--space-4);
      text-align: left;
    }

    .file-thumb {
      width: 52px;
      height: 52px;
      border-radius: var(--radius-lg);
      background: var(--accent-subtle);
      border: 1px solid var(--accent-border);
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--accent);
      flex-shrink: 0;
    }

    .file-info { flex: 1; overflow: hidden; }

    .file-name {
      font-size: var(--text-sm);
      font-weight: 600;
      color: var(--text-primary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .file-meta {
      font-size: var(--text-xs);
      color: var(--text-muted);
      margin-top: 2px;
    }

    .change-btn {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: var(--text-xs);
      font-weight: 500;
      color: var(--accent);
      background: none;
      border: none;
      cursor: pointer;
      padding: 4px 8px;
      border-radius: var(--radius-sm);
      transition: background var(--trans-fast), color var(--trans-fast);
      flex-shrink: 0;
    }

    .change-btn:hover {
      background: var(--accent-subtle);
    }

    /* ── Actions ────────────────────────────────────────────────────────────── */
    .actions {
      display: flex;
      justify-content: flex-end;
      gap: var(--space-3);
      margin-top: var(--space-4);
    }

    .btn {
      display: inline-flex;
      align-items: center;
      gap: var(--space-2);
      padding: 10px 20px;
      border-radius: var(--radius-lg);
      font-size: var(--text-sm);
      font-weight: 600;
      border: 1px solid transparent;
      cursor: pointer;
      transition:
        background var(--trans-fast),
        border-color var(--trans-fast),
        box-shadow var(--trans-fast),
        transform var(--trans-fast);
    }

    .btn:active:not(:disabled) { transform: scale(0.97); }

    .btn-secondary {
      background: var(--bg-surface);
      border-color: var(--border);
      color: var(--text-secondary);
    }

    .btn-secondary:hover {
      background: var(--bg-elevated);
      border-color: var(--border-strong);
      color: var(--text-primary);
    }

    .btn-primary {
      background: var(--accent);
      color: white;
      box-shadow: 0 1px 3px rgba(59, 130, 246, 0.3);
    }

    .btn-primary:hover:not(:disabled) {
      background: var(--accent-hover);
      box-shadow: 0 4px 12px rgba(59, 130, 246, 0.35);
    }

    .btn-primary:disabled {
      opacity: 0.65;
      cursor: not-allowed;
    }

    /* ── Error ──────────────────────────────────────────────────────────────── */
    .validation-error {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-3) var(--space-4);
      border-radius: var(--radius-md);
      background: var(--error-subtle);
      border: 1px solid var(--error-border);
      color: var(--error-text);
      font-size: var(--text-sm);
      margin-top: var(--space-3);
    }

    /* ── Spinner ────────────────────────────────────────────────────────────── */
    .spinner-icon {
      animation: spin 700ms linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    /* ── Hidden input ───────────────────────────────────────────────────────── */
    input[type="file"] { display: none; }
  `;

  static properties = {
    uploading:     { type: Boolean, reflect: true },
    _file:         { state: true },
    _dragActive:   { state: true },
    _dragCount:    { state: true },
    _validationError: { state: true },
  };

  constructor() {
    super();
    this.uploading          = false;
    this._file              = null;
    this._dragActive        = false;
    this._dragCount         = 0;
    this._validationError   = null;
  }

  render() {
    return html`
      <div
        class="zone ${this._dragActive ? 'drag-active' : ''} ${this._file ? 'has-file' : ''}"
        @click=${this._onZoneClick}
        @dragenter=${this._onDragEnter}
        @dragover=${this._onDragOver}
        @dragleave=${this._onDragLeave}
        @drop=${this._onDrop}
        role="button"
        tabindex="0"
        aria-label="Upload area"
        @keydown=${this._onKeyDown}
      >
        ${this._file ? this._renderPreview() : this._renderEmpty()}
      </div>

      ${this._validationError ? html`
        <div class="validation-error" role="alert">
          <svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
          ${this._validationError}
        </div>
      ` : ''}

      <input
        id="file-input"
        type="file"
        accept="image/jpeg,image/png,image/gif,image/webp,application/pdf"
        @change=${this._onInputChange}
      />

      ${this._file ? html`
        <div class="actions">
          <button class="btn btn-secondary" @click=${this._clear}>
            Clear
          </button>
          <button
            class="btn btn-primary"
            @click=${this._analyze}
            ?disabled=${this.uploading}
          >
            ${this.uploading
              ? html`${iconSpinner} Uploading…`
              : html`${iconSend}   Analyze Diagram`}
          </button>
        </div>
      ` : ''}
    `;
  }

  _renderEmpty() {
    return html`
      <div class="empty">
        <div class="upload-icon">${iconUpload}</div>
        <p class="hint-primary">Drop your diagram here</p>
        <p class="hint-secondary">or click to browse files</p>
        <div class="badge-row">
          ${['JPG', 'PNG', 'GIF', 'WEBP', 'PDF'].map(
            (ext) => html`<span class="badge">${ext}</span>`,
          )}
        </div>
        <p class="size-note">Maximum file size: 10 MB</p>
      </div>
    `;
  }

  _renderPreview() {
    const { name, size, type } = this._file;
    const isPdf = type === 'application/pdf';

    return html`
      <div class="preview">
        <div class="file-thumb">${isPdf ? iconPdf : iconImage}</div>
        <div class="file-info">
          <p class="file-name">${name}</p>
          <p class="file-meta">${type} · ${formatBytes(size)}</p>
        </div>
        <button class="change-btn" @click=${this._openPicker} title="Choose different file">
          Change
        </button>
      </div>
    `;
  }

  // ── Event handlers ──────────────────────────────────────────────────────────

  _onZoneClick() {
    if (!this._file && !this.uploading) this._openPicker();
  }

  _onKeyDown(e) {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      this._onZoneClick();
    }
  }

  _openPicker(e) {
    e?.stopPropagation();
    this.shadowRoot.getElementById('file-input').click();
  }

  _onInputChange(e) {
    const file = e.target.files?.[0];
    if (file) this._setFile(file);
    e.target.value = ''; // reset so same file can be re-selected
  }

  _onDragEnter(e) {
    e.preventDefault();
    this._dragCount++;
    this._dragActive = true;
  }

  _onDragOver(e) {
    e.preventDefault(); // required to allow drop
  }

  _onDragLeave() {
    this._dragCount--;
    if (this._dragCount <= 0) {
      this._dragCount  = 0;
      this._dragActive = false;
    }
  }

  _onDrop(e) {
    e.preventDefault();
    this._dragCount  = 0;
    this._dragActive = false;
    const file = e.dataTransfer?.files?.[0];
    if (file) this._setFile(file);
  }

  _setFile(file) {
    if (!ACCEPTED_TYPES.has(file.type)) {
      this._validationError = `Unsupported type "${file.type}". Please upload JPG, PNG, GIF, WEBP, or PDF.`;
      return;
    }
    if (file.size > MAX_BYTES) {
      this._validationError = `File is too large (${formatBytes(file.size)}). Maximum size is 10 MB.`;
      return;
    }
    this._validationError = null;
    this._file = file;
  }

  _clear(e) {
    e.stopPropagation();
    this._file = null;
    this._validationError = null;
  }

  _analyze(e) {
    e.stopPropagation();
    if (!this._file || this.uploading) return;
    this.dispatchEvent(
      new CustomEvent('analyze', {
        detail:   { file: this._file },
        bubbles:  true,
        composed: true,
      }),
    );
  }
}

customElements.define('upload-dropzone', UploadDropzone);
