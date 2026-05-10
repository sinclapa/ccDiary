<template>
  <div>
    <v-progress-linear
      :active="loading"
      color="primary"
      height="2"
      indeterminate
    />
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
                v-if="authStore.isContributor"
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
        <div class="d-flex justify-end gap-1">
          <v-btn
            v-if="canEdit(item)"
            :id="item.diaryId + '_edit'"
            class="action-btn"
            color="primary"
            icon="$mdi-pencil"
            size="x-small"
            variant="outlined"
            @click="editItem(item)"
          />
          <v-btn
            v-if="canEdit(item)"
            :id="item.diaryId + '_delete'"
            class="action-btn"
            color="primary"
            icon="$mdi-delete"
            size="x-small"
            variant="outlined"
            @click="deleteItem(item)"
          />
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
  import { useAuthStore } from '@/stores/auth'

  const apiStatus = useApiStatusStore()
  const authStore = useAuthStore()
  const loading = ref(false)
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
    if (authStore.isContributor) {
      cols.push({ title: 'Actions', key: 'actions', align: 'end' } as any)
    }
    return cols
  })

  function canEdit (item: Diary): boolean {
    if (authStore.isAdmin) return true
    return authStore.isContributor && item.ownerId === authStore.appUser?.entraObjectId
  }

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
    loading.value = true
    try {
      diaries.value = await diaryAPI.getDiaries()
    } catch {
      // API unavailable — ApiStatusBanner surfaces this to the user
    } finally {
      loading.value = false
    }
  }

  watch(() => apiStatus.recoveryCount, count => {
    if (count > 0) data()
  })

  onMounted(async () => {
    await data()
    defaultItem.value.author = state.user?.name ?? ''
  })

</script>

<style scoped>
.action-btn {
  transition: background-color 0.15s ease, color 0.15s ease;
}

.action-btn:hover {
  background-color: rgb(var(--v-theme-primary)) !important;
  color: white !important;
}

.action-btn:hover :deep(.v-btn__overlay) {
  opacity: 0 !important;
}
</style>
