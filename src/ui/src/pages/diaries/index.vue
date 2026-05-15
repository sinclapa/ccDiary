<template>
  <v-container>
    <v-progress-linear
      :active="loading"
      color="primary"
      height="2"
      indeterminate
    />

    <div class="d-flex align-center mb-4">
      <h1 class="text-h5">Diaries</h1>
      <v-spacer />
      <v-dialog
        v-model="dialog"
        max-width="560px"
        scrim-clickable
      >
        <template #activator="{ props }">
          <v-btn
            v-if="authStore.isContributor"
            color="primary"
            size="small"
            variant="tonal"
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
      <v-dialog v-model="dialogDelete" max-width="560px">
        <v-card class="delete-diary-dialog" rounded="xl">
          <v-card-title class="d-flex align-center gap-2 text-h6 text-primary">
            <v-icon icon="$mdi-alert-circle-outline" />
            Delete Diary
          </v-card-title>
          <v-card-text>
            <p class="mb-3">Are you sure you want to permanently delete this diary?</p>
            <div class="delete-diary-meta pa-3">
              <div><strong>Title:</strong> {{ editedItem?.title || 'Untitled diary' }}</div>
              <div><strong>Author:</strong> {{ editedItem?.author || 'Unknown author' }}</div>
            </div>
          </v-card-text>
          <v-card-actions class="px-4 pb-4">
            <v-spacer />
            <v-btn variant="text" @click="closeDelete">Cancel</v-btn>
            <v-btn color="primary" variant="flat" @click="deleteItemConfirm">Delete Diary</v-btn>
          </v-card-actions>
        </v-card>
      </v-dialog>
    </div>

    <v-row>
      <v-col
        v-for="item in diaries"
        :key="item.diaryId"
        cols="12"
        md="4"
        sm="6"
      >
        <v-card
          class="diary-card"
          height="100%"
          :href="'diaries/' + item.diaryId"
          rounded="xl"
        >
          <v-card-title class="text-primary">{{ item.title }}</v-card-title>
          <v-card-subtitle>{{ item.author }}</v-card-subtitle>
          <v-card-text class="diary-description">{{ item.description }}</v-card-text>
          <v-card-actions v-if="canEdit(item)" class="px-4 pb-3">
            <v-spacer />
            <v-btn
              :id="item.diaryId + '_edit'"
              class="action-btn"
              color="primary"
              icon="$mdi-pencil"
              size="x-small"
              variant="outlined"
              @click.prevent="editItem(item)"
            />
            <v-btn
              :id="item.diaryId + '_delete'"
              class="action-btn"
              color="primary"
              icon="$mdi-delete"
              size="x-small"
              variant="outlined"
              @click.prevent="deleteItem(item)"
            />
          </v-card-actions>
        </v-card>
      </v-col>
    </v-row>

    <div v-if="totalPages > 1" class="d-flex justify-center pb-4">
      <v-pagination
        v-model="currentPage"
        :length="totalPages"
        rounded="circle"
      />
    </div>
  </v-container>
</template>

<script setup lang="ts">
  import { diaryAPI } from '@/services/modules/diaryService'
  import Diary from '@/services/models/diary'
  import { state } from '@/services/authentication/msalConfig'
  import { useApiStatusStore } from '@/stores/apiStatus'
  import { useAuthStore } from '@/stores/auth'

  const PAGE_SIZE = 12

  const apiStatus = useApiStatusStore()
  const authStore = useAuthStore()
  const loading = ref(false)
  const dialogDelete = ref(false)
  const dialog = ref(false)
  const diaries = ref([] as Diary[])
  const currentPage = ref(1)
  const totalCount = ref(0)
  const totalPages = computed(() => Math.ceil(totalCount.value / PAGE_SIZE))
  const defaultItem = ref(new Diary('', '', '', undefined) as Diary)
  const editedItem = ref<Diary>(new Diary('', '', '', undefined) as Diary)

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
      const result = await diaryAPI.getDiaries(currentPage.value, PAGE_SIZE)
      diaries.value = result.items
      totalCount.value = result.totalCount
    } catch {
      // API unavailable — ApiStatusBanner surfaces this to the user
    } finally {
      loading.value = false
    }
  }

  watch(currentPage, () => data())

  watch(() => apiStatus.recoveryCount, count => {
    if (count > 0) data()
  })

  onMounted(async () => {
    await data()
    defaultItem.value.author = state.user?.name ?? ''
  })

</script>

<style scoped>
.diary-card {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  transition: box-shadow 0.2s ease, border-color 0.2s ease;
  text-decoration: none;
}

.diary-card:hover {
  box-shadow: 0 4px 16px rgba(var(--v-theme-primary), 0.15);
  border-color: rgba(var(--v-theme-primary), 0.4);
}

.diary-description {
  color: rgb(var(--v-theme-on-surface));
  opacity: 0.75;
}

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

.delete-diary-dialog {
  border-color: rgba(var(--v-theme-primary), 0.25);
  background: rgb(var(--v-theme-surface));
}

.delete-diary-meta {
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  border-radius: 12px;
  background: rgb(var(--v-theme-surface));
}
</style>
