<template>
  <header>
    <button v-if="state.isAuthenticated" @click="data" class="btn">Data</button><br/>
    {{ api }}<br/>
    {{ weather }}<br/>
  </header>
</template>

<script setup lang="ts">
import { state } from '@/services/authentication/msalConfig'

var api = new URL('v1/WeatherForecast/Get', import.meta.env.VITE_API)
const weather = ref(null)

async function data() {
  fetch(api)
    .then(response => response.json())
    .then(data => weather.value = data)
}

</script>
