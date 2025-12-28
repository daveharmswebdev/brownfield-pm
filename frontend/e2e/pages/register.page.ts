import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class RegisterPage extends BasePage {
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly submitButton: Locator;
  readonly successContent: Locator;
  readonly errorContent: Locator;
  readonly loginLink: Locator;

  constructor(page: Page) {
    super(page);
    this.passwordInput = page.locator('input[formControlName="password"]');
    this.confirmPasswordInput = page.locator('input[formControlName="confirmPassword"]');
    this.submitButton = page.locator('button[type="submit"]');
    this.successContent = page.locator('.success-content');
    this.errorContent = page.locator('.error-content');
    this.loginLink = page.locator('a[routerLink="/login"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/register');
  }

  async gotoWithToken(token: string): Promise<void> {
    await this.page.goto(`/register?token=${encodeURIComponent(token)}`);
  }

  async register(password: string): Promise<void> {
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(password);
    await this.submitButton.click();
  }

  async expectSuccess(): Promise<void> {
    await expect(this.successContent).toBeVisible();
    await expect(this.page.locator('h2')).toContainText('Registration Complete');
  }

  async expectTokenError(): Promise<void> {
    await expect(this.errorContent).toBeVisible();
    await expect(this.page.locator('h2')).toContainText('Invalid Invitation');
  }

  async expectServerError(errorText: string): Promise<void> {
    await expect(this.page.locator('.server-error')).toContainText(errorText);
  }
}
