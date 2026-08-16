import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, nextTick, ref } from 'vue'
import { useSearchDebounce } from '../useSearchDebounce'

// The composable registers an onScopeDispose handler, so it has to run inside an effect
// scope — which is what happens in a component anyway. Running it bare would both warn
// and leave the disposal path untestable.
function inScope (register: () => void) {
  const scope = effectScope()
  scope.run(register)
  return scope
}

describe('useSearchDebounce', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('does not call onSearch immediately when query changes', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'hello'
    await nextTick()
    expect(onSearch).not.toHaveBeenCalled()
  })

  it('calls onSearch after the default 300ms delay', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'hello'
    await nextTick()
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('resets the timer when query changes again before delay elapses', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'hel'
    await nextTick()
    vi.advanceTimersByTime(200)
    query.value = 'hello'
    await nextTick()
    vi.advanceTimersByTime(200)
    expect(onSearch).not.toHaveBeenCalled()

    vi.advanceTimersByTime(100)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('calls onSearch only once for rapid successive changes', async () => {
    // Keystrokes land in the same tick, so the default 'pre' flush coalesces them into a
    // single watcher run — the timer is set once rather than cleared and reset per key.
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'a'
    query.value = 'ab'
    query.value = 'abc'
    await nextTick()
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('respects a custom delay', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch, 500))

    query.value = 'test'
    await nextTick()
    vi.advanceTimersByTime(300)
    expect(onSearch).not.toHaveBeenCalled()

    vi.advanceTimersByTime(200)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('calls onSearch again for each distinct settled change', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'first'
    await nextTick()
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)

    query.value = 'second'
    await nextTick()
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(2)
  })

  it('cancels a pending search when the scope is disposed', async () => {
    // Navigating away mid-typing. The watcher stops itself, but before this fix the
    // timer already in flight still fired, running the search against a page that no
    // longer exists and raising the shared status banner on the new one.
    const query = ref('')
    const onSearch = vi.fn()
    const scope = inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'partial'
    await nextTick()
    vi.advanceTimersByTime(100)
    scope.stop()
    vi.advanceTimersByTime(1000)

    expect(onSearch).not.toHaveBeenCalled()
  })

  it('does not throw when disposed with no search pending', () => {
    const query = ref('')
    const onSearch = vi.fn()
    const scope = inScope(() => useSearchDebounce(query, onSearch))

    scope.stop()

    expect(onSearch).not.toHaveBeenCalled()
  })
})
