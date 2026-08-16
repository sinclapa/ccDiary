<template>
  <v-dialog max-width="560px" :model-value="modelValue" @update:model-value="$emit('update:modelValue', $event)">
    <v-card rounded="xl">
      <v-card-title class="d-flex align-center gap-2 text-h6 text-primary">
        <v-icon icon="$mdi-alert-circle-outline" />
        {{ title }}
      </v-card-title>
      <v-card-text>
        <p class="mb-3">Are you sure you want to permanently delete this {{ itemType }}?</p>
        <div class="delete-meta pa-3">
          <div v-for="item in items" :key="item.label">
            <strong>{{ item.label }}:</strong> {{ item.value }}
          </div>
        </div>
      </v-card-text>
      <v-card-actions class="px-4 pb-4">
        <v-spacer />
        <v-btn variant="text" @click="$emit('cancel')">Cancel</v-btn>
        <v-btn color="primary" variant="flat" @click="$emit('confirm')">{{ confirmLabel }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
  defineProps<{
    modelValue: boolean
    title: string
    itemType: string
    confirmLabel: string
    items: { label: string; value: string }[]
  }>()

  defineEmits<{
    'update:modelValue': [value: boolean]
    confirm: []
    cancel: []
  }>()
</script>

<style scoped>
  .delete-meta {
    border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
    border-radius: 12px;
    background: rgb(var(--v-theme-surface));
  }
</style>
