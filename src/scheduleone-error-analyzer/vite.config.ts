import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

function normalizeBasePath(value?: string) {
  if (!value || value === '/') {
    return '/'
  }

  const withLeadingSlash = value.startsWith('/') ? value : `/${value}`
  return withLeadingSlash.endsWith('/') ? withLeadingSlash : `${withLeadingSlash}/`
}

export default defineConfig({
  plugins: [react()],
  base: normalizeBasePath(process.env.GITHUB_PAGES_BASE_PATH),
})
