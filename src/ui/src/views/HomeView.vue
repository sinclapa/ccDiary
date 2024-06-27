<script setup lang="ts">
import { useAuth } from '@/auth';
import { ref } from 'vue'
import TheWelcome from '../components/TheWelcome.vue'

const auth = useAuth();

async function login() {
    await auth.msalInstance.initialize();
    await auth.msalInstance.loginPopup();
    const myAccounts = auth.msalInstance.getAllAccounts();
    auth.account = myAccounts[0];

    const response = await auth.msalInstance.acquireTokenSilent({
        account: auth.account,
        scopes: [`api://${import.meta.env.VITE_CLIENTID}/Diary.Update`]
    });

    auth.token = response.accessToken;
}

async function logout() {

}

var version =  import.meta.env.VITE_VERSION + "." + import.meta.env.VITE_BUILDNUMBER
var api = import.meta.env.VITE_API
const weather = ref(null)

async function data() {
    const headers = { "Authorization": "Bearer " + auth.token}
    fetch(api, { headers })
        .then(response => response.json())
        .then( data => weather.value = data)
}
</script>

<template>
  <main>
    <TheWelcome />
    <button v-if="!auth.account" @click="login" class="btn">Click here to login</button>
    <button v-if="auth.account" @click="logout" class="btn">Click here to logout</button>
    <button @click="data" class="btn">Data</button>
    <div v-if="auth.account">{{  auth.account.name  }}</div><br/>
    {{ version }}<br/>
    {{ api }}<br/>
    {{ weather }}<br/>
  </main>
</template>
