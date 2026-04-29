import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: 'settings', renderMode: RenderMode.Client },
  { path: 'sync', renderMode: RenderMode.Client },
  { path: 'legal/privacy', renderMode: RenderMode.Prerender },
  { path: 'legal/terms', renderMode: RenderMode.Prerender },
  { path: 'legal/oss', renderMode: RenderMode.Prerender },
  { path: '**', renderMode: RenderMode.Server },
];
