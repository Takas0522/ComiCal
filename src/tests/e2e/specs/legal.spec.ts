/**
 * Legal pages + OSS dialog smoke spec.
 *
 * SKIP RATIONALE: see specs/home.spec.ts.
 */
import { test, expect } from '../fixtures/test';

const SKIP_REASON = 'Requires running app — Stage Z will enable';

test.describe('legal', () => {
  test.skip(true, SKIP_REASON);

  test('navigates to privacy from footer and renders heading', async ({
    legalPagesPage,
    axeBuilder,
  }) => {
    await legalPagesPage.gotoPrivacy();
    await legalPagesPage.expectPrivacyHeading();
    const results = await axeBuilder().analyze();
    expect(results.violations).toEqual([]);
  });

  test('navigates to terms from footer and renders heading', async ({
    legalPagesPage,
    axeBuilder,
  }) => {
    await legalPagesPage.gotoTerms();
    await legalPagesPage.expectTermsHeading();
    const results = await axeBuilder().analyze();
    expect(results.violations).toEqual([]);
  });

  test('navigates to OSS page from footer and renders heading', async ({
    legalPagesPage,
    axeBuilder,
  }) => {
    await legalPagesPage.gotoOss();
    await legalPagesPage.expectOssHeading();
    const results = await axeBuilder().analyze();
    expect(results.violations).toEqual([]);
  });

  test('opens OSS dialog from footer and closes with ESC', async ({ legalPagesPage }) => {
    await legalPagesPage.openOssDialogFromFooter();
    await legalPagesPage.closeOssDialogWithEsc();
  });
});
