import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { del, get, set } from 'idb-keyval';
import { UpcomingFilterStore } from './upcoming-filter.store';

jest.mock('idb-keyval', () => ({
  del: jest.fn(),
  get: jest.fn(),
  set: jest.fn(),
}));

describe('UpcomingFilterStore', () => {
  let store: UpcomingFilterStore;
  const getMock = get as jest.MockedFunction<typeof get>;
  const setMock = set as jest.MockedFunction<typeof set>;
  const delMock = del as jest.MockedFunction<typeof del>;

  beforeEach(() => {
    getMock.mockResolvedValue(undefined);
    TestBed.configureTestingModule({
      providers: [{ provide: PLATFORM_ID, useValue: 'browser' }],
    });
    store = TestBed.inject(UpcomingFilterStore);
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    jest.clearAllMocks();
  });

  function deferRestore(): { resolve: (value: unknown) => void } {
    let resolve!: (value: unknown) => void;
    getMock.mockReturnValue(
      new Promise<unknown>((promiseResolve) => {
        resolve = promiseResolve;
      }),
    );
    return { resolve };
  }

  it('restores FormKC-normalized, unique keywords from IndexedDB', async () => {
    getMock.mockResolvedValue([' 漫画 ', '漫画', 'ＡＢＣ', 'ABC', '著者', 1]);

    await store.restore();

    expect(store.keywords()).toEqual(['漫画', 'ABC', '著者']);
    expect(store.restored()).toBe(true);
    expect(getMock).toHaveBeenCalledWith('upcoming-filter-keywords');
  });

  it('adds a FormKC-normalized, trimmed keyword and persists it to IndexedDB', async () => {
    await store.restore();

    const result = await store.addKeyword(' 漫画 ');

    expect(result).toEqual({ success: true });
    expect(store.keywords()).toEqual(['漫画']);
    expect(setMock).toHaveBeenCalledWith('upcoming-filter-keywords', ['漫画']);
  });

  it('preserves a keyword added before IndexedDB restoration completes', async () => {
    const pendingRestore = deferRestore();
    const restore = store.restore();
    const add = store.addKeyword('新刊');

    pendingRestore.resolve(['漫画']);
    await Promise.all([restore, add]);

    expect(store.keywords()).toEqual(['漫画', '新刊']);
    expect(setMock).toHaveBeenLastCalledWith('upcoming-filter-keywords', ['漫画', '新刊']);
  });

  it('updates a keyword requested before IndexedDB restoration completes', async () => {
    const pendingRestore = deferRestore();
    const restore = store.restore();
    const update = store.updateKeyword(1, '作者');

    pendingRestore.resolve(['漫画', '著者']);
    await expect(update).resolves.toEqual({ success: true });
    await restore;

    expect(store.keywords()).toEqual(['漫画', '作者']);
    expect(setMock).toHaveBeenLastCalledWith('upcoming-filter-keywords', ['漫画', '作者']);
  });

  it('removes a keyword requested before IndexedDB restoration completes', async () => {
    const pendingRestore = deferRestore();
    const restore = store.restore();
    const remove = store.removeKeyword(0);

    pendingRestore.resolve(['漫画', '著者']);
    await expect(remove).resolves.toEqual({ success: true });
    await restore;

    expect(store.keywords()).toEqual(['著者']);
    expect(setMock).toHaveBeenLastCalledWith('upcoming-filter-keywords', ['著者']);
  });

  it('keeps keywords cleared before IndexedDB restoration completes empty', async () => {
    const pendingRestore = deferRestore();
    const restore = store.restore();
    const clear = store.clearKeywords();

    pendingRestore.resolve(['漫画']);
    await Promise.all([restore, clear]);

    expect(store.keywords()).toEqual([]);
    expect(delMock).toHaveBeenCalledWith('upcoming-filter-keywords');
  });

  it('finishes restoration with an empty state when IndexedDB is unavailable', async () => {
    getMock.mockRejectedValue(new Error('IndexedDB unavailable'));

    await store.restore();

    expect(store.keywords()).toEqual([]);
    expect(store.restored()).toBe(true);
  });

  it('rejects duplicate and over-limit keywords without persisting', async () => {
    await store.addKeyword('漫画');
    setMock.mockClear();

    await expect(store.addKeyword(' 漫画 ')).resolves.toEqual({
      success: false,
      reason: 'duplicate',
    });
    await expect(store.addKeyword('あ'.repeat(513))).resolves.toEqual({
      success: false,
      reason: 'too-long',
    });
    expect(setMock).not.toHaveBeenCalled();
  });

  it('rejects a seventeenth keyword without persisting', async () => {
    await store.restore();
    for (let index = 0; index < 16; index += 1) {
      await expect(store.addKeyword(`keyword-${index}`)).resolves.toEqual({ success: true });
    }
    setMock.mockClear();

    await expect(store.addKeyword('keyword-16')).resolves.toEqual({
      success: false,
      reason: 'too-many-keywords',
    });

    expect(store.keywords()).toHaveLength(16);
    expect(setMock).not.toHaveBeenCalled();
  });

  it('restores no more than sixteen valid keywords', async () => {
    getMock.mockResolvedValue(Array.from({ length: 17 }, (_, index) => `keyword-${index}`));

    await store.restore();

    expect(store.keywords()).toEqual(Array.from({ length: 16 }, (_, index) => `keyword-${index}`));
  });

  it('updates, removes, and clears persisted keywords', async () => {
    await store.addKeyword('漫画');
    await store.addKeyword('著者');

    await expect(store.updateKeyword(0, '作品名')).resolves.toEqual({ success: true });
    await expect(store.removeKeyword(1)).resolves.toEqual({ success: true });
    await store.clearKeywords();

    expect(store.keywords()).toEqual([]);
    expect(delMock).toHaveBeenCalledWith('upcoming-filter-keywords');
  });

  it('completes restoration without IndexedDB during SSR', async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [{ provide: PLATFORM_ID, useValue: 'server' }],
    });
    store = TestBed.inject(UpcomingFilterStore);

    await store.restore();

    expect(store.keywords()).toEqual([]);
    expect(store.restored()).toBe(true);
    expect(getMock).not.toHaveBeenCalled();
  });

  describe('legacy SEARCH_KEYWORDS migration', () => {
    beforeEach(() => {
      localStorage.clear();
    });

    afterEach(() => {
      localStorage.clear();
    });

    it('migrates legacy keywords into empty IndexedDB store and removes legacy data', async () => {
      localStorage.setItem(
        'SEARCH_KEYWORDS',
        JSON.stringify(['紅殻のパンドラ', '幼女戦記', '月刊少女']),
      );
      getMock.mockResolvedValue(undefined);

      await store.restore();

      expect(store.keywords()).toEqual(['紅殻のパンドラ', '幼女戦記', '月刊少女']);
      expect(setMock).toHaveBeenCalledWith('upcoming-filter-keywords', [
        '紅殻のパンドラ',
        '幼女戦記',
        '月刊少女',
      ]);
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBeNull();
    });

    it('merges legacy keywords with existing IndexedDB keywords (new store first, dedup)', async () => {
      localStorage.setItem('SEARCH_KEYWORDS', JSON.stringify(['幼女戦記', 'ベルセルク']));
      getMock.mockResolvedValue(['紅殻のパンドラ', 'ベルセルク']);

      await store.restore();

      expect(store.keywords()).toEqual(['紅殻のパンドラ', 'ベルセルク', '幼女戦記']);
      expect(setMock).toHaveBeenCalledWith('upcoming-filter-keywords', [
        '紅殻のパンドラ',
        'ベルセルク',
        '幼女戦記',
      ]);
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBeNull();
    });

    it('discards malformed JSON and removes the legacy key', async () => {
      localStorage.setItem('SEARCH_KEYWORDS', '{not-json');
      getMock.mockResolvedValue(['既存']);

      await store.restore();

      expect(store.keywords()).toEqual(['既存']);
      expect(setMock).not.toHaveBeenCalled();
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBeNull();
    });

    it('discards non-array legacy payloads and removes the legacy key', async () => {
      localStorage.setItem('SEARCH_KEYWORDS', JSON.stringify({ keywords: ['x'] }));
      getMock.mockResolvedValue(['既存']);

      await store.restore();

      expect(store.keywords()).toEqual(['既存']);
      expect(setMock).not.toHaveBeenCalled();
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBeNull();
    });

    it('does nothing when the legacy key is absent', async () => {
      getMock.mockResolvedValue(['既存']);

      await store.restore();

      expect(store.keywords()).toEqual(['既存']);
      expect(setMock).not.toHaveBeenCalled();
    });

    it('caps merged keywords at MAX_KEYWORDS (16), new-store first', async () => {
      const existing = Array.from({ length: 14 }, (_, i) => `既存${i}`);
      const legacy = Array.from({ length: 5 }, (_, i) => `旧${i}`);
      localStorage.setItem('SEARCH_KEYWORDS', JSON.stringify(legacy));
      getMock.mockResolvedValue(existing);

      await store.restore();

      expect(store.keywords()).toEqual([...existing, '旧0', '旧1']);
      expect(store.keywords()).toHaveLength(16);
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBeNull();
    });

    it('caps merged keywords at MAX_KEYWORD_CHARACTERS (512)', async () => {
      const existing = ['a'.repeat(500)];
      const legacy = ['b'.repeat(20), 'c'.repeat(5)];
      localStorage.setItem('SEARCH_KEYWORDS', JSON.stringify(legacy));
      getMock.mockResolvedValue(existing);

      await store.restore();

      expect(store.keywords()).toEqual([existing[0], 'c'.repeat(5)]);
    });

    it('skips migration during SSR (no localStorage access)', async () => {
      localStorage.setItem('SEARCH_KEYWORDS', JSON.stringify(['幼女戦記']));

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [{ provide: PLATFORM_ID, useValue: 'server' }],
      });
      store = TestBed.inject(UpcomingFilterStore);

      await store.restore();

      expect(store.keywords()).toEqual([]);
      expect(setMock).not.toHaveBeenCalled();
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBe(JSON.stringify(['幼女戦記']));
    });

    it('recovers when localStorage.getItem throws', async () => {
      const spy = jest.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
        throw new DOMException('denied', 'SecurityError');
      });
      getMock.mockResolvedValue(['既存']);

      await store.restore();

      expect(store.keywords()).toEqual(['既存']);
      expect(setMock).not.toHaveBeenCalled();
      spy.mockRestore();
    });

    it('normalizes and dedups legacy keywords via NFKC/trim', async () => {
      localStorage.setItem(
        'SEARCH_KEYWORDS',
        JSON.stringify([' 幼女戦記 ', 'ＡＢＣ', 'ABC', '', 42]),
      );
      getMock.mockResolvedValue(undefined);

      await store.restore();

      expect(store.keywords()).toEqual(['幼女戦記', 'ABC']);
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBeNull();
    });

    it('retains the legacy key when IndexedDB set() fails so migration retries next boot', async () => {
      const legacy = JSON.stringify(['幼女戦記', 'ベルセルク']);
      localStorage.setItem('SEARCH_KEYWORDS', legacy);
      getMock.mockResolvedValue(undefined);
      setMock.mockRejectedValueOnce(new Error('idb write failed'));

      await store.restore();

      expect(store.keywords()).toEqual(['幼女戦記', 'ベルセルク']);
      expect(localStorage.getItem('SEARCH_KEYWORDS')).toBe(legacy);
    });
  });
});
