import { TestBed } from '@angular/core/testing';
import { DOCUMENT, PLATFORM_ID } from '@angular/core';
import { ApplicationInsightsService } from './application-insights.service';

const loadAppInsights = jest.fn();
const trackPageView = jest.fn();
const trackEvent = jest.fn();
const trackException = jest.fn();

jest.mock('@microsoft/applicationinsights-web', () => ({
  ApplicationInsights: jest.fn().mockImplementation(() => ({
    loadAppInsights,
    trackPageView,
    trackEvent,
    trackException,
  })),
}));

describe('ApplicationInsightsService', () => {
  beforeEach(() => {
    loadAppInsights.mockReset();
    trackPageView.mockReset();
    trackEvent.mockReset();
    trackException.mockReset();
  });

  function makeService(opts: {
    platform: 'browser' | 'server';
    env?: { aiConnectionString?: string };
  }): ApplicationInsightsService {
    const docStub = {
      defaultView: opts.env ? { __env__: opts.env } : null,
    };
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        ApplicationInsightsService,
        { provide: PLATFORM_ID, useValue: opts.platform },
        { provide: DOCUMENT, useValue: docStub },
      ],
    });
    return TestBed.inject(ApplicationInsightsService);
  }

  it('initialize() is a no-op on the server', () => {
    const svc = makeService({
      platform: 'server',
      env: { aiConnectionString: 'InstrumentationKey=abc;IngestionEndpoint=https://x/' },
    });

    svc.initialize();

    expect(svc.initialized()).toBe(false);
    expect(loadAppInsights).not.toHaveBeenCalled();
  });

  it('initialize() loads the SDK on the browser when a connection string is provided', () => {
    const svc = makeService({
      platform: 'browser',
      env: { aiConnectionString: 'InstrumentationKey=abc;IngestionEndpoint=https://x/' },
    });

    svc.initialize();

    expect(svc.initialized()).toBe(true);
    expect(loadAppInsights).toHaveBeenCalledTimes(1);
    expect(trackPageView).toHaveBeenCalledTimes(1);
  });

  it('initialize() is disabled-but-marked-initialized when no connection string is configured (local dev)', () => {
    const svc = makeService({ platform: 'browser', env: {} });

    svc.initialize();

    expect(svc.initialized()).toBe(true);
    expect(loadAppInsights).not.toHaveBeenCalled();

    // helpers must remain safely callable when uninitialized
    svc.trackEvent('subscription.added', { seriesId: 's-1' });
    svc.trackPageView('home');
    svc.trackException(new Error('boom'));
    expect(trackEvent).not.toHaveBeenCalled();
    expect(trackException).not.toHaveBeenCalled();
  });

  it('initialize() is idempotent', () => {
    const svc = makeService({
      platform: 'browser',
      env: { aiConnectionString: 'InstrumentationKey=abc;IngestionEndpoint=https://x/' },
    });

    svc.initialize();
    svc.initialize();

    expect(loadAppInsights).toHaveBeenCalledTimes(1);
  });

  it('trackEvent forwards to the SDK once initialized on the browser', () => {
    const svc = makeService({
      platform: 'browser',
      env: { aiConnectionString: 'InstrumentationKey=abc;IngestionEndpoint=https://x/' },
    });
    svc.initialize();

    svc.trackEvent('search.performed', { query: 'one piece' });

    expect(trackEvent).toHaveBeenCalledWith({ name: 'search.performed' }, { query: 'one piece' });
  });
});
