import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';

/**
 * Navigation item interface for bottom nav tabs
 */
interface BottomNavItem {
  label: string;
  route: string;
  icon: string;
  badge?: number | null;
}

/**
 * Bottom Navigation Component (AC7.5)
 *
 * Mobile-only bottom tab navigation bar with:
 * - 5 main navigation items (Dashboard, Properties, Expenses, Income, Receipts)
 * - FAB placeholder for quick actions
 * - Touch-friendly 44px minimum tap targets
 */
@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatBadgeModule,
  ],
  templateUrl: './bottom-nav.component.html',
  styleUrl: './bottom-nav.component.scss',
})
export class BottomNavComponent {
  // Bottom nav shows only 5 most important items (AC7.5)
  readonly navItems: BottomNavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Properties', route: '/properties', icon: 'home_work' },
    { label: 'Expenses', route: '/expenses', icon: 'receipt_long' },
    { label: 'Income', route: '/income', icon: 'payments' },
    { label: 'Receipts', route: '/receipts', icon: 'document_scanner', badge: 0 },
  ];

  /**
   * FAB click handler - placeholder for future quick actions (Epic 5)
   */
  onFabClick(): void {
    // Placeholder - will be implemented in Epic 5 for receipt capture
    console.log('FAB clicked - quick action placeholder');
  }
}
