<template>
  <header>
    <h1>Weather App</h1>
    <v-btn v-if="state.isAuthenticated" class="btn" @click="data">Fetch Weather Data</v-btn><br>
    API: {{ api }}<br>
    <v-table v-if="weather">
      <thead>
        <tr>
          <th>
            Date
          </th>
          <th>
            Temp C
          </th>
          <th>
            Temp F
          </th>
          <th>
            Summary
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="item in weather"
          :key="item.date"
        >
          <td>{{ item.date }}</td>
          <td>{{ item.temperatureC }}</td>
          <td>{{ item.temperatureF }}</td>
          <td>{{ item.summary }}</td>
        </tr>
      </tbody>
    </v-table>
  </header>
</template>

<script setup lang="ts">
  import { state } from '@/services/authentication/msalConfig'
  import { getAppConfigField } from '@/utils/appConfig';

  const api = new URL('v1/WeatherForecast/Get', getAppConfigField('VITE_API'))
  const weather = ref()

  async function data () {
    weather.value = await fetch(api)
      .then(response => response.json())
  }

</script>
