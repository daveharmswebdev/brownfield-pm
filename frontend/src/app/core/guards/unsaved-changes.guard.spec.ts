import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { unsavedChangesGuard, HasUnsavedChanges } from './unsaved-changes.guard';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

describe('UnsavedChangesGuard', () => {
  let dialogSpy: { open: ReturnType<typeof vi.fn> };
  let mockRoute: ActivatedRouteSnapshot;
  let mockState: RouterStateSnapshot;

  beforeEach(() => {
    dialogSpy = {
      open: vi.fn(),
    };

    mockRoute = {} as ActivatedRouteSnapshot;
    mockState = {} as RouterStateSnapshot;

    TestBed.configureTestingModule({
      providers: [{ provide: MatDialog, useValue: dialogSpy }],
    });
  });

  it('should allow navigation when component has no unsaved changes', () => {
    const mockComponent: HasUnsavedChanges = {
      hasUnsavedChanges: () => false,
    };

    TestBed.runInInjectionContext(() => {
      const result = unsavedChangesGuard(mockComponent, mockRoute, mockState, mockState);

      if (result instanceof Object && 'subscribe' in result) {
        result.subscribe((value) => {
          expect(value).toBe(true);
        });
      } else {
        expect(result).toBe(true);
      }
    });

    expect(dialogSpy.open).not.toHaveBeenCalled();
  });

  it('should allow navigation when component does not implement HasUnsavedChanges', () => {
    const mockComponent = {} as HasUnsavedChanges;

    TestBed.runInInjectionContext(() => {
      const result = unsavedChangesGuard(mockComponent, mockRoute, mockState, mockState);

      if (result instanceof Object && 'subscribe' in result) {
        result.subscribe((value) => {
          expect(value).toBe(true);
        });
      } else {
        expect(result).toBe(true);
      }
    });

    expect(dialogSpy.open).not.toHaveBeenCalled();
  });

  it('should allow navigation when component is null', () => {
    TestBed.runInInjectionContext(() => {
      const result = unsavedChangesGuard(null as unknown as HasUnsavedChanges, mockRoute, mockState, mockState);

      if (result instanceof Object && 'subscribe' in result) {
        result.subscribe((value) => {
          expect(value).toBe(true);
        });
      } else {
        expect(result).toBe(true);
      }
    });

    expect(dialogSpy.open).not.toHaveBeenCalled();
  });

  it('should show confirmation dialog when component has unsaved changes (AC-2.4.3)', () => {
    const mockComponent: HasUnsavedChanges = {
      hasUnsavedChanges: () => true,
    };

    dialogSpy.open.mockReturnValue({
      afterClosed: () => of(true),
    });

    TestBed.runInInjectionContext(() => {
      const result = unsavedChangesGuard(mockComponent, mockRoute, mockState, mockState);

      if (result instanceof Object && 'subscribe' in result) {
        result.subscribe((value) => {
          expect(value).toBe(true);
        });
      }
    });

    expect(dialogSpy.open).toHaveBeenCalled();
  });

  it('should allow navigation when user confirms discard (AC-2.4.3)', () => {
    const mockComponent: HasUnsavedChanges = {
      hasUnsavedChanges: () => true,
    };

    dialogSpy.open.mockReturnValue({
      afterClosed: () => of(true),
    });

    TestBed.runInInjectionContext(() => {
      const result = unsavedChangesGuard(mockComponent, mockRoute, mockState, mockState);

      if (result instanceof Object && 'subscribe' in result) {
        result.subscribe((value) => {
          expect(value).toBe(true);
        });
      }
    });
  });

  it('should block navigation when user cancels dialog (AC-2.4.3)', () => {
    const mockComponent: HasUnsavedChanges = {
      hasUnsavedChanges: () => true,
    };

    dialogSpy.open.mockReturnValue({
      afterClosed: () => of(false),
    });

    TestBed.runInInjectionContext(() => {
      const result = unsavedChangesGuard(mockComponent, mockRoute, mockState, mockState);

      if (result instanceof Object && 'subscribe' in result) {
        result.subscribe((value) => {
          expect(value).toBe(false);
        });
      }
    });
  });

  it('should pass correct dialog data (AC-2.4.3)', () => {
    const mockComponent: HasUnsavedChanges = {
      hasUnsavedChanges: () => true,
    };

    dialogSpy.open.mockReturnValue({
      afterClosed: () => of(true),
    });

    TestBed.runInInjectionContext(() => {
      unsavedChangesGuard(mockComponent, mockRoute, mockState, mockState);
    });

    expect(dialogSpy.open).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        data: expect.objectContaining({
          title: 'Unsaved Changes',
          message: 'You have unsaved changes. Discard changes?',
          confirmText: 'Discard',
          cancelText: 'Cancel',
        }),
        width: '400px',
        disableClose: true,
      })
    );
  });
});
