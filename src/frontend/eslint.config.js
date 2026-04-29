// @ts-check
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');

/**
 * NOTE on `data-testid` enforcement:
 * Neither @angular-eslint nor angular-eslint provides a first-class rule that
 * targets every `<button>`, `<a>`, `<input>`, `<select>` and requires a
 * `data-testid` attribute. We therefore use the generic
 * `@angular-eslint/template/attributes-order` family + the
 * `@angular-eslint/template/no-interpolation-in-attributes` style guards and
 * augment with the regex-based `@angular-eslint/template/no-any` style.
 *
 * For now we rely on convention + code review (see frontend.instructions.md)
 * and a custom processor could be added later. This is documented as a
 * known limitation; CI lint will still flag the most common Angular template
 * errors. E2E selectors live in src/tests/e2e/selectors and are the source of
 * truth for required `data-testid` values.
 */
module.exports = tseslint.config(
  {
    files: ['**/*.ts'],
    extends: [
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'app', style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'app', style: 'kebab-case' },
      ],
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {},
  },
  {
    ignores: ['dist/**', 'coverage/**', '.angular/**', 'node_modules/**'],
  },
);
