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
    <v-row>
      <div>
        <span class="title">{{ diary?.title }}&nbsp;</span>
        <span class="author">&nbsp;by {{ diary?.author }}</span>
      </div>
    </v-row>
    <v-row>
      <v-col cols="auto">
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
                :color="isDatePickerExpanded ? 'primary' : 'secondary'"
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
                <v-btn v-bind="props" />
                <span v-if="hasDiaryEntryOnDate(item.isoDate)" class="diary-day-marker" />
              </div>
            </template>
          </v-date-picker>
        </v-row>
        <v-row>
          <v-col style="margin: 0; padding: 0;">
            <v-btn
              aria-label="Go to start"
              class="mb-2"
              :color="'white'"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(minDate).format('YYYY-MM-DD')"
              @click="onMoveStart()"
            >
              <v-icon>
                $mdi-skip-backward
              </v-icon>
            </v-btn>
          </v-col>
          <v-col style="margin: 0; padding: 0;">
            <v-btn
              aria-label="Move backward"
              class="mb-2"
              :color="'white'"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(minDate).format('YYYY-MM-DD')"
              @click="onMoveBackward()"
            >
              <v-icon>
                $mdi-rewind
              </v-icon>
            </v-btn>
          </v-col>
          <v-col style="margin: 0; padding: 0;">
            <v-dialog
              v-model="dialog"
            >
              <template #activator="{ props }">
                <v-btn
                  v-if="canEditDiary"
                  class="mb-2"
                  v-bind="props"
                  @click="onAddEntry()"
                >
                  Add
                </v-btn>
              </template>
              <diary-entry-editor
                :date="editedItem.date"
                :entry="editedItem.entry"
                :from-location="editedItem.fromLocation"
                :image-content-type="editedItem.imageContentType"
                :image-data="editedItem.imageData"
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
          </v-col>
          <v-col style="margin: 0; padding: 0;">
            <v-btn
              aria-label="Move forward"
              class="mb-2"
              :color="'white'"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(maxDate).format('YYYY-MM-DD')"
              @click="onMoveForward()"
            >
              <v-icon>
                $mdi-fast-forward
              </v-icon>
            </v-btn>
          </v-col>
          <v-col style="margin: 0; padding: 0;">
            <v-btn
              aria-label="Go to end"
              class="mb-2"
              :color="'white'"
              :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(maxDate).format('YYYY-MM-DD')"
              @click="onMoveEnd()"
            >
              <v-icon>
                $mdi-skip-forward
              </v-icon>
            </v-btn>
          </v-col>
        </v-row>
        <v-dialog v-model="dialogDelete" max-width="500px">
          <v-card>
            <v-card-title class="text-h7">Are you sure you want to delete this diary entry?</v-card-title>
            <v-card-actions>
              <v-spacer />
              <v-btn variant="text" @click="closeDelete">Cancel</v-btn>
              <v-btn variant="text" @click="deleteItemConfirm">OK</v-btn>
              <v-spacer />
            </v-card-actions>
          </v-card>
        </v-dialog>
      </v-col>
      <v-col>
        <v-timeline :align="'start'" side="end" style="justify-content: start; height: fit-content;">
          <v-timeline-item
            v-for="(diaryEntry, i) in diaryEntries"
            :key="i"
            :dot-color="'red'"
            size="small"
          >
            <template #opposite>
              <div :class="`pt-1 headline font-weight-light text-${'red'}`" style="width: 80px;">
                {{ dayjs(diaryEntry.date).format('ddd HH:mm') }}
              </div>
            </template>
            <div>
              <h2 :class="`mt-n1 headline font-weight-light mb-4 text-${'red'}`">
                {{ diaryEntry.location }}
                <div v-if="canEditDiary">
                  <v-btn
                    aria-label="Edit entry"
                    :color="'red'"
                    icon="$mdi-pencil"
                    size="x-small"
                    @click="onEditEntry(diaryEntry)"
                  />
                  &nbsp;
                  <v-btn
                    aria-label="Delete entry"
                    :color="'red'"
                    icon="$mdi-delete"
                    size="x-small"
                    @click="onDeleteEntry(diaryEntry)"
                  />
                </div>
              </h2>
              <div>
                {{ diaryEntry.entry }}
              </div>
              <map-view v-if="diaryEntry.showMap && diaryEntry.mapLocation" class="mt-2" :location="diaryEntry.mapLocation" />
              <journey-view
                v-if="diaryEntry.showJourney && diaryEntry.fromLocation && diaryEntry.toLocation"
                class="mt-2"
                :from-location="diaryEntry.fromLocation"
                :journey-mode="diaryEntry.journeyMode"
                :to-location="diaryEntry.toLocation"
              />
              <v-img
                v-if="diaryEntry.imageData && diaryEntry.imageContentType"
                class="mt-2"
                :max-height="400"
                :src="`data:${diaryEntry.imageContentType};base64,${diaryEntry.imageData}`"
              />
            </div>
          </v-timeline-item>
        </v-timeline>
      </v-col>
    </v-row>

  </v-container>

</template>
<script setup lang="ts">
  import { diaryAPI } from '@/services/modules/diaryService'
  import { diaryEntryAPI } from '@/services/modules/diaryEntryService'
  import Diary from '@/services/models/diary'
  import DiaryEntry from '@/services/models/diaryEntry'
  import { useAuthStore } from '@/stores/auth'
  import dayjs from 'dayjs'
  import { useApiStatusStore } from '@/stores/apiStatus'
  import JourneyView from '@/components/JourneyView.vue'
  import { endFaroUserAction, startFaroUserAction } from '@/plugins/faro'

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
    font-style: italic;
  }

  :deep(.diary-day-content) {
    position: relative;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
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
</style>
