import { isPlatformBrowser } from '@angular/common';
import { Injectable, inject, PLATFORM_ID, signal } from '@angular/core';
import { del, get, set } from 'idb-keyval';

const STORAGE_KEY = 'upcoming-filter-keywords';
export const MAX_KEYWORDS = 16;
export const MAX_KEYWORD_CHARACTERS = 512;

export type KeywordMutationResult =
  | { success: true }
  | {
      success: false;
      reason: 'empty' | 'duplicate' | 'too-many-keywords' | 'too-long' | 'invalid-index';
    };

@Injectable({ providedIn: 'root' })
export class UpcomingFilterStore {
  private readonly platformId = inject(PLATFORM_ID);
  private restorePromise: Promise<void> | null = null;
  private mutationQueue: Promise<void> = Promise.resolve();

  readonly keywords = signal<readonly string[]>([]);
  readonly restored = signal(false);

  restore(): Promise<void> {
    if (this.restorePromise) return this.restorePromise;

    this.restorePromise = this.restoreKeywords();
    return this.restorePromise;
  }

  addKeyword(keyword: string): Promise<KeywordMutationResult> {
    return this.enqueueMutation(async () => {
      const normalized = normalizeKeyword(keyword);
      if (!normalized) return { success: false, reason: 'empty' };

      const current = this.keywords();
      if (current.includes(normalized)) return { success: false, reason: 'duplicate' };

      const next = [...current, normalized];
      if (!isWithinKeywordLimit(next)) return { success: false, reason: 'too-many-keywords' };
      if (!isWithinCharacterLimit(next)) return { success: false, reason: 'too-long' };

      await this.commit(next);
      return { success: true };
    });
  }

  updateKeyword(index: number, keyword: string): Promise<KeywordMutationResult> {
    return this.enqueueMutation(async () => {
      const current = this.keywords();
      if (index < 0 || index >= current.length) return { success: false, reason: 'invalid-index' };

      const normalized = normalizeKeyword(keyword);
      if (!normalized) return { success: false, reason: 'empty' };
      if (
        current.some(
          (existing, existingIndex) => existingIndex !== index && existing === normalized,
        )
      ) {
        return { success: false, reason: 'duplicate' };
      }

      const next = current.map((existing, existingIndex) =>
        existingIndex === index ? normalized : existing,
      );
      if (!isWithinCharacterLimit(next)) return { success: false, reason: 'too-long' };

      await this.commit(next);
      return { success: true };
    });
  }

  removeKeyword(index: number): Promise<KeywordMutationResult> {
    return this.enqueueMutation(async () => {
      const current = this.keywords();
      if (index < 0 || index >= current.length) return { success: false, reason: 'invalid-index' };

      await this.commit(current.filter((_, currentIndex) => currentIndex !== index));
      return { success: true };
    });
  }

  clearKeywords(): Promise<void> {
    return this.enqueueMutation(async () => {
      this.keywords.set([]);
      if (isPlatformBrowser(this.platformId)) await del(STORAGE_KEY);
    });
  }

  private async restoreKeywords(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      this.restored.set(true);
      return;
    }

    try {
      const saved = await get<unknown>(STORAGE_KEY);
      this.keywords.set(normalizeKeywords(saved));
    } catch {
      this.keywords.set([]);
    } finally {
      this.restored.set(true);
    }
  }

  private async commit(keywords: readonly string[]): Promise<void> {
    this.keywords.set(keywords);
    if (isPlatformBrowser(this.platformId)) await set(STORAGE_KEY, keywords);
  }

  private enqueueMutation<T>(mutation: () => Promise<T>): Promise<T> {
    const queuedMutation = this.mutationQueue
      .catch(() => undefined)
      .then(async () => {
        await this.restore();
        return mutation();
      });

    this.mutationQueue = queuedMutation.then(
      () => undefined,
      () => undefined,
    );
    return queuedMutation;
  }
}

export function normalizeKeyword(value: string): string {
  return value.normalize('NFKC').trim();
}

function normalizeKeywords(value: unknown): string[] {
  if (!Array.isArray(value)) return [];

  const keywords: string[] = [];
  for (const item of value) {
    if (typeof item !== 'string') continue;

    const keyword = normalizeKeyword(item);
    if (
      keyword &&
      !keywords.includes(keyword) &&
      isWithinKeywordLimit([...keywords, keyword]) &&
      isWithinCharacterLimit([...keywords, keyword])
    ) {
      keywords.push(keyword);
    }
  }

  return keywords;
}

function isWithinKeywordLimit(keywords: readonly string[]): boolean {
  return keywords.length <= MAX_KEYWORDS;
}

function isWithinCharacterLimit(keywords: readonly string[]): boolean {
  return keywords.reduce((total, keyword) => total + keyword.length, 0) <= MAX_KEYWORD_CHARACTERS;
}
