<template>
<v-container>
    <v-row>
{{ diary?.title }} by {{ diary?.author }}
</v-row>
<v-row>
    <v-col>
      <v-date-picker v-model="selectedDate" @update:model-value="selectDate" @update:month="updateMonth" @update:year="updateMonth" :min="minDate" :max="maxDate" />
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
          :location="editedItem.location"
          :entry="editedItem.entry"
          @submit="onSubmitDiaryEntry"
          @close="close"
        ></diary-entry-editor>
      </v-dialog>
      </v-col>
<v-col>

      <v-timeline align="start" side="end">
    <v-timeline-item
      v-for="(diaryEntry, i) in diaryEntries"
      :key="i"
      :dot-color="'red'"
      size="small"
    >
      <template v-slot:opposite>
        <div
          :class="`pt-1 headline font-weight-bold text-${'red'}`"
          v-text="dayjs(diaryEntry.date).format('ddd HH:mm:ss')"
        ></div>
      </template>
      <div>
        <h2 :class="`mt-n1 headline font-weight-light mb-4 text-${'red'}`">
          {{ diaryEntry.location }}
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

  async function loadCalendar(diaryId: string) : Promise<Date> {
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
      editedItem.value = new DiaryEntry(diaryId, selectedDate.value ?? new Date(), '', '')
    } else {
      editedItem.value = Object.assign({}, item)
    }
    dialog.value = true
  }

  async function onSubmitDiaryEntry(payload: {date: Date, location: string, entry: string}) {
    editedItem.value.date = payload.date
    editedItem.value.location = payload.location
    editedItem.value.entry = payload.entry
    if (editedItem.value.diaryEntryId === undefined) {
      await diaryEntryAPI.createDiaryEntry(editedItem.value)
    } else {
      await diaryEntryAPI.updateDiaryEntry(editedItem.value)
    }
    //await data()
    close()
  }

  async function selectDate(date: any) {
    diaryEntries.value = await diaryEntryAPI.searchDiaryEntryForDay(diaryId, date.getFullYear(), date.getMonth() + 1, date.getDate())
  }

  function updateMonth (x: any | undefined) {
    console.warn(x)
  }

  onMounted(() => {
    loadDiary(diaryId)
    loadCalendar(diaryId).then(x => {
      selectedDate.value = x;
      selectDate(selectedDate.value
      )})
  })
</script>
