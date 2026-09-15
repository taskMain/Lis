/// <reference types="vite/client" />

interface Window {
  __MICRO_APP_BASE_ROUTE__?: string
  __MICRO_APP_NAME__?: string
  mount?: () => void
  unmount?: () => void
}
