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
        <v-btn v-if="state.isAuthenticated" icon size="small" :id="item.diaryId + '_edit'" @click="editItem(item)">
          <v-icon>
            mdi-pencil
          </v-icon>
        </v-btn>
        <v-btn v-if="state.isAuthenticated" icon size="small" :id="item.diaryId + '_delete'" @click="deleteItem(item)">
          <v-icon>
            mdi-delete
          </v-icon>
        </v-btn>
      </template>
    </v-data-table>
  </div>
</template>

<script setup lang="ts">
  import { diaryAPI } from '@/services/modules/diaryService'
  import Diary from '@/services/models/diary'
  import { state } from '@/services/authentication/msalConfig'

  const dialogDelete = ref(false)
  const dialog = ref(false)
  const diaries = ref([] as Diary[])
  const defaultItem = ref(new Diary('', '', '') as Diary)
  const editedItem = ref(new Diary('', '', '') as Diary)

  const headers = [
    { title: 'Title', value: 'title' },
    { title: 'Author', value: 'author' },
    { title: 'Description', value: 'description' },
    { title: 'Actions', key: 'actions' },
  ]

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
      editedItem.value = Object.assign({}, defaultItem.value)
    })
  }

  async function editItem (item?: Diary) {
    if (item === undefined) {
      editedItem.value = Object.assign({}, defaultItem.value)
    } else {
      editedItem.value = Object.assign({}, item)
    }
    dialog.value = true
  }

  async function deleteItem (item: Diary) {
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
    if (editedItem.value.diaryId !== undefined) {
      await diaryAPI.deleteDiary(editedItem.value.diaryId)
      await data()
    }
    closeDelete()
  }

  async function data () {
    diaries.value = await diaryAPI.getDiaries()
  }

  onMounted(async () => {
    await data()
    defaultItem.value.author = state.user?.name ?? ''
  })

</script>
