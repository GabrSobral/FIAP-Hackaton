/**
 * API base URL.
 *
 * To change without editing this file, set window.__API_BASE__ before the
 * module loads:
 *   <script>window.__API_BASE__ = 'https://my-backend.example.com';</script>
 */
export const API_BASE =
  (typeof window !== 'undefined' && window.__API_BASE__) || 'http://localhost:8080';
