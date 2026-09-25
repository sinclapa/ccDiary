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
          {{ entryTime(entry.date).format('ddd HH:mm') }}
        </div>
      </template>
      <div
        class="entry-content"
        :class="{ 'entry-content--with-map': hasMapColumn(entry) }"
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
            :alt="`Photograph for the entry of ${entryTime(entry.date).format('D MMMM YYYY')} — select to view full size`"
            class="mt-2 diary-entry-media diary-entry-media--zoomable"
            :max-height="400"
            role="button"
            :src="imageSrc(entry)"
            tabindex="0"
            @click="openImage(entry)"
            @keydown.enter.prevent="openImage(entry)"
            @keydown.space.prevent="openImage(entry)"
          />
        </div>
        <div
          v-if="hasMapColumn(entry)"
          class="entry-map-col"
        >
          <map-view v-if="showsMap(entry)" :location="entry.mapLocation!" />
          <journey-view
            v-if="showsJourney(entry)"
            :from-location="entry.fromLocation!"
            :journey-mode="entry.journeyMode"
            :to-location="entry.toLocation!"
          />
        </div>
      </div>
    </v-timeline-item>
  </v-timeline>

  <!-- The page photographs are the point of the entry, and the thumbnail caps at 400px, so the
       handwriting is only readable full size. Scrim and Escape both close it. -->
  <v-dialog
    v-model="imageDialog"
    aria-label="Diary page photograph"
    :max-width="1200"
    scrollable
  >
    <v-card class="image-viewer">
      <v-card-title class="d-flex align-center pe-2">
        <span class="text-subtitle-1 text-truncate">{{ zoomedCaption }}</span>
        <v-spacer />
        <v-btn
          aria-label="Close photograph"
          icon="$mdi-close"
          size="small"
          variant="text"
          @click="imageDialog = false"
        />
      </v-card-title>
      <v-card-text class="pa-2">
        <img
          v-if="zoomedSrc"
          :alt="zoomedCaption"
          class="image-viewer__img"
          :src="zoomedSrc"
        >
      </v-card-text>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  import { entryTime } from '@/utils/entryTime'
  import type DiaryEntry from '@/services/models/diaryEntry'

  defineProps<{
    entries: DiaryEntry[]
    canEdit: boolean
  }>()

  defineEmits<{
    edit: [entry: DiaryEntry]
    delete: [entry: DiaryEntry]
  }>()

  const imageDialog = ref(false)
  const zoomedSrc = ref<string>()
  const zoomedCaption = ref('')

  function imageSrc (entry: DiaryEntry) {
    return `data:${entry.imageContentType};base64,${entry.imageData}`
  }

  function openImage (entry: DiaryEntry) {
    if (!entry.imageData || !entry.imageContentType) return
    zoomedSrc.value = imageSrc(entry)
    zoomedCaption.value = `${entry.location} — ${entryTime(entry.date).format('D MMMM YYYY')}`
    imageDialog.value = true
  }

  // The map column is laid out by one condition and populated by two. Inlining all three
  // meant the wrapper's condition was a copy of the other two OR'd together, so adding a
  // third map type would render it into a column that had already decided not to exist.
  function showsMap (entry: DiaryEntry) {
    return Boolean(entry.showMap && entry.mapLocation)
  }

  function showsJourney (entry: DiaryEntry) {
    return Boolean(entry.showJourney && entry.fromLocation && entry.toLocation)
  }

  function hasMapColumn (entry: DiaryEntry) {
    return showsMap(entry) || showsJourney(entry)
  }
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

  :deep(.diary-entry-media--zoomable) {
    cursor: zoom-in;
    transition: opacity 0.15s ease;
  }

  :deep(.diary-entry-media--zoomable:hover) {
    opacity: 0.9;
  }

  :deep(.diary-entry-media--zoomable:focus-visible) {
    outline: 2px solid rgb(var(--v-theme-primary));
    outline-offset: 2px;
  }

  /* The photograph fills the dialog's width and grows as tall as it needs; the dialog itself
     scrolls, so a tall stitched crop stays readable rather than being squeezed to fit. */
  .image-viewer__img {
    display: block;
    width: 100%;
    height: auto;
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
