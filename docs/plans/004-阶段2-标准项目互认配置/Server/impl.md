# 阶段 2 后端实施

## 维护边界

本文件记录后续实施批次和验证编号；详细前置数据与预期见 [Server/design.md](design.md) 的验证矩阵。业务代码在设计通过并获得实施授权后修改；建表由负责人执行，实施侧只交付最终DDL，不自行操作数据库。

后续按项目既有代码与生成物维护规则实施；相应SRS/UML契约由设计主责同步。

## 批次与依赖

以下文件范围均相对 `server/`，只修改所属类型/语句的阶段2内容，不重写整份共享文件。主责为后续交接角色，每批仅一个主责。

| 批次 | 主责 | 文件范围与交付 | 矩阵 | 前置条件 | 当前状态 |
|---|---|---|---|---|---|
| B1 取证 | 后端架构主责 | 核对四命令/事件、AppService、Manager、仓储及XML；组织来源/选择传递、scope键清单、无WorkUnit生产入口事件机制 | V11、V16、V21-V24的取证面 | 当前设计及SRS/UML确认；不以替代入口证明生产路径 | NotRun；组织链及事件机制Pending；V24运行Blocked |
| B2 写 | 后端实现主责 | `Dy.MedicalRecognition.Application.Contracts/MedicalRecognitionReportAggregate/Requests/*MutualRecognition*.cs`及既有写接口；Application同聚合AppService/DataMaps；Domain同聚合四Command、MutualRecognitionItem、Manager、仓储接口；Domain.Share同聚合四Event；Repository同聚合仓储实现和`MutualRecognitionItem.xml`，必要的`MedicalStandardItem.xml`编码读取语句 | V1-V9、V11、V15-V17、V21-V22、V26-V27；真实事件另见V24；V10为N/A | B1确认生成保护和实际机制；组织解析与事件接入各自Pending解除；设计确认、symbol影响分析及准确RED；现有目录仓储复用 | NotRun；依赖未确认组织/事件机制的实现Blocked，其余独立契约静态工作可推进 |
| B3 查询 | 后端查询主责 | Contracts/Queries既有接口、新建`RecognitionProjectConfigurationListQueryRequest.cs`及迁入的`RecognitionProjectConfigurationReadModel.cs`；Application/Queries既有AppService；Domain/Queries既有仓储接口及新建`RecognitionProjectConfigurationListItem.cs`；Repository/Queries既有仓储及XML | V12-V14、V19-V23 | B1确认契约、scope；组织授权接入依赖S2-D12解除；准确RED；复用既有查询接口 | NotRun；依赖组织机制的接入Blocked，独立契约可推进 |
| B4 DDL | 数据库设计主责 | `Dy.MedicalRecognition.Repository/Scripts/mutual_recognition_item.sql`及所属阶段迁移交付；核对B2/B3 XML映射，不并行改其主责文件 | V17-V18、V23、V25静态面 | B1文件保护取证；设计明确逐列类型、无DB默认值、列序、中文注释和短唯一索引；无需等待组织服务 | NotRun；只交付可审阅最终脚本，不执行DDL |
| B5 真实验证 | 后端测试主责 | `Dy.MedicalRecognition.Tests/`精确测试、实际Host入口/数据库/事件证据；交阶段根`testReport.md`主责归档，不由本轮修改报告 | V1-V27全部适用验证面，重点V24、V26-V27及DB面 | B2/B3/B4对应实现完成；下节阶段1证据、负责人建表确认、组织真实链路、scope及事件观察点；环境值不得猜测 | Blocked；本轮没有运行结论，V24未核实不得改N/A |

顺序为B1取证后按依赖推进B2写、B3查询、B4DDL，最后B5真实验证；独立文件和不依赖Pending的工作可并行，共享Repository/XML交接串行。B1不因某项Blocked暂停全部取证；B4交付完成后暂停依赖新表的集成测试，收到负责人建表完成确认才恢复。

## 阶段1前置证据

进入 B5 对应真实验证前必须取得：阶段1 `testReport.md` 的真实收口结论；标准目录三表目标数据库/schema 建表确认；阶段1作用域探测的实际查找键/注册键清单；负责人确认阶段1可作为阶段2目录依赖；阶段2目标表最终DDL执行完成确认。缺任一项，只阻断依赖该项的集成验证，不把构建或静态检查写成业务通过。

组织取证引用 S2-D12，补齐服务接口、授权组织集合、选中组织到服务端可信取值全链。事件取证引用 S2-D13，沿实际生产入口核对；V24未核实保持Blocked。独立查询投影、契约检查和DDL设计继续。

## 生成与人工维护

本阶段涉及 Request、ReadModel、Command、Event、Entity、DataMap、Repository、XML、DDL、Application、Manager 和必要 Host 接入。

## 执行记录

- [ ] 完成失败证据、实现、复测和阶段报告归档。
- [ ] 按 V1-V25 和新增 V26-V27 执行精确验证；V11命令与V19查询、V13单层与V20多层分别保留证据。
- [ ] 将实际结果、证据及残余阻断交阶段根 `testReport.md` 主责归档。
