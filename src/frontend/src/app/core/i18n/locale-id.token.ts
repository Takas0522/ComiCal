import { LOCALE_ID, Provider } from '@angular/core';

export const DEFAULT_LOCALE_ID = 'ja-JP';

export function provideLocaleId(localeId: string = DEFAULT_LOCALE_ID): Provider {
  return { provide: LOCALE_ID, useValue: localeId };
}
