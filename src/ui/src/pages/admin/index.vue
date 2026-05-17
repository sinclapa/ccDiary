<template>
  <v-container>
    <h1 class="text-h5 mb-4">Access Requests</h1>
    <v-progress-linear v-if="loading" color="primary" height="2" indeterminate />
    <v-tabs v-model="tab" color="primary">
      <v-tab value="pending">Pending</v-tab>
      <v-tab value="approved">Approved</v-tab>
      <v-tab value="declined">Declined</v-tab>
    </v-tabs>
    <v-tabs-window v-model="tab">
      <v-tabs-window-item value="pending">
        <v-data-table
          :headers="pendingHeaders"
          :items="pendingRequests"
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
      </v-tabs-window-item>

      <v-tabs-window-item value="approved">
        <v-data-table
          :headers="approvedHeaders"
          :items="approvedRequests"
          no-data-text="No approved access requests"
        >
          <template #item.requestedAt="{ item }">
            {{ new Date(item.requestedAt).toLocaleDateString() }}
          </template>
          <template #item.processedAt="{ item }">
            {{ item.processedAt ? new Date(item.processedAt).toLocaleDateString() : '—' }}
          </template>
          <template #item.actions="{ item }">
            <div class="d-flex gap-2 align-center">
              <v-btn
                :id="item.accessRequestId + '_resend'"
                color="primary"
                :loading="processingId === item.accessRequestId"
                size="small"
                variant="tonal"
                @click="resend(item)"
              >
                Resend Email
              </v-btn>
              <div class="copy-wrap">
                <v-btn
                  :id="item.accessRequestId + '_copy'"
                  :disabled="!item.inviteRedeemUrl"
                  icon
                  size="small"
                  variant="text"
                  @click="item.inviteRedeemUrl && copyLink(item.inviteRedeemUrl)"
                >
                  <v-icon>$mdi-content-copy</v-icon>
                </v-btn>
                <span class="copy-tooltip">
                  {{ item.inviteRedeemUrl ? 'Copy invitation link' : 'No invitation link available' }}
                </span>
              </div>
              <div class="copy-wrap">
                <v-btn
                  :id="item.accessRequestId + '_delete'"
                  color="primary"
                  icon
                  :loading="processingId === item.accessRequestId"
                  size="small"
                  variant="text"
                  @click="deleteEntry(item)"
                >
                  <v-icon>$mdi-delete</v-icon>
                </v-btn>
                <span class="copy-tooltip">Delete record</span>
              </div>
            </div>
          </template>
        </v-data-table>
      </v-tabs-window-item>

      <v-tabs-window-item value="declined">
        <v-data-table
          :headers="declinedHeaders"
          :items="declinedRequests"
          no-data-text="No declined access requests"
        >
          <template #item.requestedAt="{ item }">
            {{ new Date(item.requestedAt).toLocaleDateString() }}
          </template>
          <template #item.processedAt="{ item }">
            {{ item.processedAt ? new Date(item.processedAt).toLocaleDateString() : '—' }}
          </template>
          <template #item.actions="{ item }">
            <div class="copy-wrap">
              <v-btn
                :id="item.accessRequestId + '_delete'"
                color="primary"
                icon
                :loading="processingId === item.accessRequestId"
                size="small"
                variant="text"
                @click="deleteEntry(item)"
              >
                <v-icon>$mdi-delete</v-icon>
              </v-btn>
              <span class="copy-tooltip">Delete record</span>
            </div>
          </template>
        </v-data-table>
      </v-tabs-window-item>
    </v-tabs-window>

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
  import { computed, onMounted, ref, watch } from 'vue'
  import type { AccessRequest } from '@/services/models/accessRequest'
  import { approveRequest, declineRequest, deleteRequest, getAllRequests, resendInvitation } from '@/services/modules/adminService'

  const loading = ref(false)
  const requests = ref<AccessRequest[]>([])
  const tab = ref('pending')
  const processingId = ref<string | null>(null)
  const feedbackMessage = ref('')
  const feedbackType = ref<'success' | 'error'>('success')
  const redeemUrl = ref<string | null>(null)

  function byDates (a: AccessRequest, b: AccessRequest) {
    const byRequested = b.requestedAt.localeCompare(a.requestedAt)
    if (byRequested !== 0) return byRequested
    return (b.processedAt ?? '').localeCompare(a.processedAt ?? '')
  }

  const pendingRequests = computed(() => requests.value.filter(r => r.status === 'pending'))
  const approvedRequests = computed(() => requests.value.filter(r => r.status === 'approved').sort(byDates))
  const declinedRequests = computed(() => requests.value.filter(r => r.status === 'declined').sort(byDates))

  const pendingHeaders = [
    { title: 'Name', value: 'displayName' },
    { title: 'Email', value: 'email' },
    { title: 'Requested', value: 'requestedAt' },
    { title: 'Actions', value: 'actions', sortable: false },
  ]

  const approvedHeaders = [
    { title: 'Name', value: 'displayName' },
    { title: 'Email', value: 'email' },
    { title: 'Requested', value: 'requestedAt' },
    { title: 'Approved', value: 'processedAt' },
    { title: 'Actions', value: 'actions', sortable: false },
  ]

  const declinedHeaders = [
    { title: 'Name', value: 'displayName' },
    { title: 'Email', value: 'email' },
    { title: 'Requested', value: 'requestedAt' },
    { title: 'Declined', value: 'processedAt' },
    { title: 'Actions', value: 'actions', sortable: false },
  ]

  async function load () {
    loading.value = true
    try {
      requests.value = await getAllRequests()
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

  async function resend (item: AccessRequest) {
    processingId.value = item.accessRequestId
    const result = await resendInvitation(item.accessRequestId)
    processingId.value = null
    if (result.ok) {
      redeemUrl.value = result.redeemUrl
      feedbackType.value = 'success'
      feedbackMessage.value = `Invitation email resent to ${item.displayName}.`
    } else {
      feedbackType.value = 'error'
      feedbackMessage.value = 'Failed to resend invitation. The invitation link may no longer be available.'
    }
  }

  async function deleteEntry (item: AccessRequest) {
    processingId.value = item.accessRequestId
    const ok = await deleteRequest(item.accessRequestId)
    processingId.value = null
    if (ok) {
      feedbackType.value = 'success'
      feedbackMessage.value = `${item.displayName}'s record has been deleted.`
      await load()
    } else {
      feedbackType.value = 'error'
      feedbackMessage.value = 'Failed to delete record. Please try again.'
    }
  }

  function copyLink (url: string) {
    navigator.clipboard.writeText(url)
  }

  watch(tab, clearFeedback)

  onMounted(load)
</script>

<style scoped>
.copy-wrap {
  position: relative;
  display: inline-flex;
}

.copy-tooltip {
  position: absolute;
  bottom: calc(100% + 6px);
  left: 50%;
  transform: translateX(-50%);
  background: rgb(var(--v-theme-primary));
  color: #fff;
  font-size: 0.72rem;
  white-space: nowrap;
  padding: 3px 8px;
  border-radius: 4px;
  pointer-events: none;
  opacity: 0;
  transition: opacity 0.15s ease;
  z-index: 100;
}

.copy-wrap:hover .copy-tooltip {
  opacity: 1;
}
</style>
