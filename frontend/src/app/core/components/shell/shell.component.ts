import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import { SidebarNavComponent } from '../sidebar-nav/sidebar-nav.component';
import { BottomNavComponent } from '../bottom-nav/bottom-nav.component';
import { YearSelectorComponent } from '../../../shared/components/year-selector/year-selector.component';

/**
 * Shell Component - Main layout wrapper for authenticated views (AC7.1, AC7.3)
 *
 * Provides:
 * - Dark sidebar navigation on desktop (≥1024px) - always visible
 * - Collapsible sidebar on tablet (768-1023px) - toggle with hamburger menu
 * - Bottom tab navigation on mobile (<768px)
 * - Light background content area (#FAFAFA)
 * - Responsive layout with smooth transitions
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatToolbarModule,
    SidebarNavComponent,
    BottomNavComponent,
    YearSelectorComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);

  // Breakpoint detection using CDK BreakpointObserver
  // Mobile: <768px, Tablet: 768-1023px, Desktop: ≥1024px
  private readonly isDesktop$ = this.breakpointObserver
    .observe(['(min-width: 1024px)'])
    .pipe(map((result) => result.matches));

  private readonly isTablet$ = this.breakpointObserver
    .observe(['(min-width: 768px) and (max-width: 1023px)'])
    .pipe(map((result) => result.matches));

  private readonly isMobile$ = this.breakpointObserver
    .observe(['(max-width: 767px)'])
    .pipe(map((result) => result.matches));

  // Convert observables to signals for reactive template binding
  readonly isDesktop = toSignal(this.isDesktop$, { initialValue: false });
  readonly isTablet = toSignal(this.isTablet$, { initialValue: false });
  readonly isMobile = toSignal(this.isMobile$, { initialValue: false });

  // Sidebar open state for tablet (collapsible)
  readonly sidebarOpen = signal(false);

  // Computed: Show sidebar on desktop (always) or tablet (when open)
  readonly showSidebar = computed(() => this.isDesktop() || this.isTablet());

  // Computed: Sidebar mode - 'side' for desktop, 'over' for tablet
  readonly sidebarMode = computed(() => this.isDesktop() ? 'side' : 'over');

  // Computed: Sidebar should be opened - always on desktop, toggle on tablet
  readonly sidebarOpened = computed(() => this.isDesktop() || (this.isTablet() && this.sidebarOpen()));

  // Computed: Show bottom nav only on mobile
  readonly showBottomNav = computed(() => this.isMobile());

  // Computed: Show hamburger menu on tablet
  readonly showMenuButton = computed(() => this.isTablet());

  constructor() {
    // Close sidebar when switching away from tablet mode
    effect(() => {
      if (!this.isTablet()) {
        this.sidebarOpen.set(false);
      }
    });
  }

  /**
   * Toggle sidebar visibility (tablet only)
   */
  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  /**
   * Close sidebar (used when clicking backdrop on tablet)
   */
  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
}
