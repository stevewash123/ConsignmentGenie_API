import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

export interface ConfirmationDialogData {
  title: string;
  message: string;
  confirmButtonText?: string;
  cancelButtonText?: string;
  isDestructive?: boolean;
  showInput?: boolean;
  inputLabel?: string;
  inputPlaceholder?: string;
  inputValue?: string;
}

export interface ConfirmationResult {
  confirmed: boolean;
  inputValue?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ConfirmationDialogService {
  private dialogSubject = new Subject<ConfirmationDialogData>();
  private resultSubject = new Subject<ConfirmationResult>();

  dialog$ = this.dialogSubject.asObservable();
  result$ = this.resultSubject.asObservable();

  confirm(data: ConfirmationDialogData): Observable<ConfirmationResult> {
    // Set defaults
    const dialogData: ConfirmationDialogData = {
      confirmButtonText: 'Confirm',
      cancelButtonText: 'Cancel',
      isDestructive: false,
      showInput: false,
      ...data
    };

    this.dialogSubject.next(dialogData);

    return new Observable<ConfirmationResult>((observer) => {
      const subscription = this.result$.subscribe((result) => {
        observer.next(result);
        observer.complete();
        subscription.unsubscribe();
      });
    });
  }

  sendResult(result: ConfirmationResult): void {
    this.resultSubject.next(result);
  }

  // Convenience methods for common dialog types
  confirmDelete(itemName: string, customMessage?: string): Observable<ConfirmationResult> {
    return this.confirm({
      title: 'Confirm Delete',
      message: customMessage || `Are you sure you want to delete "${itemName}"? This action cannot be undone.`,
      confirmButtonText: 'Delete',
      isDestructive: true
    });
  }

  confirmAction(title: string, message: string, actionName: string = 'Confirm'): Observable<ConfirmationResult> {
    return this.confirm({
      title,
      message,
      confirmButtonText: actionName,
      isDestructive: false
    });
  }

  confirmWithInput(
    title: string,
    message: string,
    inputLabel: string,
    inputPlaceholder: string = '',
    actionName: string = 'Submit'
  ): Observable<ConfirmationResult> {
    return this.confirm({
      title,
      message,
      confirmButtonText: actionName,
      showInput: true,
      inputLabel,
      inputPlaceholder,
      isDestructive: false
    });
  }

  rejectWithReason(itemName: string): Observable<ConfirmationResult> {
    return this.confirm({
      title: 'Reject Application',
      message: `Are you sure you want to reject ${itemName}?`,
      confirmButtonText: 'Reject',
      isDestructive: true,
      showInput: true,
      inputLabel: 'Reason for rejection (optional):',
      inputPlaceholder: 'Enter the reason for rejection...'
    });
  }
}