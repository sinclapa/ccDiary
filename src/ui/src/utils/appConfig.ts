export function getAppConfigField (fieldName: string, options?: { placeholder?: string; defaultValue?: string }) {
  const placeholder = options?.placeholder ?? '__PLACEHOLDER__'
  const defaultValue = options?.defaultValue ?? 'NOT_SET'

  const win = (globalThis as any).APP_CONFIG
  const winVal = win?.[fieldName]
  if (winVal && winVal !== placeholder) return winVal

  const env = (import.meta.env as any)[fieldName]
  return env ?? defaultValue
}
