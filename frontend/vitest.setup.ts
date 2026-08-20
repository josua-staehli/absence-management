/**
 * jsdom implements neither matchMedia nor ResizeObserver. Mantine's color-scheme hook calls the
 * first one while it mounts, so without these stubs every test that renders a component through
 * AppLayout fails before it reaches its assertion.
 */

Object.defineProperty(globalThis, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

globalThis.ResizeObserver = class {
  observe() {}
  unobserve() {}
  disconnect() {}
} as unknown as typeof ResizeObserver;
