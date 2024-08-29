<template>
  <v-sheet class="mx-auto" width="300">
    <v-form fast-fail @submit="submit()" @submit.prevent>
      <v-card
        prepend-icon="mdi-book-open-variant-outline"
        title="Add Diary"
      >
        <v-card-text>
          <v-text-field
            v-model="title"
            label="Title"
          />

          <v-text-field
            v-model="author"
            label="Author"
          />

          <v-text-field
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
                text="Close"
                variant="plain"
                @click="close"
              />
            </v-col>
            <v-col>
              <v-btn
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

  function close () {
    emit('close')
  }
  function submit () {
    emit('submit', { title: title.value, author: author.value, description: description.value })
  }
</script>
