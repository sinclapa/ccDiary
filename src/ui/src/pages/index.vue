<template>
  <header>
    <button v-if="state.isAuthenticated" @click="data" class="btn">Data</button><br/>
    {{ api }}<br/>
    {{ weather }}<br/>
  </header>

</template>

<script setup lang="ts">
import { state } from '@/config/msalConfig'

var api = import.meta.env.VITE_API
const weather = ref(null)

async function data() {
    //const headers = { "X-name": "Bearer "}
    fetch(api)
        .then(response => response.json())
        .then( data => weather.value = data)
}

</script>
