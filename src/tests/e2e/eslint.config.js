import playwright from 'eslint-plugin-playwright';
import tsParser from '@typescript-eslint/parser';

export default [
  {
    ...playwright.configs['flat/recommended'],
    files: ['**/*.ts'],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module',
      },
    },
    rules: {
      ...(playwright.configs['flat/recommended'].rules ?? {}),
      // Phase 1 specs intentionally use `test.skip(true, ...)` /
      // `test.fixme(true, ...)` because the SWA + Functions + WireMock
      // stack is not yet wired into CI (see specs/*.spec.ts headers and
      // selectors/_audit.md). Stage Z will flip the skip flag.
      'playwright/no-skipped-test': 'off',
      'playwright/no-conditional-expect': 'off',
      'playwright/no-conditional-in-test': 'off',
      // Skipped specs unavoidably appear to have no assertions; the real
      // assertions live after `test.skip(...)` so this lint signal is
      // noise during Phase 1.
      'playwright/expect-expect': 'off',
    },
  },
  {
    ignores: ['node_modules/**', 'test-results/**', 'playwright-report/**'],
  },
];
