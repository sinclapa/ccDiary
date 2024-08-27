<template>
{{ diary }}
<v-date-picker @update:month="updateMonth" @update:year="updateMonth" @update:model-value="updateMonth" min="2024-08-22"></v-date-picker>
</template>
<script setup lang="ts">
import { diaryAPI } from '@/services/modules/diaryService';
import Diary from '@/services/models/diary'
const diary = ref(new Diary('', '', '') as Diary | undefined)
const route = useRoute('/diaries/[id]')
const id = route.params.id

async function data(id: string) {
  diary.value = await diaryAPI.getDiary(id)
}

function updateMonth(x: any | undefined)
{
  console.warn(x)
}

onMounted(() => {
  data(id)
})
</script>
