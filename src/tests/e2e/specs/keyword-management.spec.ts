import { test } from "../fixtures/app.fixture";

test.describe("絞り込みキーワード管理", () => {
  test("検索フレーズを絞り込みキーワードとして登録できる", async ({
    searchPage,
    keywordManagementPage,
  }) => {
    const keyword = "E2E 検索フレーズ";

    await searchPage.goto();
    await searchPage.registerSearchPhraseAsKeyword(keyword);
    await keywordManagementPage.goto();
    await keywordManagementPage.hasKeyword(keyword);
  });

  test("キーワードを追加、編集、削除してリロード後も状態を維持する", async ({
    keywordManagementPage,
    homePage,
  }) => {
    await keywordManagementPage.goto();
    await keywordManagementPage.addKeyword("E2E タイトル");
    await keywordManagementPage.editKeyword(0, "E2E 著者");
    await keywordManagementPage.reload();
    await keywordManagementPage.hasKeyword("E2E 著者");
    await keywordManagementPage.removeKeyword(0);
    await keywordManagementPage.isKeywordManagementEmpty();
    await keywordManagementPage.reload();
    await keywordManagementPage.isKeywordManagementEmpty();
    await homePage.goto();
    await homePage.showsNoAppliedKeywords();
  });

  test("ホームとカレンダーで保存済みキーワードを共有する", async ({
    keywordManagementPage,
    homePage,
    calendarPage,
  }) => {
    const keyword = "E2E 共有キーワード";

    await keywordManagementPage.goto();
    await keywordManagementPage.addKeyword(keyword);
    await homePage.goto();
    await homePage.showsAppliedKeyword(keyword);
    await calendarPage.goto();
    await calendarPage.showsAppliedKeyword(keyword);
  });

  test("一致しないキーワードでは発売予定の空状態を表示する", async ({
    keywordManagementPage,
    homePage,
    calendarPage,
  }) => {
    await keywordManagementPage.goto();
    await keywordManagementPage.addKeyword("e2e-no-matching-upcoming-keyword");
    await homePage.goto();
    await homePage.showAllUpcomingVolumes();
    await homePage.showsNoMatchingUpcomingVolumes();
    await calendarPage.goto();
    await calendarPage.showAllUpcomingVolumes();
    await calendarPage.showsNoMatchingUpcomingVolumes();
  });
});
