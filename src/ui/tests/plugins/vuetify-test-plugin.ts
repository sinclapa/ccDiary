import { createVuetify } from 'vuetify'
import { config } from '@vue/test-utils'
import 'vuetify/styles'
import { aliases, mdi } from 'vuetify/iconsets/mdi'

const vuetify = createVuetify({
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi },
  },
})

config.global.plugins = [vuetify]

export default vuetify
