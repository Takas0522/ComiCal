import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: '', renderMode: RenderMode.Server },
  { path: 'calendar', renderMode: RenderMode.Server },
  { path: 'search', renderMode: RenderMode.Server },
  { path: 'subscriptions', renderMode: RenderMode.Server },
  { path: 'series/:id', renderMode: RenderMode.Server },
  { path: 'settings', renderMode: RenderMode.Client },
  { path: 'login', renderMode: RenderMode.Server },
  { path: 'legal/privacy', renderMode: RenderMode.Prerender },
  { path: 'legal/terms', renderMode: RenderMode.Prerender },
  { path: 'legal/oss', renderMode: RenderMode.Prerender },
  { path: '**', renderMode: RenderMode.Server },
];
