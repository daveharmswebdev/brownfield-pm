import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError, filter, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * HTTP Interceptor for JWT authentication (AC4.5, AC4.6).
 *
 * Responsibilities:
 * 1. Add Authorization header with Bearer token to outgoing requests
 * 2. Handle 401 responses by attempting token refresh
 * 3. Retry original request after successful refresh
 * 4. Redirect to login page if refresh fails
 */
export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Skip auth header for auth endpoints (login, register, refresh)
  const isAuthEndpoint = req.url.includes('/api/v1/auth/');

  // Clone request with auth header if we have a token
  let authReq = req;
  const token = authService.accessToken();

  if (token && !isAuthEndpoint) {
    authReq = addAuthHeader(req, token);
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle 401 Unauthorized
      if (error.status === 401 && !isAuthEndpoint) {
        return handleUnauthorizedError(authReq, next, authService, router);
      }

      return throwError(() => error);
    })
  );
};

/**
 * Add Authorization header to request.
 */
function addAuthHeader(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

/**
 * Handle 401 Unauthorized error by attempting token refresh (AC4.6).
 */
function handleUnauthorizedError(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService,
  router: Router
) {
  // If refresh is already in progress, wait for it to complete
  if (authService.isRefreshingToken) {
    return authService.refreshTokenInProgress.pipe(
      filter(inProgress => !inProgress),
      take(1),
      switchMap(() => {
        const token = authService.accessToken();
        if (token) {
          return next(addAuthHeader(req, token));
        }
        // No token after refresh - redirect to login
        router.navigate(['/login']);
        return throwError(() => new Error('Authentication failed'));
      })
    );
  }

  // Attempt to refresh the token
  return authService.refreshToken().pipe(
    switchMap(response => {
      // Refresh succeeded - retry the original request with new token
      return next(addAuthHeader(req, response.accessToken));
    }),
    catchError(refreshError => {
      // Refresh failed - clear auth state and redirect to login
      authService.logout();
      router.navigate(['/login']);
      return throwError(() => refreshError);
    })
  );
}
