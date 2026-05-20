<template>
  <v-sheet rounded="xl">
    <v-form @submit.prevent="submit">
      <v-card
        class="editor-scroll-container"
        prepend-icon="$mdi-book-open-variant-outline"
        rounded="xl"
        :title="addMode ? 'Add Diary' : 'Edit Diary'"
      >
        <template #append>
          <v-btn aria-label="Cancel" icon variant="text" @click="close">
            <v-icon>$mdi-close</v-icon>
          </v-btn>
        </template>
        <v-card-text style="overflow-y: auto; flex: 1 1 auto;">
          <v-text-field
            id="title"
            v-model="title"
            color="primary"
            label="Title"
            :rules="titleRules"
          />

          <v-text-field
            id="author"
            v-model="author"
            color="primary"
            label="Author"
            :rules="authorRules"
          />

          <v-textarea
            id="description"
            v-model="description"
            auto-grow
            color="primary"
            label="Description"
            rows="3"
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
  import { SubmitEventPromise } from 'vuetify'

  const props = defineProps<{title: string, author: string, description: string, addMode: boolean}>()
  const title = ref<string>(props.title)
  const author = ref<string>(props.author)
  const description = ref<string>(props.description)
  const addMode = ref<boolean>(props.addMode)
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
    const { valid } = await submitEventPromise
    if (valid) {
      emit('submit', { title: title.value, author: author.value, description: description.value })
    }
  }
</script>

<style scoped>
  .editor-scroll-container {
    display: flex;
    flex-direction: column;
    max-height: 90dvh;
    overflow: hidden;
  }
</style>
