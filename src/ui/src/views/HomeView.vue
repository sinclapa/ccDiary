<script setup lang="ts">
import { ref } from 'vue'
import TheWelcome from '../components/TheWelcome.vue'
var x =  import.meta.env.VITE_VERSION + "." + import.meta.env.VITE_BUILDNUMBER
var api = import.meta.env.VITE_API
const weather = ref(null)
fetch(api)
    .then(response => response.json())
    .then( data => weather.value = data)
</script>

<template>
  <main>
    <TheWelcome />
    {{ x }}<br/>
    {{ api }}<br/>
    {{ weather }}<br/>
  </main>
</template>
