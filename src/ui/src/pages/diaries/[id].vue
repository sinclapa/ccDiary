<template>
  <v-container style="overflow-y: visible">
    <v-row>
      <v-progress-linear
        :active="loading"
        color="primary"
        height="2"
        indeterminate
      />
    </v-row>
    <v-row class="align-center mb-2">
      <v-col class="py-0">
        <span class="title">{{ diary?.title }}&nbsp;</span>
        <span class="author">&nbsp;by {{ diary?.author }}</span>
      </v-col>
      <v-col class="py-0 flex-grow-0 d-flex align-center" style="gap: 0">
        <v-expand-x-transition>
          <v-text-field
            v-if="searchExpanded && !display.mobile.value"
            v-model="entrySearch"
            autofocus
            class="search-inline mr-2"
            clearable
            density="compact"
            hide-details
            placeholder="Search entries…"
            variant="outlined"
            @click:clear="collapseEntrySearch"
          />
        </v-expand-x-transition>
        <v-btn
          aria-label="Search entries"
          :color="searchExpanded ? 'primary' : undefined"
          icon="$mdi-magnify"
          size="small"
          :variant="searchExpanded ? 'tonal' : 'text'"
          @click="toggleEntrySearch"
        />
      </v-col>
    </v-row>

    <v-expand-transition>
      <v-text-field
        v-if="searchExpanded && display.mobile.value"
        v-model="entrySearch"
        autofocus
        class="mb-4"
        clearable
        density="compact"
        hide-details
        placeholder="Search entries…"
        variant="outlined"
        @click:clear="collapseEntrySearch"
      />
    </v-expand-transition>

    <DiaryEntrySearchResults
      v-if="entrySearch"
      :loading="searchLoading"
      :page="searchPage"
      :page-size="searchPageSize"
      :results="searchResults"
      :search="entrySearch"
      @select="goToSearchResult"
      @update:page="searchPage = $event"
    />

    <v-row v-else>
      <v-col class="calendar-col" cols="auto">
        <v-row>
          <v-date-picker
            v-model="selectedDate"
            :max="maxDate"
            :max-height="datePickerHeight"
            :min="minDate"
            :month="calendarMonth"
            :year="calendarYear"
            @update:model-value="onCalendarSelectDateTracked"
            @update:month="updateMonth"
            @update:year="updateYear"
          >
            <template #title>
              <v-btn
                :aria-label="isDatePickerExpanded ? 'Collapse date picker' : 'Expand date picker'"
                color="primary"
                size="small"
                :variant="isDatePickerExpanded ? 'flat' : 'outlined'"
                @click="onToggleDatePickerHeight"
              >
                <v-icon :icon="isDatePickerExpanded ? '$mdi-chevron-up' : '$mdi-chevron-down'" />
                {{ isDatePickerExpanded ? 'Compact View' : 'Expanded View' }}
              </v-btn>
            </template>
            <template #header="{ transition }">
              <v-date-picker-header
                :header="selectedDate ? dayjs(selectedDate).format('ddd D MMM YYYY') : ''"
                :transition="transition"
              />
            </template>
            <template #day="{ item, props }">
              <div class="diary-day-content">
                <v-btn v-bind="props" :color="item.isoDate === selectedDateIso ? 'primary' : undefined" />
                <span v-if="hasDiaryEntryOnDate(item.isoDate)" class="diary-day-marker" />
              </div>
            </template>
          </v-date-picker>
        </v-row>
        <v-row class="pt-1">
          <v-col class="pa-0" style="display: grid; grid-template-columns: repeat(5, minmax(0, 1fr));">
            <v-btn
              aria-label="Go to start"
              color="primary"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(minDate).format('YYYY-MM-DD')"
              rounded="sm"
              size="default"
              style="min-width: 0; width: 100%"
              variant="outlined"
              @click="onMoveStart()"
            >
              <v-icon>$mdi-skip-backward</v-icon>
            </v-btn>
            <v-btn
              aria-label="Move backward"
              color="primary"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(minDate).format('YYYY-MM-DD')"
              rounded="sm"
              size="default"
              style="min-width: 0; width: 100%"
              variant="outlined"
              @click="onMoveBackward()"
            >
              <v-icon>$mdi-rewind</v-icon>
            </v-btn>
            <v-btn
              v-if="canEditDiary"
              aria-label="Add entry"
              color="primary"
              rounded="sm"
              size="default"
              style="min-width: 0; width: 100%"
              variant="outlined"
              @click="onAddEntry()"
            >
              Add
            </v-btn>
            <span v-else aria-hidden="true" />
            <v-btn
              aria-label="Move forward"
              color="primary"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(maxDate).format('YYYY-MM-DD')"
              rounded="sm"
              size="default"
              style="min-width: 0; width: 100%"
              variant="outlined"
              @click="onMoveForward()"
            >
              <v-icon>$mdi-fast-forward</v-icon>
            </v-btn>
            <v-btn
              aria-label="Go to end"
              color="primary"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(maxDate).format('YYYY-MM-DD')"
              rounded="sm"
              size="default"
              style="min-width: 0; width: 100%"
              variant="outlined"
              @click="onMoveEnd()"
            >
              <v-icon>$mdi-skip-forward</v-icon>
            </v-btn>
          </v-col>
        </v-row>
        <v-dialog
          v-model="dialog"
          max-width="560px"
          scrim-clickable
        >
          <diary-entry-editor
            :date="editedItem.date"
            :entry="editedItem.entry"
            :from-location="editedItem.fromLocation"
            :image-content-type="editedItem.imageContentType"
            :image-data="editedItem.imageData"
            :is-edit="editedItem.diaryEntryId !== undefined"
            :journey-mode="editedItem.journeyMode"
            :location="editedItem.location"
            :map-location="editedItem.mapLocation"
            :show-journey="editedItem.showJourney"
            :show-map="editedItem.showMap"
            :to-location="editedItem.toLocation"
            @close="close"
            @submit="onSubmitDiaryEntry"
          />
        </v-dialog>
        <ConfirmDeleteDialog
          v-model="dialogDelete"
          confirm-label="Delete Entry"
          item-type="diary entry"
          :items="[
            { label: 'Date', value: editedItem?.date ? dayjs(editedItem.date).format('ddd D MMM YYYY') : 'Unknown date' },
            { label: 'Time', value: editedItem?.date ? dayjs(editedItem.date).format('HH:mm') : 'Unknown time' },
            { label: 'Location', value: editedItem?.location || 'Unknown location' },
          ]"
          title="Delete Diary Entry"
          @cancel="closeDelete"
          @confirm="deleteItemConfirm"
        />
      </v-col>
      <v-col class="timeline-col pr-0">
        <DiaryTimeline
          :can-edit="canEditDiary"
          :entries="diaryEntries ?? []"
          @delete="onDeleteEntry"
          @edit="onEditEntry"
        />
      </v-col>
    </v-row><!-- end v-else calendar/timeline row -->

    <v-row v-if="!entrySearch" class="mt-6">
      <v-col class="d-flex align-center justify-space-between" cols="12">
        <v-btn
          aria-label="Previous day"
          class="day-nav-btn"
          :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(minDate).format('YYYY-MM-DD')"
          variant="text"
          @click="onMoveBackward()"
        >
          ← Previous
        </v-btn>
        <v-btn
          aria-label="Next day"
          class="day-nav-btn"
          :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(maxDate).format('YYYY-MM-DD')"
          variant="text"
          @click="onMoveForward()"
        >
          Next →
        </v-btn>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="auto">
        <v-btn
          aria-label="Back to Diaries"
          class="back-to-diaries-btn"
          color="primary"
          variant="outlined"
          @click="onBackToDiaries"
        >
          ← Back to Diaries
        </v-btn>
      </v-col>
    </v-row>

  </v-container>

</template>
<script setup lang="ts">
  import { useDisplay } from 'vuetify'
  import { diaryAPI } from '@/services/modules/diaryService'
  import { diaryEntryAPI } from '@/services/modules/diaryEntryService'
  import Diary from '@/services/models/diary'
  import DiaryEntry from '@/services/models/diaryEntry'
  import PagedResult from '@/services/models/pagedResult'
  import { useAuthStore } from '@/stores/auth'
  import dayjs from 'dayjs'
  import { useApiStatusStore } from '@/stores/apiStatus'
  import { endFaroUserAction, startFaroUserAction } from '@/plugins/faro'
  import { useSearchDebounce } from '@/composables/useSearchDebounce'

  const authStore = useAuthStore()

  const apiStatus = useApiStatusStore()
  const router = useRouter()
  const loading = ref(false)

  // Detect if the device is mobile
  const dialog = ref(false)
  const dialogDelete = ref(false)
  const selectedDate = ref<Date>()
  const diaryEntries = ref<DiaryEntry[] | null>()
  const route = useRoute('/diaries/[id]')
  const diaryId = route.params.id
  const diary = ref(new Diary('', '', '') as Diary | undefined)

  const selectedDateIso = computed(() =>
    selectedDate.value ? dayjs(selectedDate.value).format('YYYY-MM-DD') : null
  )

  const canEditDiary = computed(() => {
    if (authStore.isAdmin) return true
    if (!authStore.isContributor) return false
    return diary.value?.ownerId === authStore.appUser?.entraObjectId
  })
  const defaultItem = ref<DiaryEntry>(new DiaryEntry(diaryId, new Date(Date.now()), '', ''))
  const editedItem = ref<DiaryEntry>(new DiaryEntry(diaryId, new Date(Date.now()), '', ''))
  const minDate = ref<Date>()
  const maxDate = ref<Date>()
  const isDatePickerExpanded = ref(window.innerWidth >= 600)
  const calendarMonth = ref<number>()
  const calendarYear = ref<number>()
  const visibleMonth = ref<number>()
  const visibleYear = ref<number>()
  const markedDays = ref<number[]>([])
  const latestMarkedDaysRequest = ref(0)
  const display = useDisplay()
  const entrySearch = ref('')
  const searchExpanded = ref(false)
  const searchResults = ref<PagedResult<DiaryEntry> | null>(null)
  const searchPage = ref(1)
  const searchLoading = ref(false)
  const searchPageSize = computed(() => display.mobile.value ? 8 : 10)

  function toggleEntrySearch () {
    searchExpanded.value = !searchExpanded.value
    if (!searchExpanded.value) {
      entrySearch.value = ''
      searchResults.value = null
    }
  }

  function collapseEntrySearch () {
    entrySearch.value = ''
    searchResults.value = null
    searchExpanded.value = false
  }

  // Computed height
  const datePickerHeight = computed(() =>
    isDatePickerExpanded.value ? 500 : 130
  )

  // Toggle function with persistence
  const toggleDatePickerHeight = () => {
    isDatePickerExpanded.value = !isDatePickerExpanded.value
    localStorage.setItem('id.datePickerExpanded',
                         isDatePickerExpanded.value.toString())
  }

  const onToggleDatePickerHeight = () => {
    startFaroUserAction('diary-datepicker-toggle', { expanded: String(!isDatePickerExpanded.value) })
    try {
      toggleDatePickerHeight()
    } finally {
      endFaroUserAction()
    }
  }

  const onMoveStart = async () => {
    startFaroUserAction('diary-navigation-start')
    try {
      await moveStart()
    } finally {
      endFaroUserAction()
    }
  }

  const onMoveBackward = async () => {
    startFaroUserAction('diary-navigation-backward')
    try {
      await moveBackward()
    } finally {
      endFaroUserAction()
    }
  }

  const onMoveForward = async () => {
    startFaroUserAction('diary-navigation-forward')
    try {
      await moveForward()
    } finally {
      endFaroUserAction()
    }
  }

  const onMoveEnd = async () => {
    startFaroUserAction('diary-navigation-end')
    try {
      await moveEnd()
    } finally {
      endFaroUserAction()
    }
  }

  const onBackToDiaries = async () => {
    startFaroUserAction('diary-navigation-back-to-list')
    try {
      await router.push('/diaries')
    } finally {
      endFaroUserAction()
    }
  }

  const onAddEntry = async () => {
    startFaroUserAction('diary-entry-add')
    try {
      await editItem()
    } finally {
      endFaroUserAction()
    }
  }

  const onEditEntry = async (item: DiaryEntry) => {
    startFaroUserAction('diary-entry-edit')
    try {
      await editItem(item)
    } finally {
      endFaroUserAction()
    }
  }

  const onDeleteEntry = async (item: DiaryEntry) => {
    startFaroUserAction('diary-entry-delete')
    try {
      await deleteItem(item)
    } finally {
      endFaroUserAction()
    }
  }

  const onCalendarSelectDateTracked = async (date: any) => {
    startFaroUserAction('diary-date-select')
    try {
      await onCalendarSelectDate(date)
    } finally {
      endFaroUserAction()
    }
  }

  async function loadDiary (diaryId: string) {
    diary.value = await diaryAPI.getDiary(diaryId)
  }

  function normalizeMonth (month: number) : number {
    if (month >= 0 && month <= 11) {
      return month + 1
    }
    return month
  }

  async function refreshMarkedDays (year: number, month: number) {
    const requestId = ++latestMarkedDaysRequest.value
    const response = await diaryEntryAPI.searchDiaryEntry(diaryId, year, month) ?? []
    if (requestId === latestMarkedDaysRequest.value) {
      markedDays.value = response
    }
  }

  async function refreshMarkedDaysForVisibleMonth () {
    if (visibleYear.value === undefined || visibleMonth.value === undefined) {
      return
    }
    await refreshMarkedDays(visibleYear.value, visibleMonth.value)
  }

  function getDatePart (input: unknown) : number | null {
    if (input instanceof Date) {
      return input.getDate()
    }
    if (typeof input === 'string' || typeof input === 'number') {
      const parsed = dayjs(input)
      return parsed.isValid() ? parsed.date() : null
    }
    return null
  }

  function hasDiaryEntryOnDate (date: unknown) : boolean {
    const day = getDatePart(date)
    return day !== null && markedDays.value.includes(day)
  }

  async function loadCalendar (diaryId: string) : Promise<Date> {
    maxDate.value = await diaryEntryAPI.getMaxDate(diaryId)
    const minDiaryEntryDate = await diaryEntryAPI.getMinDate(diaryId)
    minDate.value = new Date(minDiaryEntryDate.getFullYear(), minDiaryEntryDate.getMonth(), minDiaryEntryDate.getDate())
    return minDate.value
  }

  function close () {
    dialog.value = false
    nextTick(() => {
      editedItem.value = { ...defaultItem.value }
    })
  }

  async function editItem (item?: DiaryEntry) {
    if (item === undefined) {
      let date = selectedDate.value ?? new Date()
      let location = ''
      if (diaryEntries.value && diaryEntries.value.length > 0) {
        date = diaryEntries.value[diaryEntries.value.length - 1].date
        location = diaryEntries.value[diaryEntries.value.length - 1].location
      }
      editedItem.value = new DiaryEntry(diaryId, date, location, '')
    } else {
      editedItem.value = { ...item }
    }
    dialog.value = true
  }

  async function onSubmitDiaryEntry (payload: {date: Date, location: string, entry: string, mapLocation: string, showMap: boolean, fromLocation: string, toLocation: string, showJourney: boolean, journeyMode: DiaryEntry['journeyMode'], imageData: string | undefined, imageContentType: string | undefined}) {
    editedItem.value.date = payload.date
    editedItem.value.location = payload.location
    editedItem.value.entry = payload.entry
    editedItem.value.mapLocation = payload.mapLocation
    editedItem.value.showMap = payload.showMap
    editedItem.value.fromLocation = payload.fromLocation
    editedItem.value.toLocation = payload.toLocation
    editedItem.value.showJourney = payload.showJourney
    editedItem.value.journeyMode = payload.journeyMode
    editedItem.value.imageData = payload.imageData
    editedItem.value.imageContentType = payload.imageContentType
    if (editedItem.value.diaryEntryId === undefined) {
      await diaryEntryAPI.createDiaryEntry(editedItem.value)
    } else {
      await diaryEntryAPI.updateDiaryEntry(editedItem.value)
    }
    await loadCalendar(diaryId)
    await refreshMarkedDaysForVisibleMonth()
    if (editedItem.value.date.toDateString() === selectedDate.value?.toDateString()) {
      selectDate(selectedDate.value)
    }
    close()
  }

  async function deleteItem (item: DiaryEntry) {
    editedItem.value = { ...item }
    dialogDelete.value = true
  }

  async function moveForward () {
    let current = dayjs(selectedDate.value)
    const max = dayjs(maxDate.value)
    let loadedYear = visibleYear.value ?? current.year()
    let loadedMonth = visibleMonth.value ?? normalizeMonth(current.month())

    while (true) {
      current = current.add(1, 'day')

      const reachedMax = !current.isBefore(max, 'day')
      if (reachedMax) current = max

      const yr = current.year()
      const mo = normalizeMonth(current.month())

      if (yr !== loadedYear || mo !== loadedMonth) {
        loadedYear = yr
        loadedMonth = mo
        await refreshMarkedDays(yr, mo)
      }

      if (reachedMax || markedDays.value.includes(current.date())) {
        selectedDate.value = current.toDate()
        calendarMonth.value = current.month()
        calendarYear.value = current.year()
        await selectDate(selectedDate.value)
        setDateInUrl(selectedDate.value, true)
        return
      }
    }
  }

  async function moveBackward () {
    let current = dayjs(selectedDate.value)
    const min = dayjs(minDate.value)
    let loadedYear = visibleYear.value ?? current.year()
    let loadedMonth = visibleMonth.value ?? normalizeMonth(current.month())

    while (true) {
      current = current.subtract(1, 'day')

      const reachedMin = !current.isAfter(min, 'day')
      if (reachedMin) current = min

      const yr = current.year()
      const mo = normalizeMonth(current.month())

      if (yr !== loadedYear || mo !== loadedMonth) {
        loadedYear = yr
        loadedMonth = mo
        await refreshMarkedDays(yr, mo)
      }

      if (reachedMin || markedDays.value.includes(current.date())) {
        selectedDate.value = current.toDate()
        calendarMonth.value = current.month()
        calendarYear.value = current.year()
        await selectDate(selectedDate.value)
        setDateInUrl(selectedDate.value, true)
        return
      }
    }
  }

  async function moveStart () {
    selectedDate.value = dayjs(minDate.value).startOf('day').toDate()
    await selectDate(selectedDate.value)
    setDateInUrl(selectedDate.value, true)
  }

  async function moveEnd () {
    selectedDate.value = dayjs(maxDate.value).endOf('day').toDate()
    await selectDate(selectedDate.value)
    setDateInUrl(selectedDate.value, true)
  }

  function closeDelete () {
    dialogDelete.value = false
    nextTick(() => {
      editedItem.value = { ...defaultItem.value }
    })
  }

  async function deleteItemConfirm () {
    if (editedItem.value.diaryEntryId !== undefined) {
      await diaryEntryAPI.deleteDiaryEntry(editedItem.value.diaryEntryId)
      await loadCalendar(diaryId)
      await refreshMarkedDaysForVisibleMonth()
      if (editedItem.value.date.toDateString() === selectedDate.value?.toDateString()) {
        selectDate(selectedDate.value)
      }
    }
    closeDelete()
  }

  function setDateInUrl (date: Date, replace: boolean) {
    const dateStr = dayjs(date).format('YYYY-MM-DD')
    if (route.query.date === dateStr) return
    localStorage.setItem(`diary.${route.params.id}.lastDate`, dateStr)
    const query = { ...route.query, date: dateStr }
    if (replace) {
      router.replace({ query })
    } else {
      router.push({ query })
    }
  }

  async function selectDate (date: any) {
    loading.value = true
    try {
      diaryEntries.value = await diaryEntryAPI.searchDiaryEntryForDay(diaryId, date.getFullYear(), date.getMonth() + 1, date.getDate())
    } catch {
      // API unavailable — ApiStatusBanner surfaces this to the user
    } finally {
      loading.value = false
    }
  }

  async function onCalendarSelectDate (date: any) {
    await selectDate(date)
    setDateInUrl(date, false)
  }

  async function updateMonth (month: number | string | undefined) {
    if (month === undefined) {
      return
    }
    calendarMonth.value = Number(month)
  }

  async function updateYear (year: number | string | undefined) {
    if (year === undefined) {
      return
    }
    calendarYear.value = Number(year)
  }

  async function runEntrySearch () {
    if (!entrySearch.value) {
      searchResults.value = null
      return
    }
    searchLoading.value = true
    try {
      searchResults.value = await diaryEntryAPI.textSearchDiaryEntries(diaryId, entrySearch.value, searchPage.value, searchPageSize.value)
    } catch {
      // API unavailable — ApiStatusBanner surfaces this to the user
    } finally {
      searchLoading.value = false
    }
  }

  async function goToSearchResult (entry: DiaryEntry) {
    entrySearch.value = ''
    searchResults.value = null
    searchExpanded.value = false
    const date = new Date(entry.date)
    selectedDate.value = date
    calendarMonth.value = dayjs(date).month()
    calendarYear.value = dayjs(date).year()
    await selectDate(date)
    setDateInUrl(date, false)
  }

  useSearchDebounce(entrySearch, () => { searchPage.value = 1; runEntrySearch() })

  watch(searchPage, () => runEntrySearch())
  watch(searchPageSize, () => {
    searchPage.value = 1
    runEntrySearch()
  })

  watch([calendarYear, calendarMonth], async ([year, month]) => {
    if (year === undefined || month === undefined) {
      return
    }
    visibleYear.value = Number(year)
    visibleMonth.value = normalizeMonth(Number(month))
    await refreshMarkedDays(visibleYear.value, visibleMonth.value)
  })

  watch(selectedDate, async newDate => {
    if (newDate === undefined) {
      return
    }
    const nextMonth = dayjs(newDate).month()
    const nextYear = dayjs(newDate).year()
    if (nextMonth !== calendarMonth.value || nextYear !== calendarYear.value) {
      calendarMonth.value = nextMonth
      calendarYear.value = nextYear
    }
  })

  watch(() => route.query.date, async dateStr => {
    if (!dateStr || typeof dateStr !== 'string') return
    const parsed = dayjs(dateStr, 'YYYY-MM-DD', true)
    if (!parsed.isValid()) return
    const newDate = parsed.toDate()
    if (selectedDate.value && dayjs(newDate).isSame(dayjs(selectedDate.value), 'day')) return
    selectedDate.value = newDate
    calendarMonth.value = dayjs(newDate).month()
    calendarYear.value = dayjs(newDate).year()
    await selectDate(newDate)
  })

  function resolveDateFromParam (
    dateParam: unknown,
    minDate: Date | null,
    maxDate: Date | null | undefined,
  ): Date | null {
    if (!dateParam || typeof dateParam !== 'string') return null
    const parsed = dayjs(dateParam, 'YYYY-MM-DD', true)
    if (!parsed.isValid()) return null
    const paramDate = parsed.toDate()
    if (maxDate && paramDate > maxDate) return maxDate
    if (minDate && paramDate < minDate) return null
    return paramDate
  }

  function resolveDateFromStorage (
    id: string,
    minDate: Date | null,
    maxDate: Date | null | undefined,
  ): Date | null {
    const storedDate = localStorage.getItem(`diary.${id}.lastDate`)
    if (!storedDate || !dayjs(storedDate, 'YYYY-MM-DD', true).isValid()) return null
    const stored = dayjs(storedDate).toDate()
    if (maxDate && stored > maxDate) return maxDate
    if (minDate && stored < minDate) return minDate
    return stored
  }

  function loadDiaryData () {
    loading.value = true
    loadDiary(diaryId)
    loadCalendar(diaryId).then(async x => {
      const startDate = resolveDateFromParam(route.query.date, x, maxDate.value) ??
        resolveDateFromStorage(diaryId, x, maxDate.value) ??
        x
      selectedDate.value = startDate
      calendarMonth.value = dayjs(startDate).month()
      calendarYear.value = dayjs(startDate).year()
      await selectDate(selectedDate.value)
      setDateInUrl(selectedDate.value, true)
    }).catch(() => {
      // API unavailable — ApiStatusBanner surfaces this to the user
    }).finally(() => {
      loading.value = false
    })
  }

  watch(() => apiStatus.recoveryCount, count => {
    if (count > 0) loadDiaryData()
  })

  onMounted(() => {
    loadDiaryData()
    const stored = localStorage.getItem('id.datePickerExpanded')
    if (stored) {
      isDatePickerExpanded.value = stored === 'true'
    }
  })
</script>
<style scoped>
  .title {
    font-size: 24px;
    font-weight: bold;
  }
  .author {
    font-size: 16px;
    color: rgb(var(--v-theme-primary));
  }

  :deep(.diary-day-content) {
    position: relative;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
  }

  @media (max-width: 599px) {
    :deep(.calendar-col) {
      width: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
    }
  }

  :deep(.diary-day-marker) {
    position: absolute;
    top: 1px;
    right: 1px;
    width: 8px;
    height: 8px;
    background: rgb(var(--v-theme-on-surface));
    clip-path: polygon(100% 0, 100% 100%, 0 0);
    pointer-events: none;
    filter: drop-shadow(0 0 1px rgb(var(--v-theme-on-surface)));
  }

  .day-nav-card {
    border: 1px solid rgba(var(--v-theme-primary), 0.25);
    background: linear-gradient(135deg, rgba(var(--v-theme-primary), 0.12), rgba(var(--v-theme-background), 0.9));
    -webkit-backdrop-filter: blur(8px);
    backdrop-filter: blur(8px);
    box-shadow: 0 12px 24px rgba(0, 0, 0, 0.08);
  }

  .day-nav-card__content {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 16px;
  }

  .day-nav-btn {
    color: rgb(var(--v-theme-primary)) !important;
    font-weight: 600;
    font-size: 1rem;
  }

  .day-nav-btn:hover {
    color: rgb(var(--v-theme-primary), 0.7) !important;
  }

  .day-nav-btn:active,
  .day-nav-btn:focus {
    color: rgb(var(--v-theme-primary)) !important;
  }

  .day-nav-btn:disabled {
    color: rgba(var(--v-theme-primary), 0.5) !important;
  }

  :deep(.day-nav-btn.v-btn--active) {
    color: rgb(var(--v-theme-primary)) !important;
  }

  .back-to-diaries-btn {
    margin-left: -8px;
  }

  :deep(.timeline-col .v-timeline-item__body) {
    min-width: 0;
    width: 100%;
    flex: 1 1 auto;
  }

  :deep(.timeline-col .v-timeline-item__content) {
    min-width: 0;
    width: 100%;
    max-width: 100%;
    display: block;
  }

  @media (min-width: 600px) {
    :deep(.timeline-col .v-timeline-item__body) {
      padding-right: 0;
    }
  }

  .search-inline {
    width: 480px;
  }
</style>
