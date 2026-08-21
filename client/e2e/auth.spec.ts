import { expect, test } from '@playwright/test';

test.describe('auth guest flows', () => {
  test('login page is the default guest destination', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible();
    await expect(page.getByLabel('Email')).toBeVisible();
  });

  test('register page is reachable from login', async ({ page }) => {
    await page.goto('/auth/login');
    await page.getByRole('link', { name: /create|register|sign up/i }).first().click();
    await expect(page).toHaveURL(/\/auth\/register/);
  });

  test('protected routes redirect guests to login with returnUrl', async ({ page }) => {
    await page.goto('/users');
    await expect(page).toHaveURL(/\/auth\/login/);
    await expect(page).toHaveURL(/returnUrl=/);
  });
});

test.describe('authenticated flows', () => {
  const email = process.env['E2E_USER_EMAIL'];
  const password = process.env['E2E_USER_PASSWORD'];

  test.skip(!email || !password, 'Set E2E_USER_EMAIL and E2E_USER_PASSWORD to run authenticated flows.');

  test('login reaches home and can open authorization permissions', async ({ page }) => {
    await page.goto('/auth/login');
    await page.getByLabel('Email').fill(email!);
    await page.getByLabel('Password').fill(password!);
    await page.getByRole('button', { name: /continue/i }).click();

    await expect(page).toHaveURL('/');
    await expect(page.getByText(email!)).toBeVisible();

    await page.goto('/authorization/permissions');
    await expect(page.getByRole('heading', { name: 'Permissions' })).toBeVisible();
  });
});
