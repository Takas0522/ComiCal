import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home.page').then((m) => m.HomePage),
    title: 'まんがリマインダー',
  },
  {
    path: 'calendar',
    loadComponent: () => import('./pages/calendar/calendar.page').then((m) => m.CalendarPage),
    title: 'カレンダー | まんがリマインダー',
  },
  {
    path: 'search',
    loadComponent: () => import('./pages/search/search.page').then((m) => m.SearchPage),
    title: '検索 | まんがリマインダー',
  },
  {
    path: 'subscriptions',
    loadComponent: () =>
      import('./pages/subscriptions/subscriptions.page').then((m) => m.SubscriptionsPage),
    title: '購読一覧 | まんがリマインダー',
  },
  {
    path: 'series/:id',
    loadComponent: () =>
      import('./pages/series-detail/series-detail.page').then((m) => m.SeriesDetailPage),
    title: 'シリーズ詳細 | まんがリマインダー',
  },
  {
    path: 'settings',
    loadComponent: () => import('./pages/settings/settings.page').then((m) => m.SettingsPage),
    title: '設定 | まんがリマインダー',
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.page').then((m) => m.LoginPage),
    title: 'ログイン | まんがリマインダー',
  },
  {
    path: 'legal/oss',
    loadComponent: () => import('./pages/legal/oss.page').then((m) => m.OssPage),
    title: 'OSS ライセンス情報 | まんがリマインダー',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
