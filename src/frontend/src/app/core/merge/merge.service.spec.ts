import 'fake-indexeddb/auto';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';

import { MergeService, type MergeResult } from './merge.service';
import {
  AnonymousPurchaseRepository,
  AnonymousSubscriptionRepository,
} from '../anonymous-store';
import { __resetComiCalDbForTests } from '../anonymous-store/db';
import { AuthService } from '../auth/auth.service';

class FakeAuthService {
  private _authed = false;
  isAuthenticated = (): boolean => this._authed;
  setAuthenticated(v: boolean): void {
    this._authed = v;
  }
}

describe('MergeService', () => {
  let svc: MergeService;
  let httpMock: HttpTestingController;
  let subs: AnonymousSubscriptionRepository;
  let purchases: AnonymousPurchaseRepository;
  let auth: FakeAuthService;

  beforeEach(async () => {
    await __resetComiCalDbForTests();
    window.localStorage.removeItem('merge.snoozedUntil');

    auth = new FakeAuthService();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    svc = TestBed.inject(MergeService);
    subs = TestBed.inject(AnonymousSubscriptionRepository);
    purchases = TestBed.inject(AnonymousPurchaseRepository);
    // Ensure stores are hydrated, then clear residue from previous tests.
    await subs.list();
    await purchases.list();
    await subs.clear();
    await purchases.clear();
  });

  afterEach(async () => {
    httpMock.verify();
    await __resetComiCalDbForTests();
  });

  it('getPendingCount reflects underlying store counts', async () => {
    expect(svc.getPendingCount()).toEqual({ subscriptions: 0, purchases: 0 });
    await subs.add('11111111-1111-1111-1111-111111111111');
    await purchases.upsert({
      volumeId: '22222222-2222-2222-2222-222222222222',
      seriesId: '11111111-1111-1111-1111-111111111111',
      isbn13: '9784088100005',
      state: 'bought',
      updatedAt: new Date().toISOString(),
    });
    expect(svc.getPendingCount()).toEqual({ subscriptions: 1, purchases: 1 });
  });

  it('shouldPrompt returns false when not authenticated', () => {
    auth.setAuthenticated(false);
    expect(svc.shouldPrompt()).toBe(false);
  });

  it('shouldPrompt returns false when authenticated but pending=0', () => {
    auth.setAuthenticated(true);
    expect(svc.shouldPrompt()).toBe(false);
  });

  it('shouldPrompt returns true when authenticated and has pending', async () => {
    auth.setAuthenticated(true);
    await subs.add('11111111-1111-1111-1111-111111111111');
    expect(svc.shouldPrompt()).toBe(true);
  });

  it('snooze() suppresses subsequent shouldPrompt() for 24h', async () => {
    auth.setAuthenticated(true);
    await subs.add('11111111-1111-1111-1111-111111111111');
    expect(svc.shouldPrompt()).toBe(true);
    svc.snooze();
    expect(svc.shouldPrompt()).toBe(false);
    // Manually expire the snooze.
    window.localStorage.setItem('merge.snoozedUntil', String(Date.now() - 1));
    expect(svc.shouldPrompt()).toBe(true);
  });

  it('merge() POSTs payload, clears local store on 200, returns result', async () => {
    auth.setAuthenticated(true);
    await subs.add('11111111-1111-1111-1111-111111111111');
    await purchases.upsert({
      volumeId: '22222222-2222-2222-2222-222222222222',
      seriesId: '11111111-1111-1111-1111-111111111111',
      isbn13: '9784088100005',
      state: 'bought',
      purchasedAt: '2026-04-01T00:00:00.000Z',
      updatedAt: new Date().toISOString(),
    });

    const promise = firstValueFrom(svc.merge());
    // collectPayload is async — flush microtasks so the http POST fires.
    await new Promise<void>((r) => setTimeout(r, 0));
    const req = httpMock.expectOne('/api/me/sync/merge');
    expect(req.request.method).toBe('POST');
    const body = req.request.body as {
      subscriptions: { seriesId: string }[];
      purchases: { volumeId: string; purchasedAt: string | null }[];
    };
    expect(body.subscriptions).toHaveLength(1);
    expect(body.subscriptions[0].seriesId).toBe(
      '11111111-1111-1111-1111-111111111111',
    );
    expect(body.purchases).toHaveLength(1);
    expect(body.purchases[0].volumeId).toBe(
      '22222222-2222-2222-2222-222222222222',
    );
    expect(body.purchases[0].purchasedAt).toBe('2026-04-01T00:00:00.000Z');

    const response: MergeResult = {
      merged: { subscriptions: 1, purchases: 1 },
      skipped: { subscriptions: [], purchases: [] },
    };
    req.flush(response);

    const result = await promise;
    expect(result).toEqual(response);
    // Local store cleared.
    expect((await subs.list()).length).toBe(0);
    expect((await purchases.list()).length).toBe(0);
    expect(svc.busy()).toBe(false);
  });

  it('merge() does NOT clear local store on HTTP error', async () => {
    auth.setAuthenticated(true);
    await subs.add('11111111-1111-1111-1111-111111111111');

    const promise = firstValueFrom(svc.merge());
    await new Promise<void>((r) => setTimeout(r, 0));
    const req = httpMock.expectOne('/api/me/sync/merge');
    req.flush({ title: 'oops', status: 500 }, { status: 500, statusText: 'err' });
    await expect(promise).rejects.toBeDefined();
    expect((await subs.list()).length).toBe(1);
    expect(svc.busy()).toBe(false);
  });

  it('dismiss() clears local store without HTTP', async () => {
    await subs.add('11111111-1111-1111-1111-111111111111');
    await svc.dismiss();
    httpMock.expectNone('/api/me/sync/merge');
    expect((await subs.list()).length).toBe(0);
  });

  it('openPrompt() / closePrompt() flip the isOpen signal', () => {
    expect(svc.isOpen()).toBe(false);
    svc.openPrompt();
    expect(svc.isOpen()).toBe(true);
    svc.closePrompt();
    expect(svc.isOpen()).toBe(false);
  });
});
