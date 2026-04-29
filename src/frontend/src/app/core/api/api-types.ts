/**
 * Frontend mirror of the backend Application DTOs (camelCase via
 * `JsonSerializerDefaults.Web` in ASP.NET Core).
 *
 * NOTE: keep these shapes 1:1 with `src/backend/application/DTOs/Dtos.cs`.
 */

export interface Author {
  readonly id: string;
  readonly name: string;
}

export interface Publisher {
  readonly id: string;
  readonly name: string;
}

export interface ThumbnailAsset {
  readonly blobKey: string;
  readonly width: number;
  readonly height: number;
}

export interface Volume {
  readonly id: string;
  readonly seriesId: string;
  readonly isbn: string;
  readonly volumeNumber: number | null;
  readonly releaseDate: string | null;
  readonly releaseDateIsMonthOnly: boolean;
  readonly rakutenItemUrl: string | null;
  readonly thumbnail: ThumbnailAsset | null;
}

export interface SeriesSummary {
  readonly id: string;
  readonly title: string;
  readonly publisherId: string | null;
  readonly primaryAuthorId: string;
  readonly isCompleted: boolean;
}

export interface SeriesDetail {
  readonly series: SeriesSummary;
  readonly volumes: readonly Volume[];
}

/** Generic keyset-paginated result. */
export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly nextCursor?: string | null;
}

export type SeriesSearchResult = PagedResult<SeriesSummary>;
export type VolumeSearchResult = PagedResult<Volume>;

export interface SearchSeriesParams {
  readonly q?: string;
  readonly publisherId?: string;
  readonly authorId?: string;
  readonly limit?: number;
  readonly cursor?: string;
}

export interface SearchVolumesParams {
  readonly q?: string;
  readonly releaseFrom?: string;
  readonly releaseTo?: string;
  readonly limit?: number;
  readonly cursor?: string;
}

/** A single day in the release calendar with all volumes released that day. */
export interface CalendarDay {
  readonly date: string;
  readonly volumes: readonly Volume[];
}

/** Calendar DTO covering N consecutive months starting at `monthFrom`. */
export interface CalendarDto {
  readonly monthFrom: string;
  readonly monthCount: number;
  readonly days: readonly CalendarDay[];
}
