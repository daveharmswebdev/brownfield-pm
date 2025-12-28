import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

/**
 * Root Application Component (AC7.8)
 *
 * Shows loading state while auth is being initialized to prevent
 * flash of unauthenticated content on protected routes.
 *
 * Uses a pure CSS spinner instead of MatProgressSpinnerModule to keep
 * Angular Material out of the initial bundle (optimizes bundle size).
 */
@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('property-manager');
  protected readonly authService = inject(AuthService);

  // Expose isInitializing for template (AC7.8)
  readonly isInitializing = this.authService.isInitializing;

  ngOnInit(): void {
    // Initialize auth state for all routes - ensures isInitializing becomes false
    // even for routes without guards (e.g., /verify-email)
    this.authService.initializeAuth().subscribe();
  }
}
