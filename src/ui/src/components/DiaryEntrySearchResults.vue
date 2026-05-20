<template>
  <v-row>
    <v-col cols="12">
      <v-progress-linear
        :active="loading"
        color="primary"
        height="2"
        indeterminate
      />
      <div v-if="!loading && results && results.totalCount === 0" class="text-body-2 text-disabled pa-4">
        No entries found for "{{ search }}"
      </div>
      <v-list v-else-if="results && results.items.length > 0" bg-color="transparent" lines="two">
        <v-list-item
          v-for="entry in results.items"
          :key="entry.diaryEntryId"
          class="search-result-item"
          rounded="lg"
          @click="$emit('select', entry)"
        >
          <template #prepend>
            <div class="search-result-date text-caption text-primary">
              {{ dayjs(entry.date).format('ddd D MMM YYYY') }}<br>{{ dayjs(entry.date).format('HH:mm') }}
            </div>
          </template>
          <v-list-item-title class="text-primary">{{ entry.location }}</v-list-item-title>
          <v-list-item-subtitle class="search-result-preview">{{ entry.entry }}</v-list-item-subtitle>
        </v-list-item>
      </v-list>
      <div v-if="results && results.totalCount > pageSize" class="d-flex justify-center pb-4">
        <v-pagination
          :length="Math.ceil(results.totalCount / pageSize)"
          :model-value="page"
          rounded="circle"
          :total-visible="isMobile ? 3 : 7"
          @update:model-value="$emit('update:page', $event)"
        />
      </div>
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
  import dayjs from 'dayjs'
  import { useDisplay } from 'vuetify'
  import type DiaryEntry from '@/services/models/diaryEntry'
  import type PagedResult from '@/services/models/pagedResult'

  defineProps<{
    search: string
    loading: boolean
    results: PagedResult<DiaryEntry> | null
    page: number
    pageSize: number
  }>()

  defineEmits<{
    select: [entry: DiaryEntry]
    'update:page': [page: number]
  }>()

  const { mobile } = useDisplay()
  const isMobile = mobile
</script>

<style scoped>
  .search-result-item {
    border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
    margin-bottom: 4px;
    cursor: pointer;
    transition: border-color 0.2s ease;
  }

  .search-result-item:hover {
    border-color: rgba(var(--v-theme-primary), 0.4);
  }

  .search-result-date {
    width: 110px;
    min-width: 110px;
    padding-right: 16px;
    line-height: 1.4;
    white-space: nowrap;
  }

  .search-result-preview {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
</style>
