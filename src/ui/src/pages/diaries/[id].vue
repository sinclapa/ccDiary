<template>
  <v-container style="overflow-y: visible">
    <v-row>
      <div>
      <span class="title">{{ diary?.title }}&nbsp;</span>
      <span class="author">&nbsp;by {{ diary?.author }}</span>
      </div>
    </v-row>
    <v-row>
      <v-col cols="auto">
        <v-btn
          :color="isDatePickerExpanded ? 'primary' : 'secondary'"
          :variant="isDatePickerExpanded ? 'flat' : 'outlined'"
          class="mb-3"
          size="small"
          @click="toggleDatePickerHeight"
          :aria-label="isDatePickerExpanded ? 'Collapse date picker' : 'Expand date picker'"
        >
          <v-icon :icon="isDatePickerExpanded ? 'mdi-chevron-up' : 'mdi-chevron-down'" />
          {{ isDatePickerExpanded ? 'Compact View' : 'Expanded View' }}
        </v-btn>
        <v-row>
        <v-date-picker
          v-model="selectedDate"
          :month="calendarMonth"
          :year="calendarYear"
          :max="maxDate"
          :min="minDate"
          :max-height="datePickerHeight"
          @update:model-value="selectDate"
          @update:month="updateMonth"
          @update:year="updateYear"
        >
          <template #day="{ item, props }">
            <div class="diary-day-content">
              <v-btn v-bind="props" />
              <span v-if="hasDiaryEntryOnDate(item.isoDate)" class="diary-day-marker" />
            </div>
          </template>
        </v-date-picker>
        </v-row>
        <v-row >
          <v-col style="margin: 0; padding: 0;">
        <v-btn
          class="mb-2"
          :color="'white'"
          :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(minDate).format('YYYY-MM-DD')"
          @click="moveStart()"
        >
          <v-icon>
            mdi-skip-backward
          </v-icon>
        </v-btn>
        </v-col>
        <v-col style="margin: 0; padding: 0;">
        <v-btn
          class="mb-2"
          :color="'white'"
          :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(minDate).format('YYYY-MM-DD')"
          @click="moveBackward()"
        >
          <v-icon>
            mdi-rewind
          </v-icon>
        </v-btn>
        </v-col>
        <v-col style="margin: 0; padding: 0;">
        <v-dialog
          v-model="dialog"
        >
          <template #activator="{ props }">
            <v-btn
              v-if="state.isAuthenticated"
              class="mb-2"
              v-bind="props"
              @click="editItem()"
            >
              Add
            </v-btn>
          </template>
          <diary-entry-editor
            :date="editedItem.date"
            :entry="editedItem.entry"
            :location="editedItem.location"
            @close="close"
            @submit="onSubmitDiaryEntry"
          />
        </v-dialog>
        </v-col>
        <v-col style="margin: 0; padding: 0;">
        <v-btn
          class="mb-2"
          :color="'white'"
          :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(maxDate).format('YYYY-MM-DD')"
          @click="moveForward()"
        >
          <v-icon>
            mdi-fast-forward
          </v-icon>
        </v-btn>
        </v-col>
        <v-col style="margin: 0; padding: 0;">
        <v-btn
          class="mb-2"
          :color="'white'"
          :disabled="dayjs(selectedDate).format('YYYY-MM-DD') == dayjs(maxDate).format('YYYY-MM-DD')"
          @click="moveEnd()"
        >
          <v-icon>
            mdi-skip-forward
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
              <div style="width: 80px;" :class="`pt-1 headline font-weight-light text-${'red'}`">
                {{ dayjs(diaryEntry.date).format('ddd HH:mm') }}
              </div>
            </template>
            <div>
              <h2 :class="`mt-n1 headline font-weight-light mb-4 text-${'red'}`">
                {{ diaryEntry.location }}
                <div v-if="state.isAuthenticated">
                  <v-btn
                    :color="'red'"
                    icon="mdi-pencil"
                    size="x-small"
                    @click="editItem(diaryEntry)"
                  >
                  </v-btn>
                  &nbsp;
                  <v-btn
                    :color="'red'"
                    icon="mdi-delete"
                    size="x-small"
                    @click="deleteItem(diaryEntry)"
                  >
                  </v-btn>
                </div>
              </h2>
              <div>
                {{ diaryEntry.entry }}
              </div>
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
  import { state } from '@/services/authentication/msalConfig'
  import dayjs from 'dayjs'
  import { useApiStatusStore } from '@/stores/apiStatus'

  const apiStatus = useApiStatusStore()

  // Detect if the device is mobile
  const dialog = ref(false)
  const dialogDelete = ref(false)
  const selectedDate = ref<Date>()
  const diaryEntries = ref<DiaryEntry[] | null>()
  const route = useRoute('/diaries/[id]')
  const diaryId = route.params.id
  const diary = ref(new Diary('', '', '') as Diary | undefined)
  const defaultItem = ref<DiaryEntry>(new DiaryEntry(diaryId, new Date(Date.now()), '', ''))
  const editedItem = ref<DiaryEntry>(new DiaryEntry(diaryId, new Date(Date.now()), '', ''))
  const minDate = ref<Date>()
  const maxDate = ref<Date>()
  const isDatePickerExpanded = ref( window.innerWidth >= 600)
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

  async function onSubmitDiaryEntry (payload: {date: Date, location: string, entry: string}) {
    editedItem.value.date = payload.date
    editedItem.value.location = payload.location
    editedItem.value.entry = payload.entry
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
    while (true) {
      selectedDate.value = dayjs(selectedDate.value).endOf('day').add(1, 'day').toDate()

      if (dayjs(selectedDate.value).format('YYYY-MM-DD') >= dayjs(maxDate.value).format('YYYY-MM-DD')) {
        selectedDate.value = dayjs(maxDate.value).endOf('day').toDate()
        await selectDate(selectedDate.value)
        break
      }

      await selectDate(selectedDate.value)

      if (diaryEntries.value && diaryEntries.value.length > 0) {
        break
      }
    }
  }

  async function moveBackward () {
    while (true) {
      selectedDate.value = dayjs(selectedDate.value).startOf('day').subtract(1, 'day').toDate()

      if (dayjs(selectedDate.value).format('YYYY-MM-DD') <= dayjs(minDate.value).format('YYYY-MM-DD')) {
        selectedDate.value = dayjs(minDate.value).startOf('day').toDate()
        await selectDate(selectedDate.value)
        break
      }

      await selectDate(selectedDate.value)

      if (diaryEntries.value && diaryEntries.value.length > 0) {
        break
      }
    }
  }

  function moveStart () {
    selectedDate.value = dayjs(minDate.value).startOf('day').toDate()
    selectDate(selectedDate.value)
  }

  function moveEnd () {
    selectedDate.value = dayjs(maxDate.value).endOf('day').toDate()
    selectDate(selectedDate.value)
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

  async function selectDate (date: any) {
    diaryEntries.value = await diaryEntryAPI.searchDiaryEntryForDay(diaryId, date.getFullYear(), date.getMonth() + 1, date.getDate())
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

  watch(selectedDate, async (newDate) => {
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

  function loadDiaryData () {
    loadDiary(diaryId)
    loadCalendar(diaryId).then(async x => {
      selectedDate.value = x
      calendarMonth.value = dayjs(x).month()
      calendarYear.value = dayjs(x).year()
      await selectDate(selectedDate.value)
    })
  }

  watch(() => apiStatus.recoveryCount, (count) => {
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
