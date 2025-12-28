import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { ShellComponent } from './shell.component';

describe('ShellComponent', () => {
  let component: ShellComponent;
  let fixture: ComponentFixture<ShellComponent>;

  const createBreakpointState = (matches: boolean): BreakpointState => ({
    matches,
    breakpoints: {},
  });

  const createMockBreakpointObserver = (isDesktop: boolean, isTablet: boolean, isMobile: boolean) => ({
    observe: vi.fn().mockImplementation((query: string | readonly string[]) => {
      const queryStr = Array.isArray(query) ? query[0] : query;
      if (queryStr.includes('min-width: 1024px')) {
        return of(createBreakpointState(isDesktop));
      }
      if (queryStr.includes('min-width: 768px') && queryStr.includes('max-width: 1023px')) {
        return of(createBreakpointState(isTablet));
      }
      if (queryStr.includes('max-width: 767px')) {
        return of(createBreakpointState(isMobile));
      }
      return of(createBreakpointState(false));
    }),
  });

  describe('desktop view', () => {
    beforeEach(async () => {
      const mockBreakpointObserver = createMockBreakpointObserver(true, false, false);

      await TestBed.configureTestingModule({
        imports: [ShellComponent, NoopAnimationsModule],
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([]),
          { provide: BreakpointObserver, useValue: mockBreakpointObserver },
        ],
      }).compileComponents();

      fixture = TestBed.createComponent(ShellComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should show sidebar on desktop (AC7.1)', () => {
      expect(component.showSidebar()).toBe(true);
    });

    it('should have sidebar in side mode on desktop', () => {
      expect(component.sidebarMode()).toBe('side');
    });

    it('should have sidebar opened on desktop', () => {
      expect(component.sidebarOpened()).toBe(true);
    });

    it('should not show bottom nav on desktop', () => {
      expect(component.showBottomNav()).toBe(false);
    });

    it('should not show menu button on desktop', () => {
      expect(component.showMenuButton()).toBe(false);
    });
  });

  describe('tablet view', () => {
    beforeEach(async () => {
      const mockBreakpointObserver = createMockBreakpointObserver(false, true, false);

      await TestBed.configureTestingModule({
        imports: [ShellComponent, NoopAnimationsModule],
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([]),
          { provide: BreakpointObserver, useValue: mockBreakpointObserver },
        ],
      }).compileComponents();

      fixture = TestBed.createComponent(ShellComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should show sidebar on tablet', () => {
      expect(component.showSidebar()).toBe(true);
    });

    it('should have sidebar in over mode on tablet', () => {
      expect(component.sidebarMode()).toBe('over');
    });

    it('should have sidebar closed by default on tablet', () => {
      expect(component.sidebarOpened()).toBe(false);
    });

    it('should show menu button on tablet', () => {
      expect(component.showMenuButton()).toBe(true);
    });

    it('should not show bottom nav on tablet', () => {
      expect(component.showBottomNav()).toBe(false);
    });

    it('should toggle sidebar when toggleSidebar is called', () => {
      expect(component.sidebarOpen()).toBe(false);
      component.toggleSidebar();
      expect(component.sidebarOpen()).toBe(true);
      component.toggleSidebar();
      expect(component.sidebarOpen()).toBe(false);
    });

    it('should close sidebar when closeSidebar is called', () => {
      component.toggleSidebar(); // Open it first
      expect(component.sidebarOpen()).toBe(true);
      component.closeSidebar();
      expect(component.sidebarOpen()).toBe(false);
    });

    it('should render hamburger menu button in DOM', () => {
      const menuButton = fixture.debugElement.query(
        By.css('[data-testid="menu-toggle-button"]')
      );
      expect(menuButton).toBeTruthy();
    });
  });

  describe('mobile view', () => {
    beforeEach(async () => {
      const mockBreakpointObserver = createMockBreakpointObserver(false, false, true);

      await TestBed.configureTestingModule({
        imports: [ShellComponent, NoopAnimationsModule],
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          provideRouter([]),
          { provide: BreakpointObserver, useValue: mockBreakpointObserver },
        ],
      }).compileComponents();

      fixture = TestBed.createComponent(ShellComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should show bottom nav on mobile (AC7.5)', () => {
      expect(component.showBottomNav()).toBe(true);
    });

    it('should hide sidebar on mobile (AC7.5)', () => {
      expect(component.showSidebar()).toBe(false);
    });

    it('should not show menu button on mobile', () => {
      expect(component.showMenuButton()).toBe(false);
    });
  });
});
