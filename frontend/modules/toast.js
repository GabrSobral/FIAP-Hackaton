/**
 * Display a toast notification.
 * The `<app-toast>` component listens for this event on `window`.
 *
 * @param {string} message
 * @param {'success'|'error'|'warning'|'info'} [type='info']
 */
export function showToast(message, type = 'info') {
  window.dispatchEvent(
    new CustomEvent('app-toast', { detail: { message, type } }),
  );
}
