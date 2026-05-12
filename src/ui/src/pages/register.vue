<template>
  <v-container max-width="480">
    <v-card v-if="!submitted">
      <v-card-title>Request Access</v-card-title>
      <v-card-text>
        <p class="mb-4">Fill in your details to request a contributor account. An admin will review your request and you'll receive an email invitation when approved.</p>
        <v-form ref="form" @submit.prevent="submit">
          <v-text-field
            v-model="displayName"
            label="Display Name"
            required
            :rules="[v => !!v || 'Name is required']"
          />
          <v-text-field
            v-model="email"
            label="Email"
            required
            :rules="[v => !!v || 'Email is required', v => /.+@.+/.test(v) || 'Must be a valid email']"
            type="email"
          />
          <v-alert
            v-if="error"
            class="mb-4"
            closable
            type="error"
            @click:close="error = ''"
          >{{ error }}</v-alert>
          <v-btn block color="primary" :loading="loading" type="submit">Submit Request</v-btn>
        </v-form>
      </v-card-text>
    </v-card>
    <v-card v-else>
      <v-card-title>Request Submitted</v-card-title>
      <v-card-text>
        <v-icon class="mb-2" color="success" size="48">$mdi-check-circle</v-icon>
        <p>Your access request has been submitted. You will receive a Microsoft invitation email once an admin approves your request.</p>
      </v-card-text>
    </v-card>
  </v-container>
</template>

<script setup lang="ts">
  import { ref, watch } from 'vue'
  import { useRouter } from 'vue-router'
  import { state } from '@/services/authentication/msalConfig'
  import { submitAccessRequest } from '@/services/modules/accessRequestService'

  const router = useRouter()
  watch(() => state.isAuthenticated, (isAuth) => {
    if (isAuth) router.replace('/')
  }, { immediate: true })

  const form = ref()
  const displayName = ref('')
  const email = ref('')
  const loading = ref(false)
  const submitted = ref(false)
  const error = ref('')

  async function submit () {
    const { valid } = await form.value.validate()
    if (!valid) return
    loading.value = true
    error.value = ''
    try {
      await submitAccessRequest(displayName.value, email.value)
      submitted.value = true
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'An error occurred. Please try again.'
    } finally {
      loading.value = false
    }
  }
</script>
