import { resolve } from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Geliştirmede (npm run dev) API istekleri Docker'daki gateway'e gider; böylece CORS gerekmez.
export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      // İkinci sayfa: sessiz girişin gizli çerçevede döndüğü silent-renew.html.
      input: {
        main: resolve(__dirname, 'index.html'),
        silentRenew: resolve(__dirname, 'silent-renew.html'),
      },
    },
  },
  server: {
    port: 5180,
    strictPort: true,
    proxy: {
      '/api': 'http://localhost:8000',
    },
  },
})
