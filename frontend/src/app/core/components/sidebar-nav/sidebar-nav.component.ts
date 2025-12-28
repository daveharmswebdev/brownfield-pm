import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Router } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AuthService } from '../../services/auth.service';
import { YearSelectorComponent } from '../../../shared/components/year-selector/year-selector.component';

/**
 * Navigation item interface for sidebar links
 */
interface NavItem {
  label: string;
  route: string;
  icon: string;
  badge?: number | null;
}

/**
 * Sidebar Navigation Component (AC7.1, AC7.2)
 *
 * Dark sidebar with:
 * - Forest Green accents for active states
 * - Navigation items with icons
 * - User profile section in footer
 * - Logout functionality
 */
@Component({
  selector: 'app-sidebar-nav',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatListModule,
    MatIconModule,
    MatBadgeModule,
    MatButtonModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    YearSelectorComponent,
  ],
  templateUrl: './sidebar-nav.component.html',
  styleUrl: './sidebar-nav.component.scss',
})
export class SidebarNavComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.authService.currentUser;
  readonly isLoggingOut = signal(false);

  // Navigation items (AC7.1)
  readonly navItems: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Properties', route: '/properties', icon: 'home_work' },
    { label: 'Expenses', route: '/expenses', icon: 'receipt_long' },
    { label: 'Income', route: '/income', icon: 'payments' },
    { label: 'Receipts', route: '/receipts', icon: 'document_scanner', badge: 0 }, // Placeholder badge
    { label: 'Reports', route: '/reports', icon: 'assessment' },
    { label: 'Settings', route: '/settings', icon: 'settings' },
  ];

  /**
   * Get user display name - prefer email, fall back to role
   */
  get userDisplayName(): string {
    const user = this.currentUser();
    if (!user) return 'User';
    // Since User interface only has userId, accountId, role
    // We'll display the role for now (email would be better)
    return user.role || 'User';
  }

  /**
   * Logout handler (AC7.2) - calls auth service and redirects to login
   */
  logout(): void {
    if (this.isLoggingOut()) return;

    this.isLoggingOut.set(true);
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: () => {
        // Even on error, redirect to login (local state is cleared)
        this.router.navigate(['/login']);
      },
    });
  }
}
