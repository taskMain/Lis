# Pagination

本文是服务端分页公共设计的唯一完整陈述与公共实现索引：形状、命名、窗口校验、SQL 窗口归属与前端控件。后续阶段需要分页时先读本文复用既有实现，不重新设计一套。

分页条款分散在若干规范中，各自约束一个视角（见第 8 节）；本文给出完整设计并指向实现位置，不替代那些条款。后端分层见 [Backend Architecture](backend-architecture.md)，前端 API 边界见 [Frontend API Client](frontend-api-client.md)，前端分层与共享落位见 [Frontend Application](frontend-application.md)。

## 1. 形状

分页请求是「业务筛选平级 + 分页内嵌 `page` 对象」；分页响应是 `items` 平级 + `page` 内嵌对象。

请求：

```json
{
  "organizationCode": "01",
  "hospitalCode": "0101",
  "branchCode": "0101001",
  "page": { "pageIndex": 1, "pageSize": 10 }
}
```

响应：

```json
{
  "items": [],
  "page": { "pageIndex": 1, "pageSize": 10, "totalCount": 0 }
}
```

页码为一基；页容量取值域为 1 到 200。请求的 `pageIndex` 与 `pageSize` **只在 `page` 对象内出现**，不得提升为请求类型的平级字段。

## 2. 公共类型

| 类型 | 角色 | 可见性 | 实现位置 |
|---|---|---|---|
| `PageRequestDto` | 请求分页对象，承载 `PageIndex`、`PageSize` | 公共 | `server/Dy.MedicalRecognition.Application.Contracts/Queries/PageRequestDto.cs` |
| `PageInfoDto` | 响应分页信息，承载 `PageIndex`、`PageSize`、`TotalCount`（长整型） | 公共 | `.../Queries/PageInfoDto.cs` |
| `PageResultDto<T>` | 分页响应，承载 `Items` 与 `Page` | 公共 | `.../Queries/PageResultDto.cs` |
| `PageQueryWindow` | 已校验窗口：`PageIndex`、`PageSize`、`SkipCount` | 应用层内部 | `server/Dy.MedicalRecognition.Application/Queries/PageQueryWindow.cs` |
| `PageSlice<T>` | 仓储到应用层的当页切片：`Items`、`TotalCount` | 应用层内部 | `.../Queries/PageSlice.cs` |

窗口与切片是内部投影类型，不出现在对外类型上：对外只暴露请求分页对象与响应分页信息，`SkipCount` 一类执行字段不进入公共形状。

列表查询请求以组合方式承载分页对象，不继承 `PageRequestDto`：

```csharp
public record MedicalReportListQueryRequest
{
  public PageRequestDto Page { get; init; } = new();
  // 其余筛选字段平级
}
```

## 3. 禁用字段

公共分页类型上不得出现以下字段名：`HasNext`、`Pagination`、`SkipCount`、`PageCount`。

它们分别暗示由客户端推进分页、把框架分页对象直接对外、暴露窗口起点与总页数，都与服务端分页口径不符。`SkipCount` 只允许存在于内部窗口类型上。既有守卫见 `Stage4ContractTests.Paging_contract_does_not_expose_derived_or_internal_paging_fields`。

## 4. 服务端流程

1. 入口校验请求（含内嵌分页对象上的取值域声明）。
2. 建立并校验窗口：`PageQueryWindow.Create(pageIndex, pageSize)`。页码小于 1、页容量小于 1 或超过 200 时按业务拒绝抛出，**此时不发生仓储访问**。窗口校验必须早于第一次仓储访问。
3. 先取总数：计数语句与数据语句共用同一套筛选条件。
4. 再取当页：按偏移与页容量取当页。
5. 映射响应：保留请求窗口与原样总数，`items` 为空时用空数组。

## 5. SQL 窗口归属

分页窗口交给框架/ORM 的 Provider 适配层生成，公共 XML 内**不写** `LIMIT`/`OFFSET`、`TOP` 一类方言分页语法。本项目由 `DataMapper.QueryAsync` 的 `pagination` 参数承载：

```csharp
await dataMapper.QueryAsync<MedicalReportListItem>(
  filter, scope: SqlScope, sqlId: "QueryMedicalReportList", pagination: new Pagination(skipCount, pageSize));
```

计数语句不带排序与窗口，数据语句带稳定排序；两者的筛选条件必须一致，否则总数与当页会不一致。业务 SQL 必须自行控制窗口时，只能使用已确认的公共 SQL 窗口方案，并先回到设计确认。

排序必须稳定唯一（例如报告时间倒序加主键倒序），以保证跨页无重复、无遗漏。

## 6. 前端

适配层负责请求载荷与响应归一：

- 请求：业务筛选平级，分页内嵌 `page: { pageIndex, pageSize }`。
- 响应：`totalCount` 必须来自服务端公共响应；不得补造、截断、字符串化或以零回退，也不得拉取全部数据模拟分页。
- 生成端把 `long` 生成为 `number` 时，适配层校验 `Number.isSafeInteger(totalCount) && totalCount >= 0`，校验失败按契约错误处理。

页面用共享分页控件构造 Table 的受控分页，不各自拼装 antd 字段：

| 项 | 内容 |
|---|---|
| 实现位置 | `client/apps/dy-medical-recognition/src/shared/tablePagination.ts` |
| 导出 | `createTablePagination({ pageIndex, pageSize, totalCount, loading, onChange })`、`PAGE_SIZE_OPTIONS`、`MAX_SELECTABLE_PAGE_SIZE` |
| 页容量可选值 | `[10, 20, 50, 100, 200]`；上限与服务端声明的 200 一致，只在共享控件内声明一处 |
| 呈现 | 分页控件位于表格下方、页容量可选、快速跳页、显示「共 N 条」、加载中禁用 |
| 收敛规则 | 改变页容量回到第 1 页；翻页保持当前页容量 |
| 用例 | `src/shared/tablePagination.test.ts` |

页面侧另需满足：

- 筛选条件变化按设计复位分页；分页、排序、选择和详情有明确保留或清空策略。
- 当页为空且总数为正时，按服务端总数回退到最后一页一次，避免越界页长期空转。
- 同一筛选条件下只采纳最后一次请求结果，过期响应丢弃。

## 7. 验证要求

| 面 | 要求 |
|---|---|
| 契约 | 请求与响应形状、字段名、字段类型与可空性；禁用字段不出现；内嵌分页对象的取值域声明生效 |
| 窗口校验 | 页码小于 1、页容量小于 1 或超过 200 按业务拒绝，且不发生仓储访问；合法窗口正常放行 |
| 分页行为 | 第一页、中间页、末页、越界页与空页；总数与筛选条件一致；跨页无重复、无遗漏；不得只验证返回数量 |
| 排序 | 分页窗口排序与最终排序一致 |
| 前端 | 页容量取值域与服务端一致；总数直传；加载中禁用；改变页容量回到第 1 页；越界自愈一次 |
| 方言中立 | 公共类型、DataRequest 与共享 XML 不得引入具体 Provider 类型或分页方言 |

## 8. 相关条款

本文给出完整设计与实现索引；以下条款各自约束一个视角，仍然有效。

| 视角 | 位置 |
|---|---|
| 前端 API 边界的请求与响应形状、总数来源、安全整数校验、错误边界 | [Frontend API Client](frontend-api-client.md) 第 3 节 |
| SQL 方言中立与窗口归属 | [Backend Architecture](backend-architecture.md) |
| 查询必须定义范围、空值语义、稳定排序与分页策略 | [Backend Command Query Event](backend-command-query-event.md) |
| 前端筛选复位、保留清空策略与不本地分页 | [Frontend Application](frontend-application.md) 第 4、5 节 |
| 复用检索与新增共享抽象的闸门 | [C# Documentation And Reuse](csharp-documentation-and-reuse.md) |
| 分页测试的层级与边界要求 | [C# Backend Testing](csharp-backend-testing.md)、[Frontend Testing](frontend-testing.md)、[Testing](testing.md) |

## 9. 参照实现

本项目当前的分页实现以阶段 4 报告列表为载体，可作为新列表的参照：

- 请求类型与分页对象：`Application.Contracts/Queries/MedicalReportListQueryRequest.cs`（组合 `PageRequestDto`）与 `ReportListQueryRequest.cs`、`BranchReportListQueryRequest.cs`。
- 窗口校验与取页流程：`Application/Queries/MedicalRecognitionReportQueryAppService.Reports.cs`（`PageQueryWindow.Create` → 计数 → 取页 → 映射）。
- 仓储与 SQL 窗口：`Repository/Queries/MedicalRecognitionReportQueryRepository.Reports.cs` 与 `MedicalRecognitionReportQuery.xml`。
- 前端适配层：`client/apps/dy-medical-recognition/src/pages/reportManagement/reportsApi.ts`。
- 前端页面：`client/apps/dy-medical-recognition/src/pages/reportManagement/ReportManagementBoard.tsx`。
- 用例：`Stage4QueryTests.cs`、`Stage4ContractTests.cs`、`Stage4SqlMapTests.cs`、`reportsApi.test.ts`、`ReportManagement.test.tsx`。
