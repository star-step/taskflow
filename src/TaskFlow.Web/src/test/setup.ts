import '@testing-library/jest-dom/vitest';

// jsdom has no matchMedia; components that adapt to screen width need one. Defaults to "not mobile".
if (!window.matchMedia) {
  window.matchMedia = (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  });
}
