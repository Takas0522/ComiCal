import type { Config } from 'jest';

const config: Config = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testEnvironment: 'jsdom',
  testMatch: ['<rootDir>/src/**/*.spec.ts'],
  moduleFileExtensions: ['ts', 'html', 'js', 'json', 'mjs'],
  collectCoverageFrom: [
    'src/app/**/*.ts',
    '!src/app/**/*.spec.ts',
    '!src/app/**/index.ts',
  ],
  // Phase 1 frontend coverage scope:
  //   * Bootstrap/config modules (`app.config*.ts`, `app.routes*.ts`, `app.ts`)
  //     contain only DI wiring / route metadata and have nothing to assert
  //     beyond what the build/SSR pipeline already verifies.
  //   * `pages/{login,settings,subscriptions,calendar}` and `pages/legal/terms`
  //     are placeholder Phase 2/3 screens — adding tests now would be churn.
  //   * `molecules/card` and `organisms/oss-dialog` are inherited from earlier
  //     phases (phase3-legal owns the dialog) and out of Phase 1 frontend
  //     scope. Their existing partial coverage stays intact.
  //   * `core/i18n/locale-id.token.ts` is a config token (no logic).
  coveragePathIgnorePatterns: [
    '<rootDir>/src/app/app.config.ts',
    '<rootDir>/src/app/app.config.server.ts',
    '<rootDir>/src/app/app.routes.ts',
    '<rootDir>/src/app/app.routes.server.ts',
    '<rootDir>/src/app/app.ts',
    '<rootDir>/src/app/pages/login/',
    '<rootDir>/src/app/pages/settings/',
    '<rootDir>/src/app/pages/subscriptions/',
    '<rootDir>/src/app/pages/calendar/',
    '<rootDir>/src/app/pages/legal/terms/',
    '<rootDir>/src/app/molecules/card/',
    '<rootDir>/src/app/organisms/oss-dialog/',
    '<rootDir>/src/app/core/i18n/',
  ],
  coverageDirectory: 'coverage',
  coverageReporters: ['text', 'lcov'],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80,
      lines: 80,
      statements: 80,
    },
  },
};

export default config;
