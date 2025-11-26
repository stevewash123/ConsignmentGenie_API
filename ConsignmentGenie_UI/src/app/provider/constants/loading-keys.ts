/**
 * Centralized loading keys for the provider module
 * Using constants prevents typos and enables autocomplete
 */
export const LOADING_KEYS = {
  // Statement Detail
  STATEMENT: 'provider:statement',
  STATEMENT_PDF: 'provider:statement-pdf',
  STATEMENT_REGENERATE: 'provider:statement-regenerate',

  // Statements List
  STATEMENTS_LIST: 'provider:statements-list',

  // Notifications
  NOTIFICATIONS: 'provider:notifications',
  NOTIFICATION_MARK_READ: 'provider:notification-mark-read',
  NOTIFICATION_DELETE: 'provider:notification-delete',

  // Notification Preferences
  NOTIFICATION_PREFS: 'provider:notification-prefs',
  NOTIFICATION_PREFS_SAVE: 'provider:notification-prefs-save',
} as const;

export type LoadingKey = typeof LOADING_KEYS[keyof typeof LOADING_KEYS];