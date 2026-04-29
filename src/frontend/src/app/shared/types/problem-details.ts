/**
 * Problem Details (RFC 7807) — normalized client-side type used by the
 * error interceptor and toast service.
 */
export interface ProblemDetails {
  readonly type: string;
  readonly title: string;
  readonly status: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}
