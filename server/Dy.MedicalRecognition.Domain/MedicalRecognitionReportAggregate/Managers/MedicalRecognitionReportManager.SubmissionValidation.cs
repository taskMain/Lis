using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Managers;

/// <summary>
/// 报告提交的请求校验与规范形式处理：必填、成对、条件应填、值域、业务时间顺序与明细唯一性。
/// </summary>
/// <remarks>
/// 全部校验在第一次仓储访问之前完成，因此校验失败不会留下任何业务数据；
/// 校验判定与对外契约声明的填报要求一致，但不暴露字段路径、类型或序列化细节。
/// 规范形式：证件类型代码、证件号码与患者姓名去首尾空白，证件号码另统一为大写；
/// 报告版本表按来源原值保存，规范形式只用于平台患者定位与报告主体检索列。
/// </remarks>
public partial class MedicalRecognitionReportManager
{
  /// <summary>
  /// 规范形式：去除业务文本的首尾空白；输入为 <see langword="null"/> 时返回空串。
  /// </summary>
  /// <param name="value">待处理的文本。</param>
  /// <returns>去除首尾空白后的文本。</returns>
  private static string NormalizeCode(string? value) => value?.Trim() ?? string.Empty;

  /// <summary>
  /// 规范形式：证件号码去除首尾空白并统一为大写。
  /// </summary>
  /// <param name="value">待处理的证件号码。</param>
  /// <returns>去空白并大写后的证件号码。</returns>
  private static string NormalizeDocumentNo(string? value) => NormalizeCode(value).ToUpperInvariant();

  /// <summary>
  /// 规范形式：患者姓名去除首尾空白。
  /// </summary>
  /// <param name="value">待处理的姓名。</param>
  /// <returns>去除首尾空白后的姓名。</returns>
  private static string NormalizeName(string? value) => NormalizeCode(value);

  /// <summary>
  /// 校验报告单号与公共版本信息：必填、成对、就诊类型值域与业务时间顺序。
  /// </summary>
  /// <param name="reportNo">来源报告单号。</param>
  /// <param name="version">公共版本信息。</param>
  /// <param name="receivedTime">本次请求的接收时间。</param>
  /// <exception cref="InvalidOperationException">任一项不成立时抛出。</exception>
  private static void ValidateReportVersion(string reportNo, MedicalReportVersionRequest version, DateTime receivedTime)
  {
    RequireText(reportNo, "报告单号");
    if (version is null) throw new InvalidOperationException("业务拒绝：公共版本信息不能为空。");

    RequireText(version.SourceReportName, "来源报告名称");
    RequireText(version.PatientName, "患者姓名");
    RequireText(version.PatientGenderCode, "患者性别代码");
    RequireTime(version.PatientBirthDate, "患者出生日期");
    RequireText(version.IdentityDocumentTypeCode, "证件类型代码");
    RequireText(version.IdentityDocumentNo, "证件号码");
    RequireText(version.VisitSerialNo, "就诊流水号");

    // 就诊类型仅接受五个已定义取值，不接受未知值。
    if (!AcceptedVisitTypes.Contains(version.VisitType)) throw new InvalidOperationException("业务拒绝：就诊类型不是已定义的取值。");

    RequirePair(version.ApplicationDeptId, version.ApplicationDeptName, "申请科室");
    RequirePair(version.ApplicationDoctorId, version.ApplicationDoctorName, "申请医生");
    RequirePair(version.ExecutionDeptId, version.ExecutionDeptName, "执行科室");
    RequirePair(version.ReportDeptId, version.ReportDeptName, "报告科室");
    RequirePair(version.ReportDoctorId, version.ReportDoctorName, "报告医生");
    RequirePair(version.ReviewDoctorId, version.ReviewDoctorName, "审核医生");

    RequireTime(version.ApplicationTime, "申请时间");
    RequireTime(version.ReportTime, "报告时间");
    RequireTime(version.SourceModifiedTime, "源端报告修改时间");
    if (version.ReviewTime is DateTime reviewTime) RequireTime(reviewTime, "审核时间");

    // 业务时间顺序：申请时间不得晚于报告时间，报告时间不得晚于审核时间；审核时间为空时不参与比较。
    if (version.ApplicationTime > version.ReportTime) throw new InvalidOperationException("业务拒绝：申请时间不得晚于报告时间。");
    if (version.ReviewTime is DateTime reviewedAt && version.ReportTime > reviewedAt) throw new InvalidOperationException("业务拒绝：报告时间不得晚于审核时间。");

    // 业务时间不得晚于平台接收时点：接收时间由服务端取，来源时间超前的请求按逆序拒绝。
    if (version.ReportTime > receivedTime) throw new InvalidOperationException("业务拒绝：报告时间不得晚于平台接收时间。");
  }

  /// <summary>
  /// 校验检验专项内容：必填字段与检验人成对规则。
  /// </summary>
  /// <param name="content">检验专项内容。</param>
  /// <exception cref="InvalidOperationException">必填缺失或成对字段单边提供时抛出。</exception>
  private static void ValidateLaboratoryContent(LaboratoryReportContentRequest content)
  {
    RequireText(content.SourceSpecimenNo, "院内标本号");
    RequireText(content.SpecimenTypeCode, "标本类型编码");
    RequireText(content.SpecimenTypeName, "标本类型名称");
    RequireTime(content.TestingCompletedTime, "检测完成时间");
    RequirePair(content.InspectorId, content.InspectorName, "检验人");
  }

  /// <summary>
  /// 校验一条普通检验结果：必填字段、结果类型值域、异常标志值域与检测人成对规则。
  /// </summary>
  /// <param name="result">普通检验结果。</param>
  /// <exception cref="InvalidOperationException">任一项不成立时抛出。</exception>
  private static void ValidateLaboratoryResultItem(LaboratoryResultItemRequest result)
  {
    RequireText(result.SourceProjectName, "来源项目名称");
    RequireText(result.SourceResultText, "来源结果原文");
    if (result.DisplayOrder < 1) throw new InvalidOperationException("业务拒绝：展示序号必须是正整数。");

    // 结果类型仅接受数值型、定性型、文本型三种取值。
    if (result.ResultType is not (LaboratoryResultType.Numeric or LaboratoryResultType.Qualitative or LaboratoryResultType.Textual))
      throw new InvalidOperationException("业务拒绝：结果类型不是已定义的取值。");

    // 异常标志提供时仅接受受控四值。
    if (result.AbnormalFlag is LaboratoryAbnormalFlag flag
      && flag is not (LaboratoryAbnormalFlag.Normal or LaboratoryAbnormalFlag.High or LaboratoryAbnormalFlag.Low or LaboratoryAbnormalFlag.OtherAbnormal))
    {
      throw new InvalidOperationException("业务拒绝：异常标志不是已定义的取值。");
    }

    RequireOptionalPair(result.InspectorId, result.InspectorName, "检测人");
  }

  /// <summary>
  /// 校验一条细菌鉴定结果：必填字段、检出菌种时的名称要求与检测人成对规则。
  /// </summary>
  /// <param name="bacteria">细菌鉴定结果。</param>
  /// <exception cref="InvalidOperationException">任一项不成立时抛出。</exception>
  private static void ValidateLaboratoryBacteriaResult(LaboratoryBacteriaResultRequest bacteria)
  {
    RequireText(bacteria.SourceResultText, "来源结果原文");
    RequireText(bacteria.DetectionConclusion, "检测结论");
    RequireOptionalPair(bacteria.InspectorId, bacteria.InspectorName, "检测人");

    // 培养未检出不要求菌种编码与名称；检出具体菌种时来源菌种名称必填。
    if (!string.Equals(bacteria.DetectionConclusion.Trim(), NoOrganismDetectedConclusion, StringComparison.Ordinal))
      RequireText(bacteria.SourceOrganismName, "来源菌种名称");

    foreach (LaboratoryAntimicrobialSusceptibilityRequest susceptibility in bacteria.Susceptibilities)
    {
      RequireText(susceptibility.DrugName, "受试药物名称");
      RequireText(susceptibility.SourceConclusionText, "来源结论");
      if (susceptibility.DisplayOrder < 1) throw new InvalidOperationException("业务拒绝：展示序号必须是正整数。");
      RequireOptionalPair(susceptibility.InspectorId, susceptibility.InspectorName, "检测人");
    }
  }

  /// <summary>
  /// 判定检验明细唯一性三条：来源明细标识、普通结果展示序号、药敏展示序号。
  /// </summary>
  /// <remarks>
  /// 来源明细标识提供时在同一报告版本内不重复、全部缺失时仍接收；
  /// 药敏展示序号在同一报告版本、同一细菌鉴定结果下不重复，不同细菌下相同序号可接受。
  /// </remarks>
  /// <param name="results">普通检验结果集合。</param>
  /// <param name="bacteriaResults">细菌鉴定结果集合。</param>
  /// <exception cref="InvalidOperationException">任一条重复时抛出整份报告失败。</exception>
  private static void ValidateLaboratoryDetailUniqueness(
    IReadOnlyList<LaboratoryResultItemRequest> results, IReadOnlyList<LaboratoryBacteriaResultRequest> bacteriaResults)
  {
    RequireDistinct(results.Select(result => result.SourceDetailKey), "普通检验结果来源明细标识");
    RequireDistinct(results.Select(result => result.DisplayOrder.ToString()), "普通检验结果展示序号");

    foreach (LaboratoryBacteriaResultRequest bacteria in bacteriaResults)
    {
      RequireDistinct(bacteria.Susceptibilities.Select(item => item.SourceDetailKey), "细菌鉴定结果来源明细标识");
      RequireDistinct(bacteria.Susceptibilities.Select(item => item.DisplayOrder.ToString()), "药敏结果展示序号");
    }
  }

  /// <summary>
  /// 判定一组可空标识在提供时是否两两不同。
  /// </summary>
  /// <param name="values">待判定的标识集合；空值与纯空白视为未提供。</param>
  /// <param name="contentName">重复时用于拒绝文案的内容名称。</param>
  /// <exception cref="InvalidOperationException">同一非空标识出现两次及以上时抛出。</exception>
  private static void RequireDistinct(IEnumerable<string?> values, string contentName)
  {
    HashSet<string> provided = [];
    foreach (string? value in values)
    {
      string normalized = NormalizeCode(value);
      if (normalized.Length == 0) continue;
      if (!provided.Add(normalized)) throw new InvalidOperationException($"业务拒绝：同一报告版本内{contentName}重复。");
    }
  }

  /// <summary>
  /// 校验检查专项内容：必填字段、检查医生成对规则与影像状态和调阅地址的一致性。
  /// </summary>
  /// <param name="content">检查专项内容。</param>
  /// <exception cref="InvalidOperationException">任一项不成立时抛出。</exception>
  private static void ValidateExaminationContent(ExaminationReportContentRequest content)
  {
    RequireText(content.Findings, "检查所见");
    RequireText(content.Conclusion, "检查结论");
    RequireText(content.SourceDiagnosisName, "来源诊断名称");
    RequireTime(content.ExaminationTime, "实际检查时间");
    RequirePair(content.ExaminerId, content.ExaminerName, "检查医生");

    // 来源影像状态仅接受三值；字段未提供时按未知处理，不参与拒绝判断。
    if (content.SourceImageStatus is SourceImageStatus status
      && status is not (SourceImageStatus.Available or SourceImageStatus.None or SourceImageStatus.Unknown))
    {
      throw new InvalidOperationException("业务拒绝：来源影像状态不是已定义的取值。");
    }

    // 无影像时调阅地址必须为空；有影像或未知时地址可有可无。
    if (content.SourceImageStatus == SourceImageStatus.None && !string.IsNullOrWhiteSpace(content.ImageAccessUrl))
      throw new InvalidOperationException("业务拒绝：来源影像状态为无影像时影像调阅地址必须为空。");
  }

  /// <summary>
  /// 校验一条检查项目：来源项目名称必填、部位名称必填。
  /// </summary>
  /// <param name="item">检查项目。</param>
  /// <exception cref="InvalidOperationException">任一项不成立时抛出。</exception>
  private static void ValidateExaminationItem(ExaminationItemRequest item)
  {
    RequireText(item.SourceProjectName, "来源项目名称");
    foreach (ExaminationSiteRequest site in item.Sites) RequireText(site.SiteName, "部位名称");
  }

  /// <summary>
  /// 要求文本非空且非纯空白。
  /// </summary>
  /// <param name="value">待校验文本。</param>
  /// <param name="contentName">字段的中文名称。</param>
  /// <exception cref="InvalidOperationException">缺失或纯空白时抛出。</exception>
  private static void RequireText(string? value, string contentName)
  {
    if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"业务拒绝：{contentName}不能为空或空白。");
  }

  /// <summary>
  /// 要求时间为非零值；省略与传零值都判为缺失。
  /// </summary>
  /// <param name="value">待校验时间。</param>
  /// <param name="contentName">字段的中文名称。</param>
  /// <exception cref="InvalidOperationException">零值时抛出。</exception>
  private static void RequireTime(DateTime value, string contentName)
  {
    if (value == default) throw new InvalidOperationException($"业务拒绝：{contentName}不能缺失。");
  }

  /// <summary>
  /// 要求必填的标识与名称成对：同时有值，单边提供即拒绝。
  /// </summary>
  /// <param name="id">标识。</param>
  /// <param name="name">名称。</param>
  /// <param name="contentName">成对字段的中文名称。</param>
  /// <exception cref="InvalidOperationException">单边提供，或成对字段两个都为空时抛出。</exception>
  private static void RequirePair(string? id, string? name, string contentName)
  {
    bool hasId = !string.IsNullOrWhiteSpace(id);
    bool hasName = !string.IsNullOrWhiteSpace(name);
    if (hasId != hasName) throw new InvalidOperationException($"业务拒绝：{contentName}的标识与名称必须成对提供。");
    if (!hasId) throw new InvalidOperationException($"业务拒绝：{contentName}不能为空。");
  }

  /// <summary>
  /// 要求可选的标识与名称成对：同时有值或同时为空，单边提供即拒绝，两侧都为空时接收。
  /// </summary>
  /// <remarks>明细上的可选人员字段按本规则判定：来源未提供人员时整组留空，报告照常接收。</remarks>
  /// <param name="id">标识。</param>
  /// <param name="name">名称。</param>
  /// <param name="contentName">成对字段的中文名称。</param>
  /// <exception cref="InvalidOperationException">单边提供时抛出。</exception>
  private static void RequireOptionalPair(string? id, string? name, string contentName)
  {
    bool hasId = !string.IsNullOrWhiteSpace(id);
    bool hasName = !string.IsNullOrWhiteSpace(name);
    if (hasId != hasName) throw new InvalidOperationException($"业务拒绝：{contentName}的标识与名称必须成对提供。");
  }
}
