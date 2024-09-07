<template>
  <v-sheet class="mx-auto" width="300">
    <v-form @submit.prevent="submit">
      <v-card
        prepend-icon="mdi-book-open-variant-outline"
        title="Add Diary"
      >
        <v-card-text>
          <v-text-field
            id="title"
            v-model="title"
            label="Title"
            :rules="titleRules"
          />

          <v-text-field
            id="author"
            v-model="author"
            label="Author"
            :rules="authorRules"
          />

          <v-text-field
            id="description"
            v-model="description"
            label="Description"
          />
        </v-card-text>
        <v-divider />

        <v-card-actions>
          <v-spacer />
          <v-row>
            <v-col>
              <v-btn
                block
                id="close"
                text="Close"
                variant="plain"
                @click="close"
              />
            </v-col>
            <v-col>
              <v-btn
                block
                id="save"
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
  import { SubmitEventPromise } from 'vuetify'

  const props = defineProps(['title', 'author', 'description'])
  const title = ref(props.title)
  const author = ref(props.author)
  const description = ref(props.description)
  const emit = defineEmits({
    submit (payload: { title: string, author: string, description: string }) {
      return payload
    },
    close () {
      return true
    },
  })
  const titleRules = [
    (v:string) => !!v || 'Title is required',
    (v:string) => (v && v.length >= 5) || 'Title must be at least 5 characters',
  ]

  const authorRules = [
    (v:string) => !!v || 'Author is required',
    (v:string) => (v && v.length >= 5) || 'Author must be at least 5 characters',
  ]

  function close () {
    emit('close')
  }

  async function submit (submitEventPromise: SubmitEventPromise) {
    const {valid} = await submitEventPromise
    if (valid) {
      emit('submit', { title: title.value, author: author.value, description: description.value })
    }
  }
</script>
