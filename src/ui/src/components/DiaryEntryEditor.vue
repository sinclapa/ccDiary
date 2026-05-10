<template>
  <v-sheet class="mx-auto" width="400">
    <v-form @submit.prevent="submit">
      <v-card
        prepend-icon="$mdi-pen"
        :title="isEdit ? 'Edit Diary Entry' : 'Add Diary Entry'"
      >
        <template #append>
          <v-btn :aria-label="'Cancel'" icon variant="text" @click="close">
            <v-icon>$mdi-close</v-icon>
          </v-btn>
        </template>
        <v-card-text>
          <v-row>
            <v-col>
              <v-date-input
                v-model="date"
                :min="new Date(1900, 0, 1)"
                prepend-icon=""
                prepend-inner-icon="$calendar"
              />
            </v-col>
            <v-col>
              <v-text-field
                v-model="time"
                type="time"
              />
            </v-col>
          </v-row>
          <v-text-field
            id="location"
            v-model="location"
            label="Location"
          />

          <v-textarea
            id="entry"
            v-model="entry"
            auto-grow
            label="Entry"
          />

          <v-switch
            id="show-map"
            v-model="showMap"
            class="mb-2"
            color="primary"
            hide-details
            label="Show Map"
          />

          <v-text-field
            v-if="showMap"
            id="map-location"
            v-model="mapLocation"
            hint="Enter a place name to display on the map"
            label="Map Location"
            persistent-hint
          />

          <v-switch
            id="show-journey"
            v-model="showJourney"
            class="mb-2 mt-4"
            color="primary"
            hide-details
            label="Show Journey"
          />

          <v-text-field
            v-if="showJourney"
            id="from-location"
            v-model="fromLocation"
            hint="Enter the starting place name"
            label="From Location"
            persistent-hint
          />

          <v-text-field
            v-if="showJourney"
            id="to-location"
            v-model="toLocation"
            hint="Enter the destination place name"
            label="To Location"
            persistent-hint
          />

          <v-select
            v-if="showJourney"
            id="journey-mode"
            v-model="journeyMode"
            class="mt-4"
            item-title="label"
            item-value="value"
            :items="journeyModeItems"
            label="Travel Mode"
          >
            <template #item="{ item, props: itemProps }">
              <v-list-item v-bind="itemProps">
                <template #prepend>
                  <v-icon>{{ item.raw.icon }}</v-icon>
                </template>
              </v-list-item>
            </template>
            <template #selection="{ item }">
              <v-icon class="mr-2" size="small">{{ item.raw.icon }}</v-icon>
              {{ item.title }}
            </template>
          </v-select>

          <v-switch
            id="show-image"
            v-model="showImage"
            class="mb-2 mt-4"
            color="primary"
            hide-details
            label="Add Image"
          />

          <template v-if="showImage">
            <button
              id="image-drop-zone"
              class="image-drop-zone mt-2"
              :class="{ 'drag-over': isDragging }"
              type="button"
              @click="triggerFileInput"
              @dragleave.prevent="isDragging = false"
              @dragover.prevent="isDragging = true"
              @drop.prevent="handleDrop"
              @keydown.enter="triggerFileInput"
            >
              <v-img
                v-if="imagePreview"
                max-height="200"
                :src="imagePreview"
              />
              <div
                v-else
                class="drop-zone-placeholder text-center pa-4"
              >
                <v-icon
                  color="grey-lighten-1"
                  size="48"
                >
                  $mdi-image-plus
                </v-icon>
                <div class="text-grey mt-2 text-body-2">
                  Click, drag & drop, or paste an image
                </div>
              </div>
            </button>
            <v-btn
              v-if="imagePreview"
              class="mt-2"
              color="error"
              size="small"
              variant="text"
              @click="clearImage"
            >
              Remove Image
            </v-btn>
            <input
              ref="fileInputRef"
              accept="image/jpeg,image/png,image/gif,image/webp"
              style="display: none"
              type="file"
              @change="handleFileSelect"
            >
          </template>
        </v-card-text>
        <v-divider />

        <v-card-actions>
          <v-spacer />
          <v-row>
            <v-col>
              <v-btn
                id="close"
                block
                text="Close"
                variant="plain"
                @click="close"
              />
            </v-col>
            <v-col>
              <v-btn
                id="save"
                block
                color="primary"
                text="Save"
                type="submit"
                variant="tonal"
              />
            </v-col>
          </v-row>
        </v-card-actions>
      </v-card>
    </v-form>
  </v-sheet>
</template>

<script setup lang="ts">
  import dayjs from 'dayjs'
  import { SubmitEventPromise } from 'vuetify'
  import { VDateInput } from 'vuetify/labs/VDateInput'
  import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
  import type { JourneyMode } from '@/services/models/diaryEntry'

  const journeyModeItems: { label: string; value: JourneyMode; icon: string }[] = [
    { label: 'As the Crow Flies', value: 'crow-flies', icon: '$mdi-bird' },
    { label: 'Walking', value: 'walking', icon: '$mdi-walk' },
    { label: 'Car', value: 'car', icon: '$mdi-car' },
    { label: 'Train', value: 'train', icon: '$mdi-train' },
    { label: 'Boat', value: 'boat', icon: '$mdi-ferry' },
  ]

  const props = defineProps<{isEdit?: boolean, date: Date, location: string, entry: string, mapLocation: string, showMap: boolean, fromLocation: string, toLocation: string, showJourney: boolean, journeyMode: JourneyMode, imageData?: string, imageContentType?: string}>()
  const date = ref<Date>(new Date(props.date))
  const time = ref<string>(dayjs(props.date).format('HH:mm'))
  const location = ref<string>(props.location)
  const entry = ref<string>(props.entry)
  const mapLocation = ref<string>(props.mapLocation)
  const showMap = ref<boolean>(props.showMap)
  const fromLocation = ref<string>(props.fromLocation)
  const toLocation = ref<string>(props.toLocation)
  const showJourney = ref<boolean>(props.showJourney)
  const journeyMode = ref<JourneyMode>(props.journeyMode)
  const imageData = ref<string | undefined>(props.imageData)
  const imageContentType = ref<string | undefined>(props.imageContentType)
  const showImage = ref<boolean>(!!props.imageData)
  const isDragging = ref<boolean>(false)
  const fileInputRef = ref<HTMLInputElement | null>(null)

  const imagePreview = computed(() => {
    if (imageData.value && imageContentType.value) {
      return `data:${imageContentType.value};base64,${imageData.value}`
    }
    return null
  })

  const emit = defineEmits({
    submit (payload: { date: Date, location: string, entry: string, mapLocation: string, showMap: boolean, fromLocation: string, toLocation: string, showJourney: boolean, journeyMode: JourneyMode, imageData: string | undefined, imageContentType: string | undefined }) {
      return payload
    },
    close () {
      return true
    },
  })

  function close () {
    emit('close')
  }

  async function submit (submitEventPromise: SubmitEventPromise) {
    const { valid } = await submitEventPromise
    if (valid) {
      const [hours, minutes] = time.value.split(':')
      const entryDate = new Date(date.value.setHours(Number(hours), Number(minutes), 0, 0))
      emit('submit', {
        date: entryDate,
        location: location.value,
        entry: entry.value,
        mapLocation: mapLocation.value,
        showMap: showMap.value,
        fromLocation: fromLocation.value,
        toLocation: toLocation.value,
        showJourney: showJourney.value,
        journeyMode: journeyMode.value,
        imageData: imageData.value,
        imageContentType: imageContentType.value,
      })
    }
  }

  function processFile (file: File) {
    const reader = new FileReader()
    reader.onload = e => {
      const dataUrl = e.target?.result as string
      const comma = dataUrl.indexOf(',')
      imageData.value = dataUrl.slice(comma + 1)
      imageContentType.value = file.type
    }
    reader.readAsDataURL(file)
  }

  function handleDrop (event: DragEvent) {
    isDragging.value = false
    const file = event.dataTransfer?.files?.[0]
    if (file && file.type.startsWith('image/')) {
      processFile(file)
    }
  }

  function handleFileSelect (event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0]
    if (file) {
      processFile(file)
    }
  }

  function triggerFileInput () {
    fileInputRef.value?.click()
  }

  function clearImage () {
    imageData.value = undefined
    imageContentType.value = undefined
    showImage.value = false
  }

  function handleWindowPaste (event: ClipboardEvent) {
    const items = event.clipboardData?.items
    if (!items) return
    for (const item of Array.from(items)) {
      if (item.type.startsWith('image/')) {
        const file = item.getAsFile()
        if (file) {
          showImage.value = true
          processFile(file)
          break
        }
      }
    }
  }

  onMounted(() => {
    globalThis.addEventListener('paste', handleWindowPaste)
  })

  onUnmounted(() => {
    globalThis.removeEventListener('paste', handleWindowPaste)
  })

  watch(() => props.location, newVal => {
    location.value = newVal
  })
  watch(() => props.entry, newVal => {
    entry.value = newVal
  })
  watch(() => props.date, newVal => {
    date.value = new Date(newVal)
    time.value = dayjs(newVal).format('HH:mm')
  })
  watch(() => props.mapLocation, newVal => {
    mapLocation.value = newVal
  })
  watch(() => props.showMap, newVal => {
    showMap.value = newVal
  })
  watch(showMap, newVal => {
    if (newVal && !mapLocation.value) {
      mapLocation.value = location.value
    }
  })
  watch(() => props.fromLocation, newVal => {
    fromLocation.value = newVal
  })
  watch(() => props.toLocation, newVal => {
    toLocation.value = newVal
  })
  watch(() => props.showJourney, newVal => {
    showJourney.value = newVal
  })
  watch(() => props.journeyMode, newVal => {
    journeyMode.value = newVal
  })
  watch(showJourney, newVal => {
    if (newVal && !fromLocation.value) {
      fromLocation.value = location.value
    }
  })
  watch(() => props.imageData, newVal => {
    imageData.value = newVal
  })
  watch(() => props.imageContentType, newVal => {
    imageContentType.value = newVal
  })
  watch(showImage, newVal => {
    if (!newVal) {
      imageData.value = undefined
      imageContentType.value = undefined
    }
  })
</script>

<style scoped>
  .image-drop-zone {
    background: none;
    border: 2px dashed rgba(var(--v-border-color), var(--v-border-opacity));
    border-radius: 8px;
    cursor: pointer;
    font: inherit;
    min-height: 120px;
    padding: 0;
    text-align: inherit;
    width: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: border-color 0.2s;
  }
  .image-drop-zone.drag-over {
    border-color: rgb(var(--v-theme-primary));
  }
</style>
