import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Geliştirmede (npm run dev) API istekleri Docker'daki gateway'e gider; böylece CORS gerekmez.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5180,
    strictPort: true,
    proxy: {
      '/api': 'http://localhost:8000',
    },
  },
})
