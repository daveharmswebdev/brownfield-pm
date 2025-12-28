import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map, take, catchError, of, switchMap, filter, first } from 'rxjs';
import { toObservable } from '@angular/core/rxjs-interop';
import { AuthService } from '../services/auth.service';

/**
 * Auth Guard for protecting routes that require authentication (AC4.5, AC7.6, AC7.8).
 *
 * Checks if user is authenticated:
 * - If authenticated, allows access
 * - If not authenticated, attempts token refresh (for page refresh persistence)
 * - If refresh fails, redirects to login with return URL
 * - Prevents flash of protected content while checking auth (AC7.8)
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If already authenticated, allow access immediately
  if (authService.isAuthenticated()) {
    return true;
  }

  // Not authenticated - try to refresh token (handles page refresh scenario)
  // This prevents flash of content by waiting for auth check to complete (AC7.8)
  return authService.initializeAuth().pipe(
    take(1),
    map(response => {
      if (response) {
        // Refresh succeeded - user is now authenticated
        return true;
      }
      // Refresh failed - redirect to login with return URL stored
      return createLoginRedirect(router, state.url);
    }),
    catchError(() => {
      // Error during refresh - redirect to login
      return of(createLoginRedirect(router, state.url));
    })
  );
};

/**
 * Create redirect URL to login page with return URL (AC7.6).
 * Stores the intended URL so user can be redirected after login.
 */
function createLoginRedirect(router: Router, returnUrl: string): UrlTree {
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl }
  });
}

/**
 * Guest Guard for routes that should only be accessible to unauthenticated users.
 * (e.g., login and register pages)
 *
 * If user is authenticated, redirects to dashboard (AC7.6).
 */
export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If already authenticated, redirect to dashboard
  if (authService.isAuthenticated()) {
    return router.createUrlTree(['/dashboard']);
  }

  // If still initializing, wait for it to complete before allowing access
  // This prevents showing login form to users who are actually authenticated
  if (authService.isInitializing()) {
    return authService.initializeAuth().pipe(
      take(1),
      map(response => {
        if (response) {
          // User is authenticated - redirect to dashboard
          return router.createUrlTree(['/dashboard']);
        }
        // User is not authenticated - allow access to guest route
        return true;
      }),
      catchError(() => {
        // Error checking auth - allow access to guest route
        return of(true);
      })
    );
  }

  // Not initializing and not authenticated - allow access
  return true;
};

/**
 * Public Guard for routes that should be accessible to both authenticated and unauthenticated users.
 * (e.g., email verification, password reset)
 *
 * Always allows access regardless of authentication state.
 * Does not redirect or block access based on auth status.
 */
export const publicGuard: CanActivateFn = () => {
  // Always allow access - this guard exists for semantic clarity
  // and to distinguish public routes from guest-only or protected routes
  return true;
};
