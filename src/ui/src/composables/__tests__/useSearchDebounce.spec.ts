import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { effectScope, ref } from 'vue'
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

  it('does not call onSearch immediately when query changes', () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'hello'
    expect(onSearch).not.toHaveBeenCalled()
  })

  it('calls onSearch after the default 300ms delay', () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'hello'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('resets the timer when query changes again before delay elapses', () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'hel'
    vi.advanceTimersByTime(200)
    query.value = 'hello'
    vi.advanceTimersByTime(200)
    expect(onSearch).not.toHaveBeenCalled()

    vi.advanceTimersByTime(100)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('calls onSearch only once for rapid successive changes', () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'a'
    query.value = 'ab'
    query.value = 'abc'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('respects a custom delay', () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch, 500))

    query.value = 'test'
    vi.advanceTimersByTime(300)
    expect(onSearch).not.toHaveBeenCalled()

    vi.advanceTimersByTime(200)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('calls onSearch again for each distinct settled change', () => {
    const query = ref('')
    const onSearch = vi.fn()
    inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'first'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)

    query.value = 'second'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(2)
  })

  it('cancels a pending search when the scope is disposed', () => {
    // Navigating away mid-typing. The watcher stops itself, but before this fix the
    // timer already in flight still fired, running the search against a page that no
    // longer exists and raising the shared status banner on the new one.
    const query = ref('')
    const onSearch = vi.fn()
    const scope = inScope(() => useSearchDebounce(query, onSearch))

    query.value = 'partial'
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
