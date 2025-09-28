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
          :max="maxDate"
          :min="minDate"
          :max-height="datePickerHeight"
          @update:model-value="selectDate"
          @update:month="updateMonth"
          @update:year="updateMonth"
        >
        </v-date-picker>
        </v-row>
        <v-row >
          <v-col style="margin: 0; padding: 0;">
        <v-btn
          class="mb-2"
          :color="'white'"
          :disabled="dayjs(selectedDate).get('date') == dayjs(minDate).get('date')"
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
          :disabled="dayjs(selectedDate).get('date') == dayjs(minDate).get('date')"
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
          :disabled="dayjs(selectedDate).get('date') == dayjs(maxDate).get('date')"
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
          :disabled="dayjs(selectedDate).get('date') == dayjs(maxDate).get('date')"
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

  async function loadCalendar (diaryId: string) : Promise<Date> {
    maxDate.value = await diaryEntryAPI.getMaxDate(diaryId)
    const minDiaryEntryDate = await diaryEntryAPI.getMinDate(diaryId)
    minDate.value = new Date(minDiaryEntryDate.getFullYear(), minDiaryEntryDate.getMonth(), minDiaryEntryDate.getDate())
    return minDate.value
  }

  function close () {
    dialog.value = false
    nextTick(() => {
      editedItem.value = Object.assign({}, defaultItem.value)
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
      editedItem.value = Object.assign({}, item)
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
    loadCalendar(diaryId)
    if (editedItem.value.date.toDateString() === selectedDate.value?.toDateString()) {
      selectDate(selectedDate.value)
    }
    close()
  }

  async function deleteItem (item: DiaryEntry) {
    editedItem.value = Object.assign({}, item)
    dialogDelete.value = true
  }

  function moveForward () {
    selectedDate.value = dayjs(selectedDate.value).endOf('day').add(1, 'day').toDate()
    selectDate(selectedDate.value)
  }

  function moveBackward () {
    selectedDate.value = dayjs(selectedDate.value).startOf('day').subtract(1, 'day').toDate()
    selectDate(selectedDate.value)
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
      editedItem.value = Object.assign({}, defaultItem.value)
    })
  }

  async function deleteItemConfirm () {
    if (editedItem.value.diaryEntryId !== undefined) {
      await diaryEntryAPI.deleteDiaryEntry(editedItem.value.diaryEntryId)
      loadCalendar(diaryId)
      if (editedItem.value.date.toDateString() === selectedDate.value?.toDateString()) {
        selectDate(selectedDate.value)
      }
    }
    closeDelete()
  }

  async function selectDate (date: any) {
    diaryEntries.value = await diaryEntryAPI.searchDiaryEntryForDay(diaryId, date.getFullYear(), date.getMonth() + 1, date.getDate())
  }

  function updateMonth (x: any | undefined) {
    //console.info(x)
  }

  onMounted(() => {
    loadDiary(diaryId)
    loadCalendar(diaryId).then(x => {
      selectedDate.value = x
      selectDate(selectedDate.value)
    })
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
</style>
