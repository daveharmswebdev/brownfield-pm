import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { SidebarNavComponent } from './sidebar-nav.component';
import { AuthService, User } from '../../services/auth.service';

describe('SidebarNavComponent', () => {
  let component: SidebarNavComponent;
  let fixture: ComponentFixture<SidebarNavComponent>;
  let mockLogout: ReturnType<typeof vi.fn>;

  const mockUser: User = {
    userId: 'test-user-id',
    accountId: 'test-account-id',
    role: 'Owner',
  };

  beforeEach(async () => {
    mockLogout = vi.fn().mockReturnValue(of(undefined));

    const mockAuthService = {
      currentUser: signal<User | null>(mockUser),
      logout: mockLogout,
    };

    await TestBed.configureTestingModule({
      imports: [SidebarNavComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarNavComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have 7 navigation items (AC7.1)', () => {
    expect(component.navItems.length).toBe(7);
  });

  it('should have correct navigation items in order (AC7.1)', () => {
    const expectedLabels = [
      'Dashboard',
      'Properties',
      'Expenses',
      'Income',
      'Receipts',
      'Reports',
      'Settings',
    ];
    const actualLabels = component.navItems.map((item) => item.label);
    expect(actualLabels).toEqual(expectedLabels);
  });

  it('should have Dashboard as first item (AC7.1)', () => {
    expect(component.navItems[0].label).toBe('Dashboard');
    expect(component.navItems[0].route).toBe('/dashboard');
  });

  it('should have Receipts badge placeholder (AC7.1)', () => {
    const receiptsItem = component.navItems.find((item) => item.label === 'Receipts');
    expect(receiptsItem).toBeTruthy();
    expect(receiptsItem?.badge).toBe(0);
  });

  it('should display user role in footer (AC7.2)', () => {
    expect(component.userDisplayName).toBe('Owner');
  });

  it('should render logout button (AC7.2)', () => {
    const logoutButton = fixture.debugElement.query(
      By.css('[data-testid="logout-button"]')
    );
    expect(logoutButton).toBeTruthy();
  });

  it('should call logout on auth service when logout clicked (AC7.2)', () => {
    component.logout();
    expect(mockLogout).toHaveBeenCalled();
  });

  it('should render all nav items in the DOM (AC7.1)', () => {
    const navItems = fixture.debugElement.queryAll(By.css('.nav-item'));
    expect(navItems.length).toBe(7);
  });
});
