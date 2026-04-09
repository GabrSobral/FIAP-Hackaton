const STORAGE_KEY = 'arch-analyzer-theme';
const DARK  = 'dark';
const LIGHT = 'light';

const systemPref = () =>
  window.matchMedia('(prefers-color-scheme: dark)').matches ? DARK : LIGHT;

export const theme = {
  /** @returns {'dark'|'light'} */
  get() {
    return /** @type {'dark'|'light'} */ (localStorage.getItem(STORAGE_KEY) ?? systemPref());
  },

  /** @param {'dark'|'light'} value */
  set(value) {
    localStorage.setItem(STORAGE_KEY, value);
    document.documentElement.dataset.theme = value;
    window.dispatchEvent(new CustomEvent('theme-changed', { detail: value }));
  },

  toggle() {
    this.set(this.get() === DARK ? LIGHT : DARK);
  },

  /**
   * Apply saved/system preference on page load and listen for OS-level changes.
   * Call once in app.js before the first render.
   */
  init() {
    document.documentElement.dataset.theme = this.get();

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
      // Only follow OS changes when the user has not made a manual choice.
      if (!localStorage.getItem(STORAGE_KEY)) {
        this.set(e.matches ? DARK : LIGHT);
      }
    });
  },
};
