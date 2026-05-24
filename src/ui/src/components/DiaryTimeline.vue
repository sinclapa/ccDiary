<template>
  <v-timeline :align="'start'" side="end" style="justify-content: start; height: fit-content;">
    <v-timeline-item
      v-for="(entry, i) in entries"
      :key="i"
      dot-color="primary"
      size="small"
    >
      <template #opposite>
        <div class="pt-1 headline font-weight-light text-primary" style="width: 80px;">
          {{ dayjs(entry.date).format('ddd HH:mm') }}
        </div>
      </template>
      <div
        class="entry-content"
        :class="{ 'entry-content--with-map': (entry.showMap && entry.mapLocation) || (entry.showJourney && entry.fromLocation && entry.toLocation) }"
      >
        <div class="entry-text-col">
          <h2 class="mt-n1 headline font-weight-light mb-4 text-primary">
            {{ entry.location }}
            <div v-if="canEdit">
              <v-btn
                aria-label="Edit entry"
                class="action-btn"
                color="primary"
                icon="$mdi-pencil"
                size="x-small"
                variant="outlined"
                @click="$emit('edit', entry)"
              />
              &nbsp;
              <v-btn
                aria-label="Delete entry"
                class="action-btn"
                color="primary"
                icon="$mdi-delete"
                size="x-small"
                variant="outlined"
                @click="$emit('delete', entry)"
              />
            </div>
          </h2>
          <div>
            {{ entry.entry }}
          </div>
          <v-img
            v-if="entry.imageData && entry.imageContentType"
            class="mt-2 diary-entry-media"
            :max-height="400"
            :src="`data:${entry.imageContentType};base64,${entry.imageData}`"
          />
        </div>
        <div
          v-if="(entry.showMap && entry.mapLocation) || (entry.showJourney && entry.fromLocation && entry.toLocation)"
          class="entry-map-col"
        >
          <map-view v-if="entry.showMap && entry.mapLocation" :location="entry.mapLocation" />
          <journey-view
            v-if="entry.showJourney && entry.fromLocation && entry.toLocation"
            :from-location="entry.fromLocation"
            :journey-mode="entry.journeyMode"
            :to-location="entry.toLocation"
          />
        </div>
      </div>
    </v-timeline-item>
  </v-timeline>
</template>

<script setup lang="ts">
  import dayjs from 'dayjs'
  import type DiaryEntry from '@/services/models/diaryEntry'

  defineProps<{
    entries: DiaryEntry[]
    canEdit: boolean
  }>()

  defineEmits<{
    edit: [entry: DiaryEntry]
    delete: [entry: DiaryEntry]
  }>()
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

  :deep(.diary-entry-media) {
    width: 100%;
    max-width: 100%;
    display: block;
  }

  .entry-content {
    display: flex;
    flex-direction: column;
    width: 100%;
    min-width: 0;
  }

  .entry-text-col {
    min-width: 0;
    width: 100%;
  }

  .entry-map-col {
    flex: 0 0 300px;
    min-width: 0;
  }

  .entry-content--with-map {
    flex-direction: row;
    align-items: flex-start;
    gap: 12px;
  }

  .entry-content--with-map .entry-text-col {
    flex: 1 1 auto;
    min-width: 0;
  }

  @media (max-width: 599px) {
    .entry-content--with-map {
      flex-direction: column;
    }
    .entry-content--with-map .entry-text-col {
      width: 100%;
    }
    .entry-map-col {
      flex: 0 0 auto;
      width: 100%;
    }
  }
</style>
