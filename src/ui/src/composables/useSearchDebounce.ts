import { type Ref, watch } from 'vue'

export function useSearchDebounce (query: Ref<string>, onSearch: () => void, delay = 300) {
  let timer: ReturnType<typeof setTimeout> | null = null
  watch(query, () => {
    if (timer) clearTimeout(timer)
    timer = setTimeout(onSearch, delay)
  })
}
