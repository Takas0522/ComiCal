import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./pages/home/home.page').then((m) => m.HomePage),
  },
  {
    path: 'calendar',
    loadComponent: () =>
      import('./pages/calendar/calendar.page').then((m) => m.CalendarPage),
  },
  {
    path: 'search',
    loadComponent: () =>
      import('./pages/search/search.page').then((m) => m.SearchPage),
  },
  {
    path: 'subscriptions',
    loadComponent: () =>
      import('./pages/subscriptions/subscriptions.page').then((m) => m.SubscriptionsPage),
  },
  {
    path: 'series/:id',
    loadComponent: () =>
      import('./pages/series-detail/series-detail.page').then((m) => m.SeriesDetailPage),
  },
  {
    path: 'volumes/by-isbn/:isbn',
    loadComponent: () =>
      import('./pages/volume-by-isbn/volume-by-isbn.page').then((m) => m.VolumeByIsbnPage),
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('./pages/settings/settings.page').then((m) => m.SettingsPage),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.page').then((m) => m.LoginPage),
  },
  {
    path: 'sync',
    loadComponent: () =>
      import('./pages/sync/sync.page').then((m) => m.SyncPage),
  },
  {
    path: 'legal/privacy',
    loadComponent: () =>
      import('./pages/legal/privacy/privacy.page').then((m) => m.PrivacyPage),
  },
  {
    path: 'legal/terms',
    loadComponent: () =>
      import('./pages/legal/terms/terms.page').then((m) => m.TermsPage),
  },
  {
    path: 'legal/oss',
    loadComponent: () =>
      import('./pages/legal/oss/oss.page').then((m) => m.OssPage),
  },
  { path: '**', redirectTo: '' },
];
