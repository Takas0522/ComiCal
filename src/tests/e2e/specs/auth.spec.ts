import { test } from '@playwright/test';
import { AuthPage } from '../pages/auth.page';

test.describe('認証', () => {
  test('未ログインユーザーはログインページにリダイレクトされる', async ({ page }) => {
    const authPage = new AuthPage(page);
    await authPage.gotoLogin();
    await authPage.isLoginPageVisible();
    await authPage.isLoginButtonVisible();
  });

  test('ログインページにログインボタンが表示される', async ({ page }) => {
    const authPage = new AuthPage(page);
    await authPage.gotoLogin();
    await authPage.isLoginPageVisible();
  });
});
