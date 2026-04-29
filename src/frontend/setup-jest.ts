import '@angular/localize/init';
import { setupZonelessTestEnv } from 'jest-preset-angular/setup-env/zoneless';

setupZonelessTestEnv();

// jsdom (Jest test env) does not expose `structuredClone`, but the
// IndexedDB API requires it. Pull it from the Node runtime if missing.
if (typeof globalThis.structuredClone === 'undefined') {
  // Node 17+ exposes `structuredClone` on the Node global; jsdom hides it.
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const nodeGlobal = global as any;
  if (typeof nodeGlobal.structuredClone === 'function') {
    globalThis.structuredClone = nodeGlobal.structuredClone;
  } else {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    globalThis.structuredClone = ((value: any) => JSON.parse(JSON.stringify(value))) as typeof structuredClone;
  }
}

