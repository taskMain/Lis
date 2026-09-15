# dy-medical-recognition

`检验检查结果互认平台` 是基于 React + Vite 的 micro-app 子应用。

## Commands

```bash
pnpm -F dy-medical-recognition dev
pnpm -F dy-medical-recognition build
pnpm -F dy-medical-recognition lint
```

## Runtime

- 独立运行：`http://localhost:3008/subApps/medical-recognition/`
- 嵌入运行：由宿主通过 `<micro-app>` 注入 `window.__MICRO_APP_BASE_ROUTE__`
- 路由 basename：默认 `/subApps/medical-recognition`
