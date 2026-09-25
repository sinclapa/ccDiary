<template>
  <v-sheet rounded="xl">
    <v-form @submit.prevent="submit">
      <v-card
        class="editor-scroll-container"
        prepend-icon="$mdi-pen"
        rounded="xl"
        :title="isEdit ? 'Edit Diary Entry' : 'Add Diary Entry'"
      >
        <template #append>
          <v-btn :aria-label="'Cancel'" icon variant="text" @click="close">
            <v-icon>$mdi-close</v-icon>
          </v-btn>
        </template>
        <v-card-text style="overflow-y: auto; flex: 1 1 auto;">
          <v-row>
            <v-col>
              <v-date-input
                v-model="date"
                color="primary"
                :min="new Date(1900, 0, 1)"
                prepend-icon=""
                prepend-inner-icon="$calendar"
              />
            </v-col>
            <v-col>
              <v-menu
                v-model="timeMenu"
                :close-on-content-click="false"
                location="bottom"
              >
                <template #activator="{ props: menuProps }">
                  <v-text-field
                    v-model="time"
                    color="primary"
                    prepend-inner-icon="$mdi-clock-outline"
                    readonly
                    v-bind="menuProps"
                  />
                </template>
                <v-card rounded="lg">
                  <v-time-picker
                    v-model="time"
                    color="primary"
                    format="24hr"
                  />
                  <v-card-actions>
                    <v-spacer />
                    <v-btn color="primary" variant="text" @click="timeMenu = false">OK</v-btn>
                  </v-card-actions>
                </v-card>
              </v-menu>
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
            label="Add Images"
          />

          <template v-if="showImage">
            <div
              v-if="images.length > 0"
              class="image-grid mt-2"
            >
              <div
                v-for="(image, i) in images"
                :key="i"
                class="image-tile"
              >
                <v-img
                  :alt="`Image ${i + 1} of ${images.length}`"
                  aspect-ratio="1"
                  cover
                  :src="imageSrc(image)"
                />
                <div class="image-tile__actions">
                  <v-btn
                    :aria-label="`Move image ${i + 1} earlier`"
                    density="comfortable"
                    :disabled="i === 0"
                    icon="$mdi-chevron-left"
                    size="x-small"
                    variant="flat"
                    @click="moveImage(i, -1)"
                  />
                  <v-btn
                    :aria-label="`Remove image ${i + 1}`"
                    color="error"
                    density="comfortable"
                    icon="$mdi-delete"
                    size="x-small"
                    variant="flat"
                    @click="removeImage(i)"
                  />
                  <v-btn
                    :aria-label="`Move image ${i + 1} later`"
                    density="comfortable"
                    :disabled="i === images.length - 1"
                    icon="$mdi-chevron-right"
                    size="x-small"
                    variant="flat"
                    @click="moveImage(i, 1)"
                  />
                </div>
              </div>
            </div>
            <button
              v-if="images.length < MAX_ENTRY_IMAGES"
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
              <div class="drop-zone-placeholder text-center pa-4">
                <v-icon
                  color="grey-lighten-1"
                  size="48"
                >
                  $mdi-image-plus
                </v-icon>
                <div class="text-grey mt-2 text-body-2">
                  Click, drag & drop, or paste images
                </div>
              </div>
            </button>
            <div
              v-else
              class="text-grey mt-2 text-body-2"
            >
              An entry holds up to {{ MAX_ENTRY_IMAGES }} images. Remove one to add another.
            </div>
            <v-btn
              v-if="images.length > 0"
              class="mt-2"
              color="error"
              size="small"
              variant="text"
              @click="clearImages"
            >
              Remove All Images
            </v-btn>
            <!--
              Hidden and opened programmatically by the button above, so it never
              receives focus in normal use. It still needs a name: assistive technology
              can reach it directly, and an unlabelled file input announces as nothing
              more useful than "button".
            -->
            <input
              id="diary-entry-image-input"
              ref="fileInputRef"
              accept="image/jpeg,image/png,image/gif,image/webp"
              aria-label="Choose image files for this diary entry"
              multiple
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
  import { onMounted, onUnmounted, ref, watch } from 'vue'
  import { type DiaryEntryImage, imageSrc, type JourneyMode, MAX_ENTRY_IMAGES } from '@/services/models/diaryEntry'

  const journeyModeItems: { label: string; value: JourneyMode; icon: string }[] = [
    { label: 'As the Crow Flies', value: 'crow-flies', icon: '$mdi-bird' },
    { label: 'Walking', value: 'walking', icon: '$mdi-walk' },
    { label: 'Car', value: 'car', icon: '$mdi-car' },
    { label: 'Train', value: 'train', icon: '$mdi-train' },
    { label: 'Boat', value: 'boat', icon: '$mdi-ferry' },
  ]

  const props = defineProps<{isEdit?: boolean, date: Date, location: string, entry: string, mapLocation: string, showMap: boolean, fromLocation: string, toLocation: string, showJourney: boolean, journeyMode: JourneyMode, images?: DiaryEntryImage[]}>()
  const date = ref<Date>(new Date(props.date))
  const time = ref<string>(dayjs(props.date).format('HH:mm'))
  const timeMenu = ref<boolean>(false)
  const location = ref<string>(props.location)
  const entry = ref<string>(props.entry)
  const mapLocation = ref<string>(props.mapLocation)
  const showMap = ref<boolean>(props.showMap)
  const fromLocation = ref<string>(props.fromLocation)
  const toLocation = ref<string>(props.toLocation)
  const showJourney = ref<boolean>(props.showJourney)
  const journeyMode = ref<JourneyMode>(props.journeyMode)
  const images = ref<DiaryEntryImage[]>([...(props.images ?? [])])
  const showImage = ref<boolean>(images.value.length > 0)
  const isDragging = ref<boolean>(false)
  const fileInputRef = ref<HTMLInputElement | null>(null)

  const emit = defineEmits({
    submit (payload: { date: Date, location: string, entry: string, mapLocation: string, showMap: boolean, fromLocation: string, toLocation: string, showJourney: boolean, journeyMode: JourneyMode, images: DiaryEntryImage[] }) {
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
        images: [...images.value],
      })
    }
  }

  function readFile (file: File) {
    return new Promise<DiaryEntryImage>((resolve, reject) => {
      const reader = new FileReader()
      reader.onload = e => {
        const dataUrl = e.target?.result as string
        const comma = dataUrl.indexOf(',')
        resolve({ data: dataUrl.slice(comma + 1), contentType: file.type })
      }
      reader.onerror = () => reject(reader.error)
      reader.readAsDataURL(file)
    })
  }

  // Files are read concurrently but kept in the order they were chosen, and never past the
  // limit: anything beyond it is dropped here rather than rejected by the API on save. The
  // handlers do not await this, so a file that cannot be read is skipped rather than thrown.
  async function addFiles (files: File[]) {
    const room = MAX_ENTRY_IMAGES - images.value.length
    const accepted = files.filter(f => f.type.startsWith('image/')).slice(0, Math.max(0, room))
    if (accepted.length === 0) return
    showImage.value = true
    const results = await Promise.allSettled(accepted.map(f => readFile(f)))
    const read = results
      .filter((r): r is PromiseFulfilledResult<DiaryEntryImage> => r.status === 'fulfilled')
      .map(r => r.value)
    images.value = [...images.value, ...read].slice(0, MAX_ENTRY_IMAGES)
  }

  function handleDrop (event: DragEvent) {
    isDragging.value = false
    addFiles(Array.from(event.dataTransfer?.files ?? []))
  }

  function handleFileSelect (event: Event) {
    const input = event.target as HTMLInputElement
    addFiles(Array.from(input.files ?? []))
    // Choosing the same file again must still fire change.
    input.value = ''
  }

  function moveImage (index: number, by: number) {
    const target = index + by
    if (target < 0 || target >= images.value.length) return
    const next = [...images.value]
    const moved = next.splice(index, 1)[0]
    next.splice(target, 0, moved)
    images.value = next
  }

  function removeImage (index: number) {
    images.value = images.value.filter((_, i) => i !== index)
  }

  function triggerFileInput () {
    fileInputRef.value?.click()
  }

  function clearImages () {
    images.value = []
    showImage.value = false
  }

  function handleWindowPaste (event: ClipboardEvent) {
    const items = event.clipboardData?.items
    if (!items) return
    const files = Array.from(items)
      .filter(item => item.type.startsWith('image/'))
      .map(item => item.getAsFile())
      .filter((file): file is File => file !== null)
    addFiles(files)
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
  watch(() => props.images, newVal => {
    images.value = [...(newVal ?? [])]
  })
  watch(showImage, newVal => {
    if (!newVal) {
      images.value = []
    }
  })
</script>

<style scoped>
  .editor-scroll-container {
    display: flex;
    flex-direction: column;
    max-height: 90dvh;
    overflow: hidden;
  }

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

  .image-grid {
    display: grid;
    gap: 8px;
    grid-template-columns: repeat(auto-fill, minmax(110px, 1fr));
  }

  .image-tile {
    border-radius: 8px;
    overflow: hidden;
    position: relative;
  }

  .image-tile__actions {
    background: rgba(0, 0, 0, 0.45);
    bottom: 0;
    display: flex;
    justify-content: space-between;
    left: 0;
    padding: 2px;
    position: absolute;
    right: 0;
  }
</style>
