import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import { redirectBasePlugin } from '@dy/vite-plugin-redirect-base'

export default defineConfig({
  base: '/subApps/medical-recognition/',
  plugins: [react(), redirectBasePlugin({ basePath: '/subApps/medical-recognition/' })],
  server: { port: 3008, headers: { 'Access-Control-Allow-Origin': '*' } },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    css: false,
    restoreMocks: true,
    testTimeout: 20000,
    /**
     * 写失败用例会让页面 `submit` 的 promise 进入拒绝态：该 promise 被交给 antd Modal 的 `onOk`，
     * 而 antd 不消费返回值，于是产生 unhandled rejection。按 Frontend API Client 第 5 节页面
     * **不得**为此新增捕获、包装或提示（错误由宿主统一展示），因此这是实现与 antd 契约的固有组合，
     * 不是可修的页面缺陷。
     *
     * 处理方式：各条相关用例在**用例内局部**安装 `process.on('unhandledRejection')` 观测器、
     * 用例结束即移除，并断言恰好收到该笔拒绝（写法见 `RecognitionAmounts.test.tsx` 的 C10 用例）。
     * 这里**不再**全局忽略未处理拒绝：任何未预期到的拒绝都应让用例真实判红。
     */
  },
})
