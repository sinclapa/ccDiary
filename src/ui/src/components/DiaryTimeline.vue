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
            v-if="entry.images.length > 0"
            :alt="imageAlt(entry, 0)"
            class="mt-2 diary-entry-media diary-entry-media--zoomable"
            :max-height="400"
            role="button"
            :src="imageSrc(entry.images[0])"
            tabindex="0"
            @click="openImage(entry, 0)"
            @keydown.enter.prevent="openImage(entry, 0)"
            @keydown.space.prevent="openImage(entry, 0)"
          />
          <div
            v-if="entry.images.length > 1"
            class="entry-thumbs mt-2"
          >
            <v-img
              v-for="(image, n) in entry.images.slice(1)"
              :key="n + 1"
              :alt="imageAlt(entry, n + 1)"
              aspect-ratio="1"
              class="entry-thumb diary-entry-media--zoomable"
              cover
              role="button"
              :src="imageSrc(image)"
              tabindex="0"
              @click="openImage(entry, n + 1)"
              @keydown.enter.prevent="openImage(entry, n + 1)"
              @keydown.space.prevent="openImage(entry, n + 1)"
            />
          </div>
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
    <v-card
      class="image-viewer"
      @keydown.left.prevent="stepImage(-1)"
      @keydown.right.prevent="stepImage(1)"
    >
      <v-card-title class="d-flex align-center pe-2">
        <span class="text-subtitle-1 text-truncate">{{ zoomedCaption }}</span>
        <v-spacer />
        <template v-if="zoomedImages.length > 1">
          <v-btn
            aria-label="Previous photograph"
            :disabled="zoomedIndex === 0"
            icon="$mdi-chevron-left"
            size="small"
            variant="text"
            @click="stepImage(-1)"
          />
          <span
            aria-live="polite"
            class="text-body-2 mx-1 image-viewer__count"
          >{{ zoomedIndex + 1 }} of {{ zoomedImages.length }}</span>
          <v-btn
            aria-label="Next photograph"
            :disabled="zoomedIndex === zoomedImages.length - 1"
            icon="$mdi-chevron-right"
            size="small"
            variant="text"
            @click="stepImage(1)"
          />
        </template>
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
  import { type DiaryEntryImage, imageSrc } from '@/services/models/diaryEntry'

  defineProps<{
    entries: DiaryEntry[]
    canEdit: boolean
  }>()

  defineEmits<{
    edit: [entry: DiaryEntry]
    delete: [entry: DiaryEntry]
  }>()

  const imageDialog = ref(false)
  const zoomedImages = ref<DiaryEntryImage[]>([])
  const zoomedIndex = ref(0)
  const zoomedCaption = ref('')
  const zoomedSrc = computed(() => {
    const image = zoomedImages.value[zoomedIndex.value]
    return image ? imageSrc(image) : undefined
  })

  function imageAlt (entry: DiaryEntry, index: number) {
    const day = entryTime(entry.date).format('D MMMM YYYY')
    const which = entry.images.length > 1 ? ` (${index + 1} of ${entry.images.length})` : ''
    return `Photograph for the entry of ${day}${which} — select to view full size`
  }

  function openImage (entry: DiaryEntry, index: number) {
    if (!entry.images[index]) return
    zoomedImages.value = entry.images
    zoomedIndex.value = index
    zoomedCaption.value = `${entry.location} — ${entryTime(entry.date).format('D MMMM YYYY')}`
    imageDialog.value = true
  }

  function stepImage (by: number) {
    const next = zoomedIndex.value + by
    if (next >= 0 && next < zoomedImages.value.length) {
      zoomedIndex.value = next
    }
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

  .image-viewer__count {
    white-space: nowrap;
  }

  .entry-thumbs {
    display: grid;
    gap: 6px;
    grid-template-columns: repeat(auto-fill, minmax(72px, 1fr));
  }

  .entry-thumb {
    border-radius: 4px;
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
