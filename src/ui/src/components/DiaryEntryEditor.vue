<template>
  <v-sheet class="mx-auto" width="400">
    <v-form @submit.prevent="submit">
      <v-card
        prepend-icon="mdi-pen"
        title="Add Diary Entry"
      >
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
            color="primary"
            label="Show Map"
            hide-details
            class="mb-2"
          />

          <v-text-field
            v-if="showMap"
            id="map-location"
            v-model="mapLocation"
            label="Map Location"
            hint="Enter a place name to display on the map"
            persistent-hint
          />
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
  import { watch } from 'vue'

  const props = defineProps<{date: Date, location: string, entry: string, mapLocation: string, showMap: boolean}>()
  const date = ref<Date>(new Date(props.date))
  const time = ref<string>(dayjs(props.date).format('HH:mm'))
  const location = ref<string>(props.location)
  const entry = ref<string>(props.entry)
  const mapLocation = ref<string>(props.mapLocation)
  const showMap = ref<boolean>(props.showMap)
  const emit = defineEmits({
    submit (payload: { date: Date, location: string, entry: string, mapLocation: string, showMap: boolean }) {
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
      })
    }
  }

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
</script>
