using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.Reports;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.Queries;

namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 报告管理端只读查询：两个列表入口共用的服务端分页、版本列表与版本详情。
/// </summary>
/// <remarks>
/// 全部查询只筛选与投影，不修改数据，也不判断业务状态。
/// 分页窗口在第一次仓储访问之前完成校验；名称回填按当页涉及的组织与医院批量读取，调用次数不随返回行数增长；
/// 患者姓名、证件号码与联系电话都按来源原值返回。
/// 范围校验只确认可信业务归属与记录归属一致性，不构成操作权限判断。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 查询报告列表（平台管理员入口）。
  /// </summary>
  /// <param name="request">筛选条件与分页参数；组织、医院、院区按请求使用。</param>
  /// <returns>当页报告与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">分页窗口越界、组织或医院或院区不可用，或外部组织服务不可用时抛出。</exception>
  public async Task<PageResultDto<MedicalReportListReadModel>> QueryMedicalReportListAsync(ReportListQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);

    // 平台管理员入口按请求使用组织、医院与院区，并校验三者存在、启用与父子归属；不要求请求组织等于可信组织。
    OrganizationPath? requestedPath = null;
    if (HasAnyScope(request.OrganizationCode, request.HospitalCode, request.BranchCode))
    {
      requestedPath = (await organizationPathResolver.ResolveOrThrow(
        [new OrganizationPathTarget(request.OrganizationCode, request.HospitalCode, request.BranchCode)],
        "业务拒绝：组织不存在或已停用。",
        "业务拒绝：医院不存在、已停用或不属于所选组织。",
        "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];
    }

    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);
    MedicalReportListFilter filter = new()
    {
      OrganizationCode = requestedPath?.OrganizationCode,
      HospitalCode = requestedPath?.HospitalCode,
      BranchCode = requestedPath?.BranchCode,
      ReportTimeFrom = ToStartOfDay(request.ReportDateFrom),
      ReportTimeTo = ToStartOfNextDay(request.ReportDateTo),
      ReportType = request.ReportType,
      ReportNo = request.ReportNo?.Trim(),
      IdentityDocumentNoPrefix = NormalizeDocumentNoPrefix(request.IdentityDocumentNo),
      PatientNamePrefix = NormalizeNamePrefix(request.PatientName)
    };

    PageSlice<MedicalReportListItem> slice = await ReadReportPageAsync(filter, window);
    return await MapReportPageAsync(slice, window);
  }

  /// <summary>
  /// 查询本可信范围内的报告列表（医院管理员入口）。
  /// </summary>
  /// <param name="request">院区与筛选条件、分页参数；请求不提交组织与医院。</param>
  /// <returns>当页报告与分页信息。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">可信组织或医院不可解析、分页窗口越界，或请求院区不属于可信医院时抛出。</exception>
  public async Task<PageResultDto<MedicalReportListReadModel>> QueryBranchMedicalReportListAsync(BranchReportListQueryRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);

    // 组织与医院取自可信上下文并覆盖同名请求字段；院区取请求且必须属于可信医院。
    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, request.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];

    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);
    MedicalReportListFilter filter = new()
    {
      OrganizationCode = path.OrganizationCode,
      HospitalCode = path.HospitalCode,
      BranchCode = path.BranchCode,
      ReportTimeFrom = ToStartOfDay(request.ReportDateFrom),
      ReportTimeTo = ToStartOfNextDay(request.ReportDateTo),
      ReportType = request.ReportType,
      ReportNo = request.ReportNo?.Trim(),
      IdentityDocumentNoPrefix = NormalizeDocumentNoPrefix(request.IdentityDocumentNo),
      PatientNamePrefix = NormalizeNamePrefix(request.PatientName)
    };

    PageSlice<MedicalReportListItem> slice = await ReadReportPageAsync(filter, window);
    return await MapReportPageAsync(slice, window);
  }

  /// <summary>
  /// 查询某个报告的全部历史版本。
  /// </summary>
  /// <param name="request">报告标识。</param>
  /// <returns>版本列表读模型集合；该报告无版本时返回空集合。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">报告标识为空 Guid 时抛出。</exception>
  /// <exception cref="InvalidOperationException">报告不存在或报告不在可信范围内时抛出。</exception>
  public async Task<IReadOnlyList<MedicalReportVersionListReadModel>> QueryMedicalReportVersionListAsync(MedicalReportVersionListQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    await EnsureReportInTrustedScopeAsync(request.ReportId);

    List<MedicalReportVersionListReadModel> versions = [];
    foreach (MedicalReportVersionItem item in await repository.QueryMedicalReportVersionListAsync(request.ReportId))
    {
      // 专项内容按报告类型读取：检验报告只有检验内容、检查报告只有检查内容，因此每次列表读取只多一次或零次内容读取。
      string? reportRemark = item.ReportType == MedicalReportType.Laboratory
        ? (await repository.GetLaboratoryReportContentAsync(item.ReportVersionId))?.ReportRemark
        : (await repository.GetExaminationReportContentAsync(item.ReportVersionId))?.ReportRemark;

      versions.Add(new MedicalReportVersionListReadModel
      {
        ReportVersionId = item.ReportVersionId,
        VersionSequence = item.VersionSequence,
        SourceModifiedTime = item.SourceModifiedTime == default ? null : item.SourceModifiedTime,
        PlatformReceivedTime = item.PlatformReceivedTime,
        ReportDoctorName = NullIfBlank(item.ReportDoctorName),
        ReviewDoctorName = NullIfBlank(item.ReviewDoctorName),
        InspectorName = item.ReportType == MedicalReportType.Laboratory ? await ReadLaboratoryInspectorNameAsync(item.ReportVersionId) : null,
        DetailInspectors = item.ReportType == MedicalReportType.Laboratory ? await ReadLaboratoryDetailInspectorsAsync(item.ReportVersionId) : null,
        ExaminerName = item.ReportType == MedicalReportType.Examination ? await ReadExaminationDoctorNameAsync(item.ReportVersionId) : null,
        SourceReportRemark = NullIfBlank(reportRemark),
        PdfFileName = item.PdfFileName,
        IsCurrentVersion = item.IsCurrentVersion,
        IsSuperseded = item.IsSuperseded,
        ReportStatus = item.ReportStatus
      });
    }

    return versions;
  }

  /// <summary>
  /// 查询某个报告版本的完整内容。
  /// </summary>
  /// <param name="request">报告标识与报告版本标识。</param>
  /// <returns>版本详情读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">报告标识或报告版本标识为空 Guid 时抛出。</exception>
  /// <exception cref="InvalidOperationException">版本不存在或不属于该报告，或报告不在可信范围内时抛出。</exception>
  public async Task<MedicalReportVersionDetailQueryReadModel> QueryMedicalReportVersionDetailAsync(MedicalReportVersionDetailQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    await EnsureReportInTrustedScopeAsync(request.ReportId);

    MedicalReportVersionDetailItem detail = await repository.GetMedicalReportVersionDetailAsync(request.ReportId, request.ReportVersionId)
      ?? throw new InvalidOperationException("业务拒绝：报告版本不存在或不属于该报告。");

    MedicalReportVersionContentReadModel content = detail.ReportType == MedicalReportType.Laboratory
      ? new MedicalReportVersionContentReadModel
      {
        Common = await ReadCommonAsync(detail.ReportVersionId),
        LaboratoryContent = await ReadLaboratoryContentAsync(detail.ReportVersionId)
      }
      : new MedicalReportVersionContentReadModel
      {
        Common = await ReadCommonAsync(detail.ReportVersionId),
        ExaminationContent = await ReadExaminationContentAsync(detail.ReportVersionId)
      };

    return new MedicalReportVersionDetailQueryReadModel
    {
      ReportType = detail.ReportType,
      ReportNo = detail.ReportNo,
      ReportVersionId = detail.ReportVersionId,
      VersionSequence = detail.VersionSequence,
      SourceModifiedTime = detail.SourceModifiedTime == default ? null : detail.SourceModifiedTime,
      PlatformReceivedTime = detail.PlatformReceivedTime,
      ReportDoctorName = NullIfBlank(detail.ReportDoctorName),
      ReviewDoctorName = NullIfBlank(detail.ReviewDoctorName),
      InspectorName = detail.ReportType == MedicalReportType.Laboratory ? await ReadLaboratoryInspectorNameAsync(detail.ReportVersionId) : null,
      DetailInspectors = detail.ReportType == MedicalReportType.Laboratory ? await ReadLaboratoryDetailInspectorsAsync(detail.ReportVersionId) : null,
      ExaminerName = detail.ReportType == MedicalReportType.Examination ? await ReadExaminationDoctorNameAsync(detail.ReportVersionId) : null,
      SourceReportRemark = NullIfBlank(detail.SourceReportRemark),
      Content = content,
      File = new MedicalReportVersionFileReadModel { FileName = detail.PdfFileName }
    };
  }

  /// <summary>
  /// 校验报告在本入口的可信范围内。
  /// </summary>
  /// <remarks>
  /// 医院管理员入口按可信组织与可信医院限定，院区取令牌声明并校验归属；
  /// 报告不在该范围内的定位一律不可得，不泄漏其他医院或院区的数据。
  /// 范围校验按报告来源归属独立读取一次，不依赖版本或文件信息。
  /// </remarks>
  /// <param name="reportId">报告标识。</param>
  /// <returns>报告在可信范围内时完成的异步操作。</returns>
  /// <exception cref="InvalidOperationException">可信归属不可解析，或报告不存在、越出可信范围时抛出。</exception>
  private async Task EnsureReportInTrustedScopeAsync(Guid reportId)
  {
    (OrganizationPath path, DateTime _) = await ResolveTrustedReportScopeAsync();
    MedicalReportScopeItem scope = await repository.GetMedicalReportScopeAsync(reportId)
      ?? throw new InvalidOperationException("业务拒绝：报告不存在。");

    bool inTrustedScope = string.Equals(scope.OrganizationCode, path.OrganizationCode, StringComparison.Ordinal)
      && string.Equals(scope.HospitalCode, path.HospitalCode, StringComparison.Ordinal)
      && string.Equals(scope.BranchCode, path.BranchCode, StringComparison.Ordinal);
    if (!inTrustedScope) throw new InvalidOperationException("业务拒绝：报告不在当前可信组织、医院与院区范围内。");
  }

  /// <summary>
  /// 解析报告管理端的可信三层归属。
  /// </summary>
  /// <remarks>
  /// 组织、医院与院区三层都来自可信上下文：登录令牌该层非空白即取令牌值，令牌缺失该层时用当前登录用户档案的归属补齐，
  /// 令牌与档案冲突或补齐后仍为空即拒绝；院区不从请求取值，因此调用方不能扩大可信范围。
  /// </remarks>
  /// <returns>三层可信归属与本次请求的接收时间。</returns>
  /// <exception cref="InvalidOperationException">可信组织、医院或院区不可解析时抛出。</exception>
  private async Task<(OrganizationPath Path, DateTime ReceivedTime)> ResolveTrustedReportScopeAsync()
  {
    TrustedScope trustedScope = await trustedScopeResolver.ResolveWithBranchOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。", "无法确定当前可信院区。");

    OrganizationPath path = (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, trustedScope.BranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];
    return (path, DateTime.Now);
  }

  /// <summary>
  /// 先取总数、再取当页，得到当页切片。
  /// </summary>
  /// <param name="filter">已规范化的筛选条件。</param>
  /// <param name="window">已校验的分页窗口。</param>
  /// <returns>当页数据与满足同一套筛选条件的总数。</returns>
  private async Task<PageSlice<MedicalReportListItem>> ReadReportPageAsync(MedicalReportListFilter filter, PageQueryWindow window)
  {
    long totalCount = await repository.CountMedicalReportListAsync(filter);
    IReadOnlyList<MedicalReportListItem> items = [.. await repository.QueryMedicalReportListAsync(filter, window.SkipCount, window.PageSize)];
    return new PageSlice<MedicalReportListItem>(items, totalCount);
  }

  /// <summary>
  /// 把当页报告行映射为读模型并对当页整体回填组织、医院、院区名称。
  /// </summary>
  /// <param name="slice">当页数据与总数。</param>
  /// <param name="window">已校验的分页窗口。</param>
  /// <returns>当页报告与分页信息。</returns>
  /// <exception cref="InvalidOperationException">外部组织服务不可用或当页归属不成立时抛出；不降级为编码或空名称。</exception>
  private async Task<PageResultDto<MedicalReportListReadModel>> MapReportPageAsync(PageSlice<MedicalReportListItem> slice, PageQueryWindow window)
  {
    Dictionary<(string OrganizationCode, string HospitalCode, string BranchCode), OrganizationPath> paths =
      await ResolvePagePathsAsync(slice.Items);

    return new PageResultDto<MedicalReportListReadModel>
    {
      Items =
      [
        .. slice.Items.Select(item =>
        {
          OrganizationPath path = paths[(item.OrganizationCode, item.HospitalCode, item.BranchCode)];
          return new MedicalReportListReadModel
          {
            ReportId = item.ReportId,
            OrganizationCode = path.OrganizationCode,
            HospitalCode = path.HospitalCode,
            BranchCode = path.BranchCode,
            OrganizationName = path.OrganizationName,
            HospitalName = path.HospitalName,
            BranchName = path.BranchName,
            ReportType = item.ReportType,
            ReportNo = item.ReportNo,
            ReportTime = item.ReportTime,
            CurrentVersionSequence = item.CurrentVersionSequence,
            PatientName = item.PatientName,
            IdentityDocumentNo = item.IdentityDocumentNo,
            Status = item.Status
          };
        })
      ],
      Page = new PageInfoDto { PageIndex = window.PageIndex, PageSize = window.PageSize, TotalCount = slice.TotalCount }
    };
  }

  /// <summary>
  /// 按当页涉及的不同组织与医院一次性解析归属与名称。
  /// </summary>
  /// <remarks>
  /// 组织全量读取一次，每个不同组织读取一次医院，每家不同医院读取一次院区，
  /// 因此调用次数只随当页涉及的组织与医院数量增长，不随返回行数增长；当页为空时不读取外部服务。
  /// </remarks>
  /// <param name="items">当页报告行。</param>
  /// <returns>按三层编码索引的已校验路径。</returns>
  /// <exception cref="InvalidOperationException">任一行的归属不成立或外部组织服务不可用时抛出。</exception>
  private async Task<Dictionary<(string OrganizationCode, string HospitalCode, string BranchCode), OrganizationPath>> ResolvePagePathsAsync(
    IReadOnlyList<MedicalReportListItem> items)
  {
    Dictionary<(string, string, string), OrganizationPath> paths = [];
    if (items.Count == 0) return paths;

    IReadOnlyList<OrganizationPathTarget> targets =
    [
      .. items
        .Select(item => new OrganizationPathTarget(item.OrganizationCode, item.HospitalCode, item.BranchCode))
        .Distinct()
    ];
    IReadOnlyList<OrganizationPath> resolved = await organizationPathResolver.ResolveOrThrow(
      targets,
      "业务拒绝：报告来源组织不存在或已停用。",
      "业务拒绝：报告来源医院不存在、已停用或不属于该组织。",
      "业务拒绝：报告来源院区不存在、已停用或不属于该医院。");

    for (int index = 0; index < targets.Count; index++)
    {
      OrganizationPathTarget target = targets[index];
      paths[(target.OrganizationCode?.Trim() ?? string.Empty, target.HospitalCode?.Trim() ?? string.Empty, target.BranchCode?.Trim() ?? string.Empty)] = resolved[index];
    }

    return paths;
  }

  /// <summary>
  /// 读取版本公共信息并做展示口径处理。
  /// </summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>公共信息读模型。</returns>
  /// <exception cref="InvalidOperationException">版本不存在时抛出。</exception>
  private async Task<ReportVersionCommonReadModel> ReadCommonAsync(Guid reportVersionId)
  {
    MedicalRecognitionReportDetailCommon? common = await repository.GetMedicalReportVersionCommonAsync(reportVersionId)
      ?? throw new InvalidOperationException("业务拒绝：报告版本不存在。");

    return new ReportVersionCommonReadModel
    {
      PatientName = common.PatientName,
      PatientGenderCode = common.PatientGenderCode,
      PatientBirthDate = common.PatientBirthDate,
      // 患者联系电话按来源原值返回：只把「来源未提供」与「提供空串」归一为空值。
      PatientPhoneNumber = NullIfBlank(common.PatientPhoneNumber),
      AgeAtReport = common.AgeAtReport,
      IdentityDocumentTypeCode = common.IdentityDocumentTypeCode,
      IdentityDocumentNo = common.IdentityDocumentNo,
      VisitType = common.VisitType,
      VisitSerialNo = common.VisitSerialNo,
      SourceReportName = common.SourceReportName,
      ApplicationDeptId = common.ApplicationDeptId,
      ApplicationDeptName = common.ApplicationDeptName,
      ApplicationDoctorId = common.ApplicationDoctorId,
      ApplicationDoctorName = common.ApplicationDoctorName,
      ExecutionDeptId = common.ExecutionDeptId,
      ExecutionDeptName = common.ExecutionDeptName,
      ReportDeptId = common.ReportDeptId,
      ReportDeptName = common.ReportDeptName,
      ReportDoctorId = common.ReportDoctorId,
      ReportDoctorName = common.ReportDoctorName,
      ReviewDoctorId = common.ReviewDoctorId,
      ReviewDoctorName = common.ReviewDoctorName,
      ReviewTime = common.ReviewTime,
      InpatientNo = common.InpatientNo,
      WardName = common.WardName,
      RoomName = common.RoomName,
      BedNo = common.BedNo,
      ApplicationTime = common.ApplicationTime,
      ReportTime = common.ReportTime,
      SourceConfidentialFlag = common.SourceConfidentialFlag
    };
  }

  /// <summary>
  /// 读取检验内容并组装为读模型。
  /// </summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>检验内容读模型；该版本没有检验专项内容时为 <see langword="null"/>。</returns>
  private async Task<LaboratoryReportContentViewReadModel?> ReadLaboratoryContentAsync(Guid reportVersionId)
  {
    LaboratoryReportContentItem? content = await repository.GetLaboratoryReportContentAsync(reportVersionId);
    if (content is null) return null;

    List<LaboratoryBacteriaResultReadModel> bacteriaResults = [];
    foreach (LaboratoryBacteriaResultItem bacteria in await repository.QueryLaboratoryBacteriaResultsAsync(reportVersionId))
    {
      bacteriaResults.Add(new LaboratoryBacteriaResultReadModel
      {
        SourceDetailKey = bacteria.SourceDetailKey,
        SourceOrganismCode = bacteria.SourceOrganismCode,
        SourceOrganismName = bacteria.SourceOrganismName,
        SourceResultText = bacteria.SourceResultText,
        DetectionConclusion = bacteria.DetectionConclusion,
        ColonyCount = bacteria.ColonyCount,
        CultureMedium = bacteria.CultureMedium,
        CultureTime = bacteria.CultureTime,
        CultureCondition = bacteria.CultureCondition,
        DiscoveryMethod = bacteria.DiscoveryMethod,
        DetectionMethod = bacteria.DetectionMethod,
        Description = bacteria.Description,
        InstrumentCode = bacteria.InstrumentCode,
        InstrumentName = bacteria.InstrumentName,
        TestPanelCode = bacteria.TestPanelCode,
        TestPanelName = bacteria.TestPanelName,
        InspectorId = bacteria.InspectorId,
        InspectorName = bacteria.InspectorName,
        Susceptibilities =
        [
          .. (await repository.QueryLaboratorySusceptibilitiesAsync(bacteria.BacteriaResultId)).Select(item => new LaboratoryAntimicrobialSusceptibilityReadModel
          {
            SourceDetailKey = item.SourceDetailKey,
            DrugCode = item.DrugCode,
            DrugName = item.DrugName,
            SusceptibilityCode = item.SusceptibilityCode,
            SourceConclusionText = item.SourceConclusionText,
            ResistanceResultCode = item.ResistanceResultCode,
            DiskContent = item.DiskContent,
            MicValue = item.MicValue,
            InhibitionZoneDiameter = item.InhibitionZoneDiameter,
            ReferenceValue = item.ReferenceValue,
            DisplayOrder = item.DisplayOrder,
            InspectorId = item.InspectorId,
            InspectorName = item.InspectorName,
            TestingMethod = item.TestingMethod,
            TestPanelOrder = item.TestPanelOrder
          })
        ]
      });
    }

    return new LaboratoryReportContentViewReadModel
    {
      ReportCategoryCode = content.ReportCategoryCode,
      ReportCategoryName = content.ReportCategoryName,
      ReportRemark = content.ReportRemark,
      OverallAbnormalFlag = content.OverallAbnormalFlag,
      SourceOrderSerialNo = content.SourceOrderSerialNo,
      SpecimenCollectedTime = content.SpecimenCollectedTime,
      SpecimenSubmittedTime = content.SpecimenSubmittedTime,
      LaboratoryReceivedTime = content.LaboratoryReceivedTime,
      SourceSpecimenNo = content.SourceSpecimenNo,
      SpecimenTypeCode = content.SpecimenTypeCode,
      SpecimenTypeName = content.SpecimenTypeName,
      TestingCompletedTime = content.TestingCompletedTime,
      InspectorId = content.InspectorId,
      InspectorName = content.InspectorName,
      Results =
      [
        .. (await repository.QueryLaboratoryResultItemsAsync(reportVersionId)).Select(item => new LaboratoryResultItemReadModel
        {
          SourceDetailKey = item.SourceDetailKey,
          SourceProjectName = item.SourceProjectName,
          SourceProjectCode = item.SourceProjectCode,
          StandardProjectCode = item.StandardProjectCode,
          SourceResultText = item.SourceResultText,
          ResultType = item.ResultType,
          LoincCode = item.LoincCode,
          Unit = item.Unit,
          ReferenceRange = item.ReferenceRange,
          TestingMethod = item.TestingMethod,
          InstrumentCode = item.InstrumentCode,
          InstrumentName = item.InstrumentName,
          DisplayOrder = item.DisplayOrder,
          AbnormalFlag = item.AbnormalFlag,
          CriticalValueFlag = item.CriticalValueFlag,
          LaboratoryChargeItemCode = item.LaboratoryChargeItemCode,
          MedicalInsuranceChargeItemCode = item.MedicalInsuranceChargeItemCode,
          InspectorId = item.InspectorId,
          InspectorName = item.InspectorName
        })
      ],
      BacteriaResults = bacteriaResults
    };
  }

  /// <summary>
  /// 读取该版本的检验人名称。
  /// </summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>检验人名称；该版本没有检验专项内容时为 <see langword="null"/>。</returns>
  private async Task<string?> ReadLaboratoryInspectorNameAsync(Guid reportVersionId) =>
    NullIfBlank((await repository.GetLaboratoryReportContentAsync(reportVersionId))?.InspectorName);

  /// <summary>
  /// 读取该版本普通检验结果的明细检测人。
  /// </summary>
  /// <remarks>按检测人名称去重后以顿号连接；全部缺失时返回 <see langword="null"/>。</remarks>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>明细检测人文本；无检测人时返回 <see langword="null"/>。</returns>
  private async Task<string?> ReadLaboratoryDetailInspectorsAsync(Guid reportVersionId)
  {
    string[] inspectors =
    [
      .. (await repository.QueryLaboratoryResultItemsAsync(reportVersionId))
        .Select(item => item.InspectorName?.Trim() ?? string.Empty)
        .Where(name => name.Length > 0)
        .Distinct(StringComparer.Ordinal)
    ];
    return inspectors.Length == 0 ? null : string.Join('、', inspectors);
  }

  /// <summary>
  /// 读取该版本的检查医生名称。
  /// </summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>检查医生名称；该版本没有检查专项内容时为 <see langword="null"/>。</returns>
  private async Task<string?> ReadExaminationDoctorNameAsync(Guid reportVersionId) =>
    NullIfBlank((await repository.GetExaminationReportContentAsync(reportVersionId))?.ExaminerName);

  /// <summary>
  /// 读取检查内容并组装为读模型。
  /// </summary>
  /// <param name="reportVersionId">报告版本标识。</param>
  /// <returns>检查内容读模型；该版本没有检查专项内容时为 <see langword="null"/>。</returns>
  private async Task<ExaminationReportContentViewReadModel?> ReadExaminationContentAsync(Guid reportVersionId)
  {
    ExaminationReportContentItem? content = await repository.GetExaminationReportContentAsync(reportVersionId);
    if (content is null) return null;

    List<ExaminationItemReadModel> items = [];
    foreach (ExaminationItemView item in await repository.QueryExaminationItemsAsync(reportVersionId))
    {
      items.Add(new ExaminationItemReadModel
      {
        SourceProjectName = item.SourceProjectName,
        SourceProjectCode = item.SourceProjectCode,
        StandardProjectCode = item.StandardProjectCode,
        Sites =
        [
          .. (await repository.QueryExaminationSitesAsync(item.ItemId)).Select(site => new ExaminationSiteReadModel
          {
            SiteName = site.SiteName,
            SourceSiteCode = site.SourceSiteCode
          })
        ]
      });
    }

    return new ExaminationReportContentViewReadModel
    {
      SourceExaminationTypeCode = content.SourceExaminationTypeCode,
      SourceExaminationTypeName = content.SourceExaminationTypeName,
      ReportRemark = content.ReportRemark,
      OverallAbnormalFlag = content.OverallAbnormalFlag,
      Findings = content.Findings,
      Conclusion = content.Conclusion,
      ConditionDescription = content.ConditionDescription,
      ExaminationPurpose = content.ExaminationPurpose,
      SourceDiagnosisCode = content.SourceDiagnosisCode,
      SourceDiagnosisName = content.SourceDiagnosisName,
      ExaminationTime = content.ExaminationTime,
      ExaminerId = content.ExaminerId,
      ExaminerName = content.ExaminerName,
      SourceImageStatus = content.SourceImageStatus,
      ImageAccessUrl = content.ImageAccessUrl,
      ExaminationMethod = content.ExaminationMethod,
      DeviceCode = content.DeviceCode,
      DeviceName = content.DeviceName,
      Items = items
    };
  }

  /// <summary>
  /// 判断平台管理员入口是否提交了任一范围条件。
  /// </summary>
  /// <param name="organizationCode">请求的组织编码。</param>
  /// <param name="hospitalCode">请求的医院编码。</param>
  /// <param name="branchCode">请求的院区编码。</param>
  /// <returns>三个条件中至少一个非空白时为 <see langword="true"/>。</returns>
  private static bool HasAnyScope(string? organizationCode, string? hospitalCode, string? branchCode) =>
    !string.IsNullOrWhiteSpace(organizationCode) || !string.IsNullOrWhiteSpace(hospitalCode) || !string.IsNullOrWhiteSpace(branchCode);

  /// <summary>
  /// 把日期筛选字段转成当日起点时刻，作为时间范围的下界。
  /// </summary>
  /// <remarks>
  /// 结果不带时区标注，数据库驱动按原值写入 <c>timestamp without time zone</c> 列；
  /// 带时区标注的值会被驱动按本机时区换算，使边界偏移。
  /// </remarks>
  /// <param name="date">起始日；为空表示不设下界。</param>
  /// <returns>该日零点；无输入时返回 <see langword="null"/>。</returns>
  private static DateTime? ToStartOfDay(DateOnly? date) =>
    date?.ToDateTime(TimeOnly.MinValue);

  /// <summary>
  /// 把日期筛选字段转成次日零点时刻，作为时间范围的不含上界。
  /// </summary>
  /// <remarks>结果不带时区标注，理由与 <see cref="ToStartOfDay"/> 相同。</remarks>
  /// <param name="date">结束日；为空表示不设上界。</param>
  /// <returns>结束日次日零点；无输入时返回 <see langword="null"/>。</returns>
  private static DateTime? ToStartOfNextDay(DateOnly? date) =>
    date?.AddDays(1).ToDateTime(TimeOnly.MinValue);

  /// <summary>
  /// 按规范形式处理证件号码筛选输入：去首尾空白并统一大写。
  /// </summary>
  /// <param name="value">用户输入。</param>
  /// <returns>规范形式后的前缀；输入缺失或处理后为空时返回 <see langword="null"/>，表示不施加该筛选条件。</returns>
  private static string? NormalizeDocumentNoPrefix(string? value)
  {
    string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
    return normalized.Length == 0 ? null : normalized;
  }

  /// <summary>
  /// 按规范形式处理姓名筛选输入：去首尾空白。
  /// </summary>
  /// <param name="value">用户输入。</param>
  /// <returns>规范形式后的前缀；输入缺失或处理后为空时返回 <see langword="null"/>，表示不施加该筛选条件。</returns>
  private static string? NormalizeNamePrefix(string? value)
  {
    string normalized = value?.Trim() ?? string.Empty;
    return normalized.Length == 0 ? null : normalized;
  }

  /// <summary>
  /// 把空白文本归一为 <see langword="null"/>，使「来源未提供」与「提供空串」在返回契约上是同一事实。
  /// </summary>
  /// <param name="value">待归一文本。</param>
  /// <returns>去除首尾空白后的文本；为空时返回 <see langword="null"/>。</returns>
  private static string? NullIfBlank(string? value)
  {
    string normalized = value?.Trim() ?? string.Empty;
    return normalized.Length == 0 ? null : normalized;
  }
}
