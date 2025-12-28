import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class RegisterPage extends BasePage {
  readonly accountNameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly submitButton: Locator;
  readonly successContent: Locator;
  readonly loginLink: Locator;

  constructor(page: Page) {
    super(page);
    this.accountNameInput = page.locator('input[formControlName="accountName"]');
    this.emailInput = page.locator('input[formControlName="email"]');
    this.passwordInput = page.locator('input[formControlName="password"]');
    this.confirmPasswordInput = page.locator('input[formControlName="confirmPassword"]');
    this.submitButton = page.locator('button[type="submit"]');
    this.successContent = page.locator('.success-content');
    this.loginLink = page.locator('a[routerLink="/login"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/register');
  }

  async register(accountName: string, email: string, password: string): Promise<void> {
    await this.accountNameInput.fill(accountName);
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(password);
    await this.submitButton.click();
  }

  async expectSuccess(): Promise<void> {
    await expect(this.successContent).toBeVisible();
    await expect(this.page.locator('h2')).toContainText('Registration Successful');
  }
}
