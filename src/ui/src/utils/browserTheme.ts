/**
 * browserTheme.ts
 *
 * Utility functions for handling browser theme preferences
 */

const THEME_STORAGE_KEY = 'ccdiary-theme'

/**
 * Determine theme based on stored preference, falling back to system preference
 * @returns 'dark' or 'light'
 */
export function getSystemTheme (): 'dark' | 'light' {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY)
    if (stored === 'dark' || stored === 'light') return stored
    return globalThis.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}

/**
 * Persist the user's theme choice to localStorage
 */
export function saveTheme (theme: 'dark' | 'light'): void {
  try {
    localStorage.setItem(THEME_STORAGE_KEY, theme)
  } catch {
    // storage unavailable — ignore
  }
}
