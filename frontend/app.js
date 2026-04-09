// ─── Components (registers custom elements) ───────────────────────────────────
import './components/theme-toggle.js';
import './components/upload-dropzone.js';
import './components/status-tracker.js';
import './components/report-panel.js';
import './components/app-toast.js';
import './components/history-panel.js';

// ─── Modules ──────────────────────────────────────────────────────────────────
import { theme }         from './modules/theme.js';
import { createAnalysis, getAnalysisReport } from './modules/api.js';
import { pollUntilDone } from './modules/polling.js';
import { showToast }     from './modules/toast.js';

// ─── Boot ─────────────────────────────────────────────────────────────────────

theme.init();

// ─── DOM refs ─────────────────────────────────────────────────────────────────

const dropzone      = document.getElementById('dropzone');
const tracker       = document.getElementById('tracker');
const reportEl      = document.getElementById('report');
const sectionStatus = document.getElementById('section-status');
const sectionReport = document.getElementById('section-report');
const history       = document.getElementById('history');

// ─── State ────────────────────────────────────────────────────────────────────

/** Cancel an in-flight poll when the user submits a new file. */
let abortCtrl = null;

// ─── History: "View report" handler ──────────────────────────────────────────

history.addEventListener('history-view', async ({ detail: item }) => {
  try {
    const report = await getAnalysisReport(item.id);
    reportEl.report      = report;
    sectionReport.hidden = false;
    await reportEl.updateComplete;
    sectionReport.scrollIntoView({ behavior: 'smooth', block: 'start' });
  } catch (err) {
    showToast(err.message, 'error');
  }
});

// ─── Analyze handler ──────────────────────────────────────────────────────────

dropzone.addEventListener('analyze', async ({ detail: { file } }) => {
  // Abort any previous polling session
  abortCtrl?.abort();
  abortCtrl = new AbortController();

  // Show status panel, hide stale report
  sectionStatus.hidden = false;
  sectionReport.hidden = true;

  // Reset sub-components
  tracker.reset();
  dropzone.uploading = true;

  // Smooth-scroll to status panel
  sectionStatus.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

  try {
    // ── 1. Upload ────────────────────────────────────────────────────────────
    const { analysisId } = await createAnalysis(file);
    dropzone.uploading = false;
    tracker.analysisId = analysisId;

    // ── 2. Poll until terminal state ─────────────────────────────────────────
    const final = await pollUntilDone(
      analysisId,
      ({ status, errorMessage, logs }) => {
        tracker.status       = status;
        tracker.errorMessage = errorMessage;
        if (logs?.length) tracker.logs = logs;
      },
      abortCtrl.signal,
    );

    // ── 3. Handle result ─────────────────────────────────────────────────────
    if (final.status === 'Processed') {
      const report = await getAnalysisReport(analysisId);
      reportEl.report     = report;
      sectionReport.hidden = false;

      await reportEl.updateComplete; // wait for Lit to render
      sectionReport.scrollIntoView({ behavior: 'smooth', block: 'start' });
      showToast('Analysis complete — report is ready!', 'success');
    } else {
      showToast(final.errorMessage ?? 'Analysis failed. Please try again.', 'error');
    }

  } catch (err) {
    if (err.name === 'AbortError') return; // user submitted a new file — expected
    dropzone.uploading = false;
    showToast(err.message, 'error');
  }
});
