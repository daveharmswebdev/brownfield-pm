import { HttpClient } from '@angular/common/http';
import { Injectable, inject, computed, signal } from '@angular/core';
import { Observable, tap, catchError, throwError, BehaviorSubject, of } from 'rxjs';

export interface RegisterRequest {
  password: string;
  token: string;
}

export interface RegisterResponse {
  userId: string;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
}

export interface ValidationError {
  type: string;
  title: string;
  status: number;
  errors: Record<string, string[]>;
  traceId: string;
}

export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
  traceId: string;
}

export interface User {
  userId: string;
  accountId: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/auth';

  // Signals for state management (AC4.5)
  private readonly _accessToken = signal<string | null>(null);
  private readonly _currentUser = signal<User | null>(null);
  private readonly _isInitializing = signal<boolean>(true); // AC7.8 - Loading state

  // Token refresh tracking
  private refreshTokenInProgress$ = new BehaviorSubject<boolean>(false);

  // Public readonly signals
  readonly accessToken = this._accessToken.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);
  readonly isInitializing = this._isInitializing.asReadonly(); // AC7.8 - Exposed for guards/components

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.baseUrl}/register`, request);
  }

  verifyEmail(token: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/verify-email`, { token });
  }

  /**
   * Login with email and password (AC4.8).
   * Stores access token in memory (signal) on success.
   * Refresh token is stored as HttpOnly cookie by the server.
   */
  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.baseUrl}/login`,
      { email, password },
      { withCredentials: true } // Include cookies for refresh token
    ).pipe(
      tap(response => {
        this._accessToken.set(response.accessToken);
        // Decode JWT to extract user info
        const user = this.decodeToken(response.accessToken);
        if (user) {
          this._currentUser.set(user);
        }
      })
    );
  }

  /**
   * Refresh access token using refresh token cookie (AC4.6).
   * Called automatically by interceptor when access token expires.
   */
  refreshToken(): Observable<LoginResponse> {
    // Prevent multiple simultaneous refresh requests
    if (this.refreshTokenInProgress$.value) {
      return throwError(() => new Error('Token refresh already in progress'));
    }

    this.refreshTokenInProgress$.next(true);

    return this.http.post<LoginResponse>(
      `${this.baseUrl}/refresh`,
      {},
      { withCredentials: true }
    ).pipe(
      tap(response => {
        this._accessToken.set(response.accessToken);
        const user = this.decodeToken(response.accessToken);
        if (user) {
          this._currentUser.set(user);
        }
        this.refreshTokenInProgress$.next(false);
      }),
      catchError(error => {
        this.refreshTokenInProgress$.next(false);
        // Clear auth state on refresh failure
        this.clearAuthState();
        return throwError(() => error);
      })
    );
  }

  /**
   * Check if token refresh is in progress.
   */
  get isRefreshingToken(): boolean {
    return this.refreshTokenInProgress$.value;
  }

  /**
   * Get the refresh token in progress observable for interceptor coordination.
   */
  get refreshTokenInProgress(): BehaviorSubject<boolean> {
    return this.refreshTokenInProgress$;
  }

  /**
   * Logout - call server-side logout API and clear local state (AC5.4).
   * Server invalidates refresh token and clears cookie.
   * Returns Observable that completes after logout.
   */
  logout(): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/logout`,
      {},
      { withCredentials: true }
    ).pipe(
      tap(() => {
        this.clearAuthState();
      }),
      catchError(error => {
        // Always clear local state even if API call fails
        this.clearAuthState();
        return throwError(() => error);
      })
    );
  }

  /**
   * Clear authentication state.
   */
  private clearAuthState(): void {
    this._accessToken.set(null);
    this._currentUser.set(null);
  }

  /**
   * Check if access token is expired.
   */
  isTokenExpired(): boolean {
    const token = this._accessToken();
    if (!token) return true;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp * 1000; // Convert to milliseconds
      return Date.now() >= exp;
    } catch {
      return true;
    }
  }

  /**
   * Decode JWT token to extract user information.
   */
  private decodeToken(token: string): User | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        userId: payload.userId,
        accountId: payload.accountId,
        role: payload.role,
      };
    } catch {
      return null;
    }
  }

  /**
   * Request a password reset email (AC6.7).
   * Always returns success to prevent email enumeration.
   */
  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/forgot-password`, { email });
  }

  /**
   * Reset password using token from email (AC6.8).
   * On success, user must re-authenticate.
   */
  resetPassword(token: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/reset-password`, { token, newPassword });
  }

  /**
   * Initialize auth state from storage (for page refresh persistence).
   * Since access token is in memory only, this will attempt to refresh.
   * Sets isInitializing to false when complete (AC7.8).
   */
  initializeAuth(): Observable<LoginResponse | null> {
    this._isInitializing.set(true);

    // Try to refresh the token on app initialization
    // This handles the case where the user refreshes the page
    return this.http.post<LoginResponse>(
      `${this.baseUrl}/refresh`,
      {},
      { withCredentials: true }
    ).pipe(
      tap(response => {
        this._accessToken.set(response.accessToken);
        const user = this.decodeToken(response.accessToken);
        if (user) {
          this._currentUser.set(user);
        }
        this._isInitializing.set(false);
      }),
      catchError(() => {
        // Refresh failed - user needs to login
        this.clearAuthState();
        this._isInitializing.set(false);
        return of(null);
      })
    );
  }

  /**
   * Mark initialization as complete without attempting refresh.
   * Used when we know the user is not authenticated.
   */
  markInitialized(): void {
    this._isInitializing.set(false);
  }
}
