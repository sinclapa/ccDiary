<template>
  <div>
    <v-data-table
      :headers="headers"
      :items="diaries"
    >
      <template #top>
        <v-toolbar
          flat
        >
          <v-toolbar-title>Diaries</v-toolbar-title>
          <v-spacer />
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
                Add Diary
              </v-btn>
            </template>
            <DiaryEditor
              :add-mode="editedItem?.diaryId == undefined"
              :author="editedItem?.author"
              :description="editedItem?.description"
              :title="editedItem?.title"
              @close="close"
              @submit="onAddDiary"
            />
          </v-dialog>
          <v-dialog v-model="dialogDelete" max-width="500px">
            <v-card>
              <v-card-title class="text-h7">Are you sure you want to delete this diary?</v-card-title>
              <v-card-actions>
                <v-spacer />
                <v-btn variant="text" @click="closeDelete">Cancel</v-btn>
                <v-btn variant="text" @click="deleteItemConfirm">OK</v-btn>
                <v-spacer />
              </v-card-actions>
            </v-card>
          </v-dialog>
        </v-toolbar>
      </template>
      <template #item.title="{ item }">
        <a :href="'diaries/'+ item.diaryId">
          {{ item.title }}
        </a>
      </template>
      <template #item.actions="{ item }">
        <div class="d-flex justify-end">
          <v-btn
            v-if="state.isAuthenticated"
            :id="item.diaryId + '_edit'"
            icon
            size="small"
            @click="editItem(item)"
          >
            <v-icon>
              mdi-pencil
            </v-icon>
          </v-btn>
          <v-btn
            v-if="state.isAuthenticated"
            :id="item.diaryId + '_delete'"
            icon
            size="small"
            @click="deleteItem(item)"
          >
            <v-icon>
              mdi-delete
            </v-icon>
          </v-btn>
        </div>
      </template>
    </v-data-table>
  </div>
</template>

<script setup lang="ts">
  import { diaryAPI } from '@/services/modules/diaryService'
  import Diary from '@/services/models/diary'
  import { state } from '@/services/authentication/msalConfig'
  import { useApiStatusStore } from '@/stores/apiStatus'

  const apiStatus = useApiStatusStore()
  const dialogDelete = ref(false)
  const dialog = ref(false)
  const diaries = ref([] as Diary[])
  const defaultItem = ref(new Diary('', '', '', undefined) as Diary)
  const editedItem = ref<Diary>(new Diary('', '', '', undefined) as Diary)

  const headers = computed(() => {
    const cols = [
      { title: 'Title', value: 'title' },
      { title: 'Author', value: 'author' },
      { title: 'Description', value: 'description' },
    ]
    if (state.isAuthenticated) {
      cols.push({ title: 'Actions', key: 'actions', align: 'end' } as any)
    }
    return cols
  })

  async function onAddDiary (payload : {title: string, author: string, description: string}) {
    editedItem.value.title = payload.title
    editedItem.value.author = payload.author
    editedItem.value.description = payload.description
    if (editedItem.value.diaryId === undefined) {
      await diaryAPI.createDiary(editedItem.value)
    } else {
      await diaryAPI.updateDiary(editedItem.value)
    }
    await data()
    close()
  }

  function close () {
    dialog.value = false
    nextTick(() => {
      editedItem.value = { ...defaultItem.value }
    })
  }

  async function editItem (item?: Diary) {
    if (item === undefined) {
      editedItem.value = { ...defaultItem.value }
    } else {
      editedItem.value = { ...item }
    }
    dialog.value = true
  }

  async function deleteItem (item: Diary) {
    editedItem.value = { ...item }
    dialogDelete.value = true
  }

  function closeDelete () {
    dialogDelete.value = false
    nextTick(() => {
      editedItem.value = { ...defaultItem.value }
    })
  }

  async function deleteItemConfirm () {
    if (editedItem.value.diaryId !== undefined) {
      await diaryAPI.deleteDiary(editedItem.value.diaryId)
      await data()
    }
    closeDelete()
  }

  async function data () {
    diaries.value = await diaryAPI.getDiaries()
  }

  watch(() => apiStatus.recoveryCount, (count) => {
    if (count > 0) data()
  })

  onMounted(async () => {
    await data()
    defaultItem.value.author = state.user?.name ?? ''
  })

</script>
