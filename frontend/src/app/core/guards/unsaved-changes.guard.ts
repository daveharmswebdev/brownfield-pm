import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../shared/components/confirm-dialog/confirm-dialog.component';

/**
 * Interface for components that track unsaved changes (AC-2.4.3)
 */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/**
 * Guard to prevent navigation when component has unsaved changes (AC-2.4.3)
 *
 * Shows a confirmation dialog when:
 * - User clicks Cancel with unsaved changes
 * - User uses browser back button with unsaved changes
 * - User navigates away via any route change
 *
 * Returns Observable<boolean>:
 * - true: Allow navigation (no changes or user confirmed discard)
 * - false: Block navigation (user cancelled dialog)
 */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (
  component
): Observable<boolean> => {
  // If component doesn't implement the interface or has no changes, allow navigation
  if (!component?.hasUnsavedChanges || !component.hasUnsavedChanges()) {
    return of(true);
  }

  // Component has unsaved changes - show confirmation dialog
  const dialog = inject(MatDialog);

  const dialogData: ConfirmDialogData = {
    title: 'Unsaved Changes',
    message: 'You have unsaved changes. Discard changes?',
    confirmText: 'Discard',
    cancelText: 'Cancel',
  };

  const dialogRef = dialog.open(ConfirmDialogComponent, {
    data: dialogData,
    width: '400px',
    disableClose: true,
  });

  return dialogRef.afterClosed().pipe(
    map((result) => result === true)
  );
};
