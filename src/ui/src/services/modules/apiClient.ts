import { getAppConfigField } from '@/utils/appConfig'

/**
 * Shared HTTP plumbing for the API service modules.
 *
 * The API runs on Container Apps with no minimum replicas, so a request that arrives after
 * an idle period is answered by the ingress — with a gateway error — while a container
 * starts. Every call therefore needs to tell "the server is still waking" apart from "the
 * server said no", and retry the first without bothering the user about it.
 */

/** Gateway responses returned by the ingress while no replica is serving yet. */
const UPSTREAM_UNAVAILABLE = new Set([502, 503, 504])

/**
 * Thrown when the API could not be reached, as opposed to answering.
 *
 * The distinction matters wherever an empty answer is meaningful: a caller that cannot tell
 * "you have no account" from "the server is still starting" will show the first when it
 * means the second.
 */
export class ApiUnavailableError extends Error {
  constructor (public readonly status: number) {
    super(`The API is unavailable (HTTP ${status}).`)
    this.name = 'ApiUnavailableError'
  }
}

/** True when a response means the ingress could not reach a running replica. */
export function isUnavailable (response: Response): boolean {
  return UPSTREAM_UNAVAILABLE.has(response.status)
}

const MAX_ATTEMPTS = 4
const BASE_BACKOFF_MS = 600

const IDEMPOTENT = new Set(['GET', 'HEAD', 'OPTIONS'])

const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

/** Builds an absolute URL for an API path, relative to the configured base. */
export function apiUrl (path: string): URL {
  return new URL(path, getAppConfigField('VITE_API'))
}

/**
 * Fetches from the API, retrying while the container is still coming up.
 *
 * A gateway error means the ingress never handed the request to the application, so it is
 * safe to retry whatever the method. A thrown error is less certain — the request may have
 * been sent and the response lost — so only idempotent methods are retried on one of those.
 */
export async function apiFetch (input: URL | string, init?: RequestInit): Promise<Response> {
  const method = (init?.method ?? 'GET').toUpperCase()
  const retryOnThrow = IDEMPOTENT.has(method)

  let lastError: unknown

  for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
    const lastAttempt = attempt === MAX_ATTEMPTS

    try {
      // Resolved through globalThis on every call, deliberately: the auth layer replaces
      // globalThis.fetch to attach the bearer token, and capturing it here would pin the
      // unwrapped original and send every request anonymously.
      const response = init === undefined
        ? await globalThis.fetch(input)
        : await globalThis.fetch(input, init)
      if (!UPSTREAM_UNAVAILABLE.has(response.status) || lastAttempt) {
        return response
      }
    } catch (error) {
      if (!retryOnThrow || lastAttempt) throw error
      lastError = error
    }

    await delay(BASE_BACKOFF_MS * 2 ** (attempt - 1))
  }

  // Unreachable: the loop either returns or throws on its final attempt.
  throw lastError
}

/**
 * Fetches JSON, returning the fallback when the API answers with anything but success.
 *
 * The status check is the point. Parsing unconditionally turned an error response into a
 * JSON parse rejection, which the pages swallowed into an empty list — so an API that was
 * merely still starting looked like an account with no diaries in it.
 */
export async function apiFetchJson<T> (input: URL | string, fallback: T, init?: RequestInit): Promise<T> {
  const response = await apiFetch(input, init)
  if (!response.ok) return fallback
  return await response.json() as T
}
