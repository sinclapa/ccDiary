import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ref } from 'vue'
import { useSearchDebounce } from '../useSearchDebounce'

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
    useSearchDebounce(query, onSearch)

    query.value = 'hello'
    expect(onSearch).not.toHaveBeenCalled()
  })

  it('calls onSearch after the default 300ms delay', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    useSearchDebounce(query, onSearch)

    query.value = 'hello'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('resets the timer when query changes again before delay elapses', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    useSearchDebounce(query, onSearch)

    query.value = 'hel'
    vi.advanceTimersByTime(200)
    query.value = 'hello'
    vi.advanceTimersByTime(200)
    expect(onSearch).not.toHaveBeenCalled()

    vi.advanceTimersByTime(100)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('calls onSearch only once for rapid successive changes', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    useSearchDebounce(query, onSearch)

    query.value = 'a'
    query.value = 'ab'
    query.value = 'abc'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('respects a custom delay', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    useSearchDebounce(query, onSearch, 500)

    query.value = 'test'
    vi.advanceTimersByTime(300)
    expect(onSearch).not.toHaveBeenCalled()

    vi.advanceTimersByTime(200)
    expect(onSearch).toHaveBeenCalledTimes(1)
  })

  it('calls onSearch again for each distinct settled change', async () => {
    const query = ref('')
    const onSearch = vi.fn()
    useSearchDebounce(query, onSearch)

    query.value = 'first'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(1)

    query.value = 'second'
    vi.advanceTimersByTime(300)
    expect(onSearch).toHaveBeenCalledTimes(2)
  })
})
