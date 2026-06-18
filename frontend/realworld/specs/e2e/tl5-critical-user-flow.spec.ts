// realworld/specs/e2e/tl5-critical-user-flow.spec.ts

import { test, expect } from '@playwright/test';

test.describe('TL5 Critical User Flow', () => {
  test('E2E-01: user can register, create an article and view it in the application', async ({ page }) => {
    // TL5 risk reference:
    // R1 Authentication
    // R4 Data processing
    // R5 Critical user flow: authenticated article creation

    const uniqueId = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

    const user = {
      username: `tl5user${uniqueId}`.replace(/-/g, ''),
      email: `tl5-${uniqueId}@example.com`,
      password: 'password123',
    };

    const article = {
      title: `TL5 Critical Flow Article ${uniqueId}`,
      description: `TL5 test description ${uniqueId}`,
      body: `This article was created by the TL5 Playwright critical user flow test. Unique id: ${uniqueId}`,
      tags: ['tl5', 'e2e', 'riskbased'],
    };

    await page.goto('/register', { waitUntil: 'load' });

    await page.fill('input[name="username"]', user.username);
    await page.fill('input[name="email"]', user.email);
    await page.fill('input[name="password"]', user.password);

    await Promise.all([
      page.waitForURL('/'),
      page.click('button[type="submit"]'),
    ]);

    await expect(page.locator(`a[href="/profile/${user.username}"]`)).toBeVisible();

    await page.click('a[href="/editor"]');
    await expect(page).toHaveURL('/editor');

    await page.fill('input[name="title"]', article.title);
    await page.fill('input[name="description"]', article.description);
    await page.fill('textarea[name="body"]', article.body);

    for (const tag of article.tags) {
      await page.fill('input[placeholder="Enter tags"]', tag);
      await page.press('input[placeholder="Enter tags"]', 'Enter');

      await expect(page.locator('.tag-list .tag-default', { hasText: tag })).toBeVisible();
    }

    await Promise.all([
      page.waitForURL(/\/article\/.+/),
      page.click('button:has-text("Publish Article")'),
    ]);

    const articleUrl = page.url();
    expect(articleUrl).toContain('/article/');

    await expect(page.locator('h1')).toHaveText(article.title);
    await expect(page.locator('.article-content')).toContainText(article.body);

    for (const tag of article.tags) {
      await expect(page.locator('.article-content .tag-list .tag-default', { hasText: tag })).toBeVisible();
    }

    await expect(page.locator('.article-meta .author', { hasText: user.username }).first()).toBeVisible();
    await expect(page.locator('a:has-text("Edit Article")').first()).toBeVisible();
    await expect(page.locator('button:has-text("Delete Article")').first()).toBeVisible();

    await page.goto('/', { waitUntil: 'load' });

    const createdArticlePreview = page.locator('.article-preview', {
      hasText: article.title,
    }).first();

    await expect(createdArticlePreview).toBeVisible();
    await expect(createdArticlePreview).toContainText(article.description);

    await createdArticlePreview.locator('.preview-link').click();

    await expect(page).toHaveURL(articleUrl);
    await expect(page.locator('h1')).toHaveText(article.title);
  });
});