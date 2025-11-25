import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private loadingKeys = new BehaviorSubject<Set<string>>(new Set());

  /**
   * Start loading for a specific key
   */
  start(key: string): void {
    const current = this.loadingKeys.value;
    current.add(key);
    this.loadingKeys.next(new Set(current));
  }

  /**
   * Stop loading for a specific key
   */
  stop(key: string): void {
    const current = this.loadingKeys.value;
    current.delete(key);
    this.loadingKeys.next(new Set(current));
  }

  /**
   * Check if a specific key is loading
   */
  isLoading(key: string): boolean {
    return this.loadingKeys.value.has(key);
  }

  /**
   * Observable for a specific key's loading state
   */
  isLoading$(key: string): Observable<boolean> {
    return this.loadingKeys.pipe(
      map(keys => keys.has(key))
    );
  }

  /**
   * Check if anything is loading
   */
  get anyLoading(): boolean {
    return this.loadingKeys.value.size > 0;
  }

  /**
   * Observable for any loading state
   */
  get anyLoading$(): Observable<boolean> {
    return this.loadingKeys.pipe(
      map(keys => keys.size > 0)
    );
  }

  /**
   * Get all current loading keys (for debugging)
   */
  get activeKeys(): string[] {
    return Array.from(this.loadingKeys.value);
  }

  /**
   * Clear all loading states (useful for error recovery)
   */
  clear(): void {
    this.loadingKeys.next(new Set());
  }
}