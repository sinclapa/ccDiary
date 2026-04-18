<template>
  <v-container>
    <h1 class="text-h5 mb-4">Access Requests</h1>
    <v-progress-linear v-if="loading" color="primary" height="2" indeterminate />
    <v-data-table
      :headers="headers"
      :items="requests"
      no-data-text="No pending access requests"
    >
      <template #item.requestedAt="{ item }">
        {{ new Date(item.requestedAt).toLocaleDateString() }}
      </template>
      <template #item.actions="{ item }">
        <div class="d-flex gap-2">
          <v-btn
            :id="item.accessRequestId + '_approve'"
            color="success"
            :loading="processingId === item.accessRequestId"
            size="small"
            variant="tonal"
            @click="approve(item)"
          >
            Approve
          </v-btn>
          <v-btn
            :id="item.accessRequestId + '_decline'"
            color="error"
            :loading="processingId === item.accessRequestId"
            size="small"
            variant="tonal"
            @click="decline(item)"
          >
            Decline
          </v-btn>
        </div>
      </template>
    </v-data-table>
    <v-alert
      v-if="feedbackMessage"
      class="mt-4"
      closable
      :type="feedbackType"
      @click:close="clearFeedback"
    >
      {{ feedbackMessage }}
      <div v-if="redeemUrl" class="mt-2">
        <span class="text-body-2">Invitation link (share if email doesn't arrive):</span>
        <a class="d-block text-body-2 text-truncate" :href="redeemUrl" rel="noopener" target="_blank">{{ redeemUrl }}</a>
      </div>
    </v-alert>
  </v-container>
</template>

<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import type { AccessRequest } from '@/services/models/accessRequest'
  import { approveRequest, declineRequest, getPendingRequests } from '@/services/modules/adminService'

  const loading = ref(false)
  const requests = ref<AccessRequest[]>([])
  const processingId = ref<string | null>(null)
  const feedbackMessage = ref('')
  const feedbackType = ref<'success' | 'error'>('success')
  const redeemUrl = ref<string | null>(null)

  const headers = [
    { title: 'Name', value: 'displayName' },
    { title: 'Email', value: 'email' },
    { title: 'Requested', value: 'requestedAt' },
    { title: 'Actions', value: 'actions', sortable: false },
  ]

  async function load () {
    loading.value = true
    try {
      requests.value = await getPendingRequests()
    } finally {
      loading.value = false
    }
  }

  function clearFeedback () {
    feedbackMessage.value = ''
    redeemUrl.value = null
  }

  async function approve (item: AccessRequest) {
    processingId.value = item.accessRequestId
    const result = await approveRequest(item.accessRequestId)
    processingId.value = null
    if (result.ok) {
      redeemUrl.value = result.redeemUrl
      feedbackType.value = 'success'
      feedbackMessage.value = result.redeemUrl
        ? `${item.displayName} has been approved. An invitation email has been sent.`
        : `${item.displayName} has been approved.`
      await load()
    } else {
      feedbackType.value = 'error'
      feedbackMessage.value = 'Failed to approve request. Please try again.'
    }
  }

  async function decline (item: AccessRequest) {
    processingId.value = item.accessRequestId
    const ok = await declineRequest(item.accessRequestId)
    processingId.value = null
    if (ok) {
      feedbackType.value = 'success'
      feedbackMessage.value = `${item.displayName}'s request has been declined.`
      await load()
    } else {
      feedbackType.value = 'error'
      feedbackMessage.value = 'Failed to decline request. Please try again.'
    }
  }

  onMounted(load)
</script>
