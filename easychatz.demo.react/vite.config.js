import { cpSync, createReadStream, existsSync, statSync } from 'node:fs'
import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { easyChatzOutput } from './scripts/paths.mjs'

const BLAZOR_FOLDERS = ['_framework', '_content']

const CONTENT_TYPES = {
  '.js': 'text/javascript',
  '.mjs': 'text/javascript',
  '.wasm': 'application/wasm',
  '.json': 'application/json',
  '.css': 'text/css',
  '.svg': 'image/svg+xml',
  '.dll': 'application/octet-stream',
  '.dat': 'application/octet-stream',
  '.blat': 'application/octet-stream',
}

function easyChatz() {
  return {
    name: 'easychatz',

    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = decodeURIComponent((req.url ?? '').split('?')[0])
        if (!BLAZOR_FOLDERS.some(folder => url.startsWith(`/${folder}/`))) {
          return next()
        }

        const file = path.join(easyChatzOutput, url)
        if (!file.startsWith(easyChatzOutput + path.sep) || !existsSync(file) || !statSync(file).isFile()) {
          return next()
        }

        res.setHeader('Content-Type', CONTENT_TYPES[path.extname(file)] ?? 'application/octet-stream')
        res.setHeader('Cache-Control', 'no-cache')
        createReadStream(file).pipe(res)
      })
    },

    writeBundle(options) {
      for (const folder of BLAZOR_FOLDERS) {
        cpSync(path.join(easyChatzOutput, folder), path.join(options.dir, folder), { recursive: true })
      }
    },
  }
}

export default defineConfig({
  plugins: [react(), easyChatz()],
  server: {
    port: 5174,
    strictPort: true,
  },
})
