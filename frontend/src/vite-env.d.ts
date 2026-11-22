/// <reference types="vite/client" />

// Typed VITE_ environment variables. These are bundled into the browser: never put secrets here.
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
