# Frontend Testing

本文约束 MedicalRecognition 前端、适配层和 API Client 测试。共同纪律见 [Testing Baseline](testing-baseline.md)，宿主验收见 [Testing](testing.md)。

## 1. 必须覆盖

1. API Client：OpenAPI 来源、预处理、`kiota-lock.json`、`/auth/login` 排除、公开入口、类型和构建。
2. 适配层：请求参数、分页筛选、组织范围、可空字段、枚举、日期、文件和 DTO 映射。
3. 页面组件：loading、空态、禁用、校验、提交、防重复、成功刷新和失败恢复。
4. 路由权限：宿主路径、可信上下文、可见性、可操作性、只读边界和请求范围。
5. 列表弹层：分页、排序、筛选复位、跨页选择、打开、取消、关闭、遮罩、焦点和再次打开。

## 2. 测试质量

- 通用 RED、等价重构基线、静态 finding、批次和证据复用规则只遵循 [Testing Baseline](testing-baseline.md)。
- 断言用户可观察结果、请求参数和状态变化，不只验证组件能渲染或按钮存在。
- 测试不依赖顺序、共享可变状态、不可控网络或固定 sleep。
- 生成代码不手改；类型问题在 OpenAPI、生成配置、入口或适配层解决。
- mock 位于 Client/适配层边界，并保持真实契约形态。

## 3. 异常边界

平台错误处理策略以 [Frontend API Client](frontend-api-client.md#5-平台错误边界) 为权威。失败测试只验证本层恢复：结束 loading、保留输入/已有数据、保持必要弹窗、不误刷新、不误报成功；宿主统一提示通过宿主浏览器验收。

未发起 API 的表单、可信上下文和客户端文件校验可以验证本地提示。

## 4. 前端执行差异

在 [Testing Baseline](testing-baseline.md) 的相应检查点运行受影响组件、页面、hooks、Context 和适配层测试，以及适用的 lint、typecheck 和 build。涉及路由、菜单、权限、认证、组织上下文、API 失败或真实交互时继续执行宿主验证；报告方式遵循 [Testing](testing.md)。

客户端自动化和构建通过只代表客户端验证面通过，不能单独宣告页面功能完成。
