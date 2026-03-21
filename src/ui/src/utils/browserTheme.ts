/**
 * browserTheme.ts
 *
 * Utility functions for handling browser theme preferences
 */

/**
 * Determine theme based on system preference
 * @returns 'dark' or 'light'
 */
export function getSystemTheme(): 'dark' | 'light' {
  try {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}