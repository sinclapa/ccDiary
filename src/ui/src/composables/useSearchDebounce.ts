import { onScopeDispose, type Ref, watch } from 'vue'

export function useSearchDebounce (query: Ref<string>, onSearch: () => void, delay = 300) {
  let timer: ReturnType<typeof setTimeout> | null = null

  watch(query, () => {
    if (timer) clearTimeout(timer)
    timer = setTimeout(onSearch, delay)
  }, { flush: 'sync' })

  // The watcher stops itself when the scope is disposed, but a timer already in flight
  // does not. Without this, typing in the search box and navigating away within the
  // delay still fires onSearch against a torn-down page — which writes to its refs and,
  // via the search request, to the shared apiStatus store, so a failure there raises the
  // global status banner on whatever page the user actually landed on.
  onScopeDispose(() => {
    if (timer) clearTimeout(timer)
  })
}
