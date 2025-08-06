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

  const props = defineProps<{date: Date, location: string, entry: string}>()
  const date = ref<Date>(new Date(props.date))
  const time = ref<string>(dayjs(props.date).format('HH:mm'))
  const location = ref<string>(props.location)
  const entry = ref<string>(props.entry)
  const emit = defineEmits({
    submit (payload: { date: Date, location: string, entry: string }) {
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
      emit('submit', { date: entryDate, location: location.value, entry: entry.value })
    }
  }

  watch(() => props.location, (newVal) => {
    location.value = newVal
  })
  watch(() => props.entry, (newVal) => {
    entry.value = newVal
  })
  watch(() => props.date, (newVal) => {
    date.value = new Date(newVal)
    time.value = dayjs(newVal).format('HH:mm')
  })
</script>
