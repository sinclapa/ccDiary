<template>
   <v-sheet class="mx-auto" width="300">
    <v-form fast-fail @submit.prevent @submit="submit()">
      <v-card
        prepend-icon="mdi-book-open-variant-outline"
        title="Add Diary"
      >
        <v-card-text>
          <v-text-field
            v-model="title"
            label="Title"
          ></v-text-field>

          <v-text-field
            v-model="author"
            label="Author"
          ></v-text-field>

          <v-text-field
            v-model="description"
            label="Description"
          ></v-text-field>
        </v-card-text>
        <v-divider></v-divider>

        <v-card-actions>
          <v-spacer></v-spacer>
          <v-row>
            <v-col>
              <v-btn
                text="Close"
                variant="plain"
                @click="close"
                block
              ></v-btn>
            </v-col>
            <v-col>
              <v-btn
                color="primary"
                text="Save"
                type="submit"
                variant="tonal"
                block
              ></v-btn>
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
  submit(payload: { title: string, author: string, description: string }) {
  },
  close() {
  }
})

function close() {
  emit("close")
}
function submit() {
  emit("submit", {title: title.value, author: author.value, description: description.value})
}
</script>
