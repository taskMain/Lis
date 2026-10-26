# 标准轴
硬性违规
1 PlatformParameterController:22/PlatformParameterMaintenanceAppService:44 具体类/NonAction；backend-architecture§1。改接口/移Host。
2 OrganizationScopeSelector:108 直调client.api；frontend-application§1。经api.ts。
3 MedicalReportListFilter:20/MedicalReportScopeItem:14 无消费字段；票06。删/接线。
4 platformParametersApi:122 固定Error；frontend-api-client§5。交宿主。
疑似
- Mysterious Name：Code承载实例Id。
- Duplicated Code：readTrustedCode/readId/readName。
- Speculative Generality：OrganizationPathTarget文本构造器、Filter Code兼容属性。
- Data Clumps：OrganizationPath三组Id+名称。