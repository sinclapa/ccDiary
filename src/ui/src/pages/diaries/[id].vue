<template>
  <v-container>
    <v-row>
      <h2>{{ diary?.title }}</h2><h4>&nbsp;by {{ diary?.author }}</h4>
    </v-row>
    <v-row>
      <v-col>
        <v-date-picker
          v-model="selectedDate"
          :max="maxDate"
          :min="minDate"
          @update:model-value="selectDate"
          @update:month="updateMonth"
          @update:year="updateMonth"
        />
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
              Add Entry
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
        <v-timeline :align="'start'" side="end">
          <v-timeline-item
            v-for="(diaryEntry, i) in diaryEntries"
            :key="i"
            :dot-color="'red'"
            size="small"
          >
            <template #opposite>
              <div
                :class="`pt-1 headline font-weight-bold text-${'red'}`"
                v-text="dayjs(diaryEntry.date).format('ddd HH:mm:ss')"
              />
            </template>
            <div>
              <h2 :class="`mt-n1 headline font-weight-light mb-4 text-${'red'}`">
                {{ diaryEntry.location }}

                <v-btn
                  v-if="state.isAuthenticated"
                  :color="'red'"
                  icon
                  size="small"
                  @click="editItem(diaryEntry)"
                >
                  <v-icon>
                    mdi-pencil
                  </v-icon>
                </v-btn>
                <v-btn
                  v-if="state.isAuthenticated"
                  :color="'red'"
                  icon
                  size="small"
                  @click="deleteItem(diaryEntry)"
                >
                  <v-icon>
                    mdi-delete
                  </v-icon>
                </v-btn>
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
      if (diaryEntries.value && diaryEntries.value.length > 0) {
        date = diaryEntries.value[diaryEntries.value.length - 1].date
      }
      editedItem.value = new DiaryEntry(diaryId, date, '', '')
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
    console.warn(x)
  }

  onMounted(() => {
    loadDiary(diaryId)
    loadCalendar(diaryId).then(x => {
      selectedDate.value = x
      selectDate(selectedDate.value)
    })
  })
</script>
