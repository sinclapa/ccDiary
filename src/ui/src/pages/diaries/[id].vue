<template>
{{ diary }}
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

onMounted(() => {
  data(id)
})
</script>
