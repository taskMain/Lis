using System.Globalization;
using Dy.Core.Extensions.Models;
using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;

namespace Dy.MedicalRecognition.Application.Queries;

/// <summary>
/// 互认统计与导出的应用服务实现分片：范围解析、窗口校验、语句编排、读模型组装与导出生成编排。
/// </summary>
/// <remarks>
/// 本分片承载统计契约分片声明的十个入口；全部入口都是只读查询与一个文件生成导出，
/// 不声明事务、不登记事件、不产生状态变化。
/// </remarks>
public sealed partial class MedicalRecognitionReportQueryAppService
{
  /// <summary>
  /// 查询接收侧互认使用汇总（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 接收组与来源组的组织、医院、院区按请求使用并经组织路径解析校验存在、启用与父子归属；
  /// 任一范围条件为空不附加该层过滤，全空即按授权全量；两组组织编码同时提供且不一致时整次拒绝。
  /// 分页窗口校验先于第一次仓储访问；同期互认率与原因占比在应用层按行内次数计算，
  /// 汇总行的组织、医院与院区名称按当页分组行涉及的编码批量回填。
  /// </remarks>
  /// <param name="request">日期范围、汇总维度、接收组与来源组筛选及分页参数；范围条件按请求使用。</param>
  /// <returns>当页汇总分组行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">分页窗口越界，或范围条件不存在、已停用、父子归属不匹配、两组组织编码不一致时抛出。</exception>
  public async Task<PageResultDto<RecognitionUsageSummaryReadModel>> QueryRecognitionUsageSummaryAsync(RecognitionUsageSummaryQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);
    EnsureSameOrganization(request.OrganizationCode, request.SourceOrganizationCode);

    OrganizationPath? receiver = await ResolveOptionalScopeAsync(
      request.OrganizationCode, request.HospitalCode, request.BranchCode,
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。");
    OrganizationPath? source = await ResolveOptionalScopeAsync(
      request.SourceOrganizationCode, request.SourceHospitalCode, request.SourceBranchCode,
      "业务拒绝：来源组织不存在或已停用。",
      "业务拒绝：来源医院不存在、已停用或不属于所选组织。",
      "业务拒绝：来源院区不存在、已停用或不属于所选医院。");

    RecognitionUsageSummaryFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      GroupDimension = request.GroupDimension,
      OrganizationCode = ScopeFilterCode(receiver?.OrganizationCode),
      HospitalCode = ScopeFilterCode(receiver?.HospitalCode),
      BranchCode = ScopeFilterCode(receiver?.BranchCode),
      SourceOrganizationCode = ScopeFilterCode(source?.OrganizationCode),
      SourceHospitalCode = ScopeFilterCode(source?.HospitalCode),
      SourceBranchCode = ScopeFilterCode(source?.BranchCode),
      RecognitionDeptId = request.RecognitionDeptId?.Trim(),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadUsageSummaryPageAsync(filter, window);
  }

  /// <summary>
  /// 查询接收侧互认使用明细（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 接收组与来源组的范围条件按请求使用并经组织路径解析校验，任一层为空不附加该层过滤；
  /// 两组组织编码同时提供且不一致时整次拒绝。分页窗口校验先于第一次仓储访问；
  /// 明细类型决定读取的四类事实语句之一，业务时间随明细类型分别取匹配生成时间、互认时间或实际引用时间。
  /// </remarks>
  /// <param name="request">日期范围、明细类型、接收组与来源组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件、不采纳原因代码或分页取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">分页窗口越界，或范围条件不存在、已停用、父子归属不匹配、两组组织编码不一致时抛出。</exception>
  public async Task<PageResultDto<RecognitionUsageDetailReadModel>> QueryRecognitionUsageDetailsAsync(RecognitionUsageDetailsQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);
    EnsureSameOrganization(request.OrganizationCode, request.SourceOrganizationCode);

    OrganizationPath? receiver = await ResolveOptionalScopeAsync(
      request.OrganizationCode, request.HospitalCode, request.BranchCode,
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。");
    OrganizationPath? source = await ResolveOptionalScopeAsync(
      request.SourceOrganizationCode, request.SourceHospitalCode, request.SourceBranchCode,
      "业务拒绝：来源组织不存在或已停用。",
      "业务拒绝：来源医院不存在、已停用或不属于所选组织。",
      "业务拒绝：来源院区不存在、已停用或不属于所选医院。");

    RecognitionUsageDetailFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      OrganizationCode = ScopeFilterCode(receiver?.OrganizationCode),
      HospitalCode = ScopeFilterCode(receiver?.HospitalCode),
      BranchCode = ScopeFilterCode(receiver?.BranchCode),
      SourceOrganizationCode = ScopeFilterCode(source?.OrganizationCode),
      SourceHospitalCode = ScopeFilterCode(source?.HospitalCode),
      SourceBranchCode = ScopeFilterCode(source?.BranchCode),
      RecognitionDeptId = request.RecognitionDeptId?.Trim(),
      RecognitionDoctorId = request.RecognitionDoctorId?.Trim(),
      NonAdoptionReason = ParseNonAdoptionReasonCode(request.NonAdoptionReasonCode),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadUsageDetailPageAsync(filter, request.DetailType, window);
  }

  /// <summary>
  /// 查询来源医院被互认汇总（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 来源组与接收组的组织、医院、院区按请求使用并经组织路径解析校验存在、启用与父子归属；
  /// 任一范围条件为空不附加该层过滤，全空即按授权全量；两组组织编码同时提供且不一致时整次拒绝。
  /// 汇总维度只接受来源医院、来源院区与标准项目，互认科室维度值在进入仓储之前按业务拒绝。
  /// 分页窗口校验先于第一次仓储访问；被互认次数由语句只按采纳事实聚合，
  /// 汇总行的来源方名称按当页分组行涉及的编码批量回填。
  /// </remarks>
  /// <param name="request">日期范围、汇总维度、来源组与接收组筛选及分页参数；范围条件按请求使用。</param>
  /// <returns>当页汇总分组行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出（请求校验先于一切判定）。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">
  /// 分页窗口越界、汇总维度取互认科室值，或范围条件不存在、已停用、父子归属不匹配、两组组织编码不一致时抛出。
  /// </exception>
  public async Task<PageResultDto<SourceRecognitionSummaryReadModel>> QuerySourceRecognitionSummaryAsync(SourceRecognitionSummaryQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);
    EnsureSameOrganization(request.ReceiverOrganizationCode, request.SourceOrganizationCode);
    EnsureSourceGroupDimension(request.GroupDimension);

    OrganizationPath? source = await ResolveOptionalScopeAsync(
      request.SourceOrganizationCode, request.SourceHospitalCode, request.SourceBranchCode,
      "业务拒绝：来源组织不存在或已停用。",
      "业务拒绝：来源医院不存在、已停用或不属于所选组织。",
      "业务拒绝：来源院区不存在、已停用或不属于所选医院。");
    OrganizationPath? receiver = await ResolveOptionalScopeAsync(
      request.ReceiverOrganizationCode, request.ReceiverHospitalCode, request.ReceiverBranchCode,
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。");

    SourceRecognitionSummaryFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      GroupDimension = request.GroupDimension,
      SourceOrganizationCode = ScopeFilterCode(source?.OrganizationCode),
      SourceHospitalCode = ScopeFilterCode(source?.HospitalCode),
      SourceBranchCode = ScopeFilterCode(source?.BranchCode),
      ReceiverOrganizationCode = ScopeFilterCode(receiver?.OrganizationCode),
      ReceiverHospitalCode = ScopeFilterCode(receiver?.HospitalCode),
      ReceiverBranchCode = ScopeFilterCode(receiver?.BranchCode),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadSourceSummaryPageAsync(filter, window);
  }

  /// <summary>
  /// 查询来源医院被互认明细（平台管理员入口）。
  /// </summary>
  /// <remarks>
  /// 来源组与接收组的范围条件按请求使用并经组织路径解析校验，任一层为空不附加该层过滤；
  /// 两组组织编码同时提供且不一致时整次拒绝。分页窗口校验先于第一次仓储访问；
  /// 来源侧明细只反映被采纳事实，业务时间取处理结果的互认时间。
  /// </remarks>
  /// <param name="request">日期范围、来源组与接收组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">分页窗口越界，或范围条件不存在、已停用、父子归属不匹配、两组组织编码不一致时抛出。</exception>
  public async Task<PageResultDto<SourceRecognitionDetailReadModel>> QuerySourceRecognitionDetailsAsync(SourceRecognitionDetailsQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);
    EnsureSameOrganization(request.ReceiverOrganizationCode, request.SourceOrganizationCode);

    OrganizationPath? source = await ResolveOptionalScopeAsync(
      request.SourceOrganizationCode, request.SourceHospitalCode, request.SourceBranchCode,
      "业务拒绝：来源组织不存在或已停用。",
      "业务拒绝：来源医院不存在、已停用或不属于所选组织。",
      "业务拒绝：来源院区不存在、已停用或不属于所选医院。");
    OrganizationPath? receiver = await ResolveOptionalScopeAsync(
      request.ReceiverOrganizationCode, request.ReceiverHospitalCode, request.ReceiverBranchCode,
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。");

    SourceRecognitionDetailFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      SourceOrganizationCode = ScopeFilterCode(source?.OrganizationCode),
      SourceHospitalCode = ScopeFilterCode(source?.HospitalCode),
      SourceBranchCode = ScopeFilterCode(source?.BranchCode),
      ReceiverOrganizationCode = ScopeFilterCode(receiver?.OrganizationCode),
      ReceiverHospitalCode = ScopeFilterCode(receiver?.HospitalCode),
      ReceiverBranchCode = ScopeFilterCode(receiver?.BranchCode),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadSourceDetailPageAsync(filter, window);
  }

  /// <summary>
  /// 查询本可信范围内的接收侧互认使用汇总（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 接收组织与接收医院取自可信上下文，请求不提交；本侧接收院区可选，为空按可信医院全院范围统计，
  /// 解析目标只构造组织与医院两层、不进入院区层；非空时必须存在、启用且属于可信医院。
  /// 来源组织恒为可信组织，来源组医院与院区取请求并校验属于可信组织。
  /// 分页窗口校验先于第一次仓储访问；同期互认率与原因占比在应用层按行内次数计算。
  /// </remarks>
  /// <param name="request">日期范围、汇总维度、本侧院区与来源组筛选及分页参数。</param>
  /// <returns>当页汇总分组行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">可信组织或医院不可解析、分页窗口越界，或本侧院区、来源组条件不存在、已停用、不属于可信范围时抛出。</exception>
  public async Task<PageResultDto<RecognitionUsageSummaryReadModel>> QueryBranchRecognitionUsageSummaryAsync(BranchRecognitionUsageSummaryQueryRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);

    OrganizationPath localScope = await ResolveBranchLocalScopeAsync(trustedScope, request.BranchCode);
    OrganizationPath? sourceScope = await ResolveBranchSourceScopeAsync(trustedScope, request.SourceHospitalCode, request.SourceBranchCode);

    RecognitionUsageSummaryFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      GroupDimension = request.GroupDimension,
      OrganizationCode = localScope.OrganizationCode,
      HospitalCode = localScope.HospitalCode,
      BranchCode = request.BranchCode is null ? null : localScope.BranchCode,
      SourceOrganizationCode = trustedScope.OrganizationCode,
      SourceHospitalCode = ScopeFilterCode(sourceScope?.HospitalCode),
      SourceBranchCode = ScopeFilterCode(sourceScope?.BranchCode),
      RecognitionDeptId = request.RecognitionDeptId?.Trim(),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadUsageSummaryPageAsync(filter, window);
  }

  /// <summary>
  /// 查询本可信范围内的接收侧互认使用明细（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 接收组织与接收医院取自可信上下文，请求不提交；本侧接收院区可选，为空按可信医院全院范围查询明细，
  /// 解析目标只构造组织与医院两层、不进入院区层；非空时必须存在、启用且属于可信医院。
  /// 来源组织恒为可信组织，来源组医院与院区取请求并校验属于可信组织。
  /// 分页窗口校验先于第一次仓储访问；明细类型决定读取的四类事实语句之一。
  /// </remarks>
  /// <param name="request">日期范围、明细类型、本侧院区与来源组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件、不采纳原因代码或分页取值非法时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">可信组织或医院不可解析、分页窗口越界，或本侧院区、来源组条件不存在、已停用、不属于可信范围时抛出。</exception>
  public async Task<PageResultDto<RecognitionUsageDetailReadModel>> QueryBranchRecognitionUsageDetailsAsync(BranchRecognitionUsageDetailsQueryRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);

    OrganizationPath localScope = await ResolveBranchLocalScopeAsync(trustedScope, request.BranchCode);
    OrganizationPath? sourceScope = await ResolveBranchSourceScopeAsync(trustedScope, request.SourceHospitalCode, request.SourceBranchCode);

    RecognitionUsageDetailFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      OrganizationCode = localScope.OrganizationCode,
      HospitalCode = localScope.HospitalCode,
      BranchCode = request.BranchCode is null ? null : localScope.BranchCode,
      SourceOrganizationCode = trustedScope.OrganizationCode,
      SourceHospitalCode = ScopeFilterCode(sourceScope?.HospitalCode),
      SourceBranchCode = ScopeFilterCode(sourceScope?.BranchCode),
      RecognitionDeptId = request.RecognitionDeptId?.Trim(),
      RecognitionDoctorId = request.RecognitionDoctorId?.Trim(),
      NonAdoptionReason = ParseNonAdoptionReasonCode(request.NonAdoptionReasonCode),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadUsageDetailPageAsync(filter, request.DetailType, window);
  }

  /// <summary>
  /// 查询本可信范围的来源医院被互认汇总（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 来源组织与来源医院固定为可信上下文，请求无法指定其他来源医院；本侧来源院区可选，
  /// 为空按可信医院全部来源院区统计，解析目标只构造组织与医院两层、不进入院区层；非空时必须存在、启用且属于可信医院。
  /// 接收组织恒为可信组织，接收组医院与院区取请求并校验属于可信组织。
  /// 汇总维度只接受来源医院、来源院区与标准项目，互认科室维度值在进入仓储之前按业务拒绝。
  /// 分页窗口校验先于第一次仓储访问；被互认次数由语句只按采纳事实聚合。
  /// </remarks>
  /// <param name="request">日期范围、汇总维度、本侧来源院区与接收组筛选及分页参数。</param>
  /// <returns>当页汇总分组行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织或医院不可解析、分页窗口越界、汇总维度取互认科室值，或本侧院区、接收组条件不存在、已停用、不属于可信范围时抛出。
  /// </exception>
  public async Task<PageResultDto<SourceRecognitionSummaryReadModel>> QueryBranchSourceRecognitionSummaryAsync(BranchSourceRecognitionSummaryQueryRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);
    EnsureSourceGroupDimension(request.GroupDimension);

    OrganizationPath localSourceScope = await ResolveBranchLocalScopeAsync(trustedScope, request.SourceBranchCode);
    OrganizationPath? receiverScope = await ResolveBranchReceiverScopeAsync(trustedScope, request.ReceiverHospitalCode, request.ReceiverBranchCode);

    SourceRecognitionSummaryFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      GroupDimension = request.GroupDimension,
      SourceOrganizationCode = localSourceScope.OrganizationCode,
      SourceHospitalCode = localSourceScope.HospitalCode,
      SourceBranchCode = request.SourceBranchCode is null ? null : localSourceScope.BranchCode,
      ReceiverOrganizationCode = trustedScope.OrganizationCode,
      ReceiverHospitalCode = ScopeFilterCode(receiverScope?.HospitalCode),
      ReceiverBranchCode = ScopeFilterCode(receiverScope?.BranchCode),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadSourceSummaryPageAsync(filter, window);
  }

  /// <summary>
  /// 查询本可信范围的来源医院被互认明细（医院管理员入口）。
  /// </summary>
  /// <remarks>
  /// 来源组织与来源医院固定为可信上下文，请求无法指定其他来源医院；本侧来源院区可选，
  /// 为空按可信医院全部来源院区查询，解析目标只构造组织与医院两层、不进入院区层；非空时必须存在、启用且属于可信医院。
  /// 接收组织恒为可信组织，接收组医院与院区取请求并校验属于可信组织。
  /// 分页窗口校验先于第一次仓储访问；来源侧明细只反映被采纳事实，业务时间取处理结果的互认时间。
  /// </remarks>
  /// <param name="request">日期范围、本侧来源院区与接收组筛选及分页参数。</param>
  /// <returns>当页明细行与分页信息；无匹配时返回空集合且分页信息保留。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">筛选条件或分页取值非法时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织或医院不可解析、分页窗口越界，或本侧院区、接收组条件不存在、已停用、不属于可信范围时抛出。
  /// </exception>
  public async Task<PageResultDto<SourceRecognitionDetailReadModel>> QueryBranchSourceRecognitionDetailsAsync(BranchSourceRecognitionDetailsQueryRequest request)
  {
    // 可信组织与医院的解析先于公共请求校验：可信范围不成立时不需要、也不得读取任何业务数据。
    TrustedScope trustedScope = await trustedScopeResolver.ResolveOrThrowAsync(
      HttpRequestInfo, "无法确定当前可信组织。", "无法确定当前可信医院。");
    MedicalRecognitionRequestValidator.Validate(request);
    PageQueryWindow window = PageQueryWindow.Create(request.Page.PageIndex, request.Page.PageSize);

    OrganizationPath localSourceScope = await ResolveBranchLocalScopeAsync(trustedScope, request.SourceBranchCode);
    OrganizationPath? receiverScope = await ResolveBranchReceiverScopeAsync(trustedScope, request.ReceiverHospitalCode, request.ReceiverBranchCode);

    SourceRecognitionDetailFilter filter = new()
    {
      PeriodStart = request.StartTime.ToDateTime(TimeOnly.MinValue),
      PeriodEnd = request.EndTime.AddDays(1).ToDateTime(TimeOnly.MinValue),
      SourceOrganizationCode = localSourceScope.OrganizationCode,
      SourceHospitalCode = localSourceScope.HospitalCode,
      SourceBranchCode = request.SourceBranchCode is null ? null : localSourceScope.BranchCode,
      ReceiverOrganizationCode = trustedScope.OrganizationCode,
      ReceiverHospitalCode = ScopeFilterCode(receiverScope?.HospitalCode),
      ReceiverBranchCode = ScopeFilterCode(receiverScope?.BranchCode),
      ItemType = request.ItemType,
      CategoryName = request.CategoryName?.Trim(),
      GroupName = request.GroupName?.Trim(),
      StandardProjectCode = request.StandardProjectCode?.Trim()
    };

    return await ReadSourceDetailPageAsync(filter, window);
  }

  /// <summary>
  /// 按标识查看互认匹配记录集合视图（单记录读取，不分页）。
  /// </summary>
  /// <remarks>
  /// 先按标识读取组级信息，未命中即按业务拒绝，不再读取匹配项集合；
  /// 命中后读取全部匹配项，接收方与来源方名称按涉及编码批量回填；
  /// 记录级反馈状态取组内聚合值，各项的反馈状态与决策取自身处理结果。
  /// </remarks>
  /// <param name="request">互认匹配记录标识。</param>
  /// <returns>匹配记录集合视图读模型。</returns>
  /// <exception cref="ArgumentNullException">查询条件为 null 时抛出。</exception>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">匹配记录标识为空 Guid 时抛出，此时不发生仓储访问。</exception>
  /// <exception cref="InvalidOperationException">匹配记录不存在，或归属名称解析失败、外部组织服务不可用时抛出。</exception>
  public async Task<RecognitionMatchRecordReadModel> QueryRecognitionMatchRecordAsync(RecognitionMatchRecordQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);

    // 先查询组级信息再判空：未知标识在读取匹配项集合之前整次拒绝，单记录读取没有分页窗口。
    RecognitionMatchRecordView? view = await repository.QueryRecognitionMatchRecordViewAsync(request.RecognitionMatchRecordId);
    if (view is null)
    {
      throw new InvalidOperationException("业务拒绝：互认匹配记录不存在。");
    }

    IReadOnlyList<RecognitionMatchRecordViewItem> items =
      [.. await repository.QueryRecognitionMatchRecordViewItemsAsync(request.RecognitionMatchRecordId)];

    // 接收方按组级三值一次解析；来源方按当组匹配项涉及编码批量解析，读取次数不随匹配项数量增长。
    Dictionary<(string, string, string), OrganizationPath> receiverNames = await ResolveStatisticsPageNamesAsync(
      [(view.ReceiverOrganizationCode, view.ReceiverHospitalCode, view.ReceiverBranchCode)],
      "业务拒绝：匹配记录的接收组织不存在或已停用。",
      "业务拒绝：匹配记录的接收医院不存在、已停用或不属于该组织。",
      "业务拒绝：匹配记录的接收院区不存在、已停用或不属于该医院。");
    Dictionary<(string, string, string), OrganizationPath> sourceNames = await ResolveStatisticsPageNamesAsync(
      [.. items.Select(item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode))],
      "业务拒绝：匹配项的来源组织不存在或已停用。",
      "业务拒绝：匹配项的来源医院不存在、已停用或不属于该组织。",
      "业务拒绝：匹配项的来源院区不存在、已停用或不属于该医院。");

    return new RecognitionMatchRecordReadModel
    {
      RecognitionMatchRecordId = view.RecognitionMatchRecordId,
      MatchCreatedTime = view.MatchCreatedTime,
      Receiver = MapReceiverOrganization(view.ReceiverOrganizationCode, view.ReceiverHospitalCode, view.ReceiverBranchCode, receiverNames),
      PatientName = view.PatientName ?? string.Empty,
      IdentityDocumentNo = view.IdentityDocumentNo,
      VisitType = view.VisitType,
      VisitSerialNo = view.VisitSerialNo,
      IsProcessed = view.IsProcessed,
      RecognitionTime = view.RecognitionTime,
      RecognitionDeptId = view.RecognitionDeptId,
      RecognitionDeptName = view.RecognitionDeptName,
      RecognitionDoctorId = view.RecognitionDoctorId,
      RecognitionDoctorName = view.RecognitionDoctorName,
      MatchItems = [.. items.Select(item => MapMatchRecordItem(item, sourceNames))]
    };
  }

  /// <summary>
  /// 校验平台入口的两组组织编码：同时提供且不一致时整次拒绝，不允许一次查询横跨两个互不一致的组织范围。
  /// </summary>
  /// <param name="organizationCode">请求提交的接收组织编码；可为空。</param>
  /// <param name="sourceOrganizationCode">请求提交的来源组织编码；可为空。</param>
  /// <exception cref="InvalidOperationException">两个组织编码都提供且不一致时抛出。</exception>
  private static void EnsureSameOrganization(string? organizationCode, string? sourceOrganizationCode)
  {
    string receiverCode = organizationCode?.Trim() ?? string.Empty;
    string sourceCode = sourceOrganizationCode?.Trim() ?? string.Empty;
    if (receiverCode.Length > 0 && sourceCode.Length > 0 && !string.Equals(receiverCode, sourceCode, StringComparison.Ordinal))
    {
      throw new InvalidOperationException("业务拒绝：接收组织与来源组织不一致，不能同时按两个组织统计。");
    }
  }

  /// <summary>
  /// 校验来源侧汇总的维度子集：只接受来源医院、来源院区与标准项目，互认科室维度值按业务拒绝。
  /// </summary>
  /// <remarks>
  /// 来源侧行集按采纳事实聚合，没有互认科室归属维度；校验在应用层、进入仓储之前完成。
  /// </remarks>
  /// <param name="groupDimension">请求提交的汇总维度。</param>
  /// <exception cref="InvalidOperationException">维度取互认科室值时抛出。</exception>
  private static void EnsureSourceGroupDimension(RecognitionStatisticsGroupDimension groupDimension)
  {
    if (groupDimension == RecognitionStatisticsGroupDimension.RecognitionDepartment)
    {
      throw new InvalidOperationException("业务拒绝：来源侧汇总不支持按互认科室维度统计。");
    }
  }

  /// <summary>
  /// 解析平台入口的一组可选范围条件：每层取值为空即未提供，跳过该层校验与过滤；
  /// 提供的取值按链路逐层校验存在、启用与父子归属——只提交组织时只校验组织层，
  /// 提交到医院时校验组织与医院两层，提交到院区时三层整体校验。
  /// </summary>
  /// <param name="organizationCode">请求提交的组织编码；可为空。</param>
  /// <param name="hospitalCode">请求提交的医院编码；可为空。</param>
  /// <param name="branchCode">请求提交的院区编码；可为空。</param>
  /// <param name="missingOrganizationMessage">组织不存在或已停用时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院不存在、已停用或不属于该组织时的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区不存在、已停用或不属于该医院时的业务拒绝文案。</param>
  /// <returns>已校验的组织路径；范围条件全空时为 <see langword="null"/>，未提供层的编码与名称为空串。</returns>
  /// <exception cref="InvalidOperationException">提供的任一层取值不存在、已停用或父子归属不匹配时抛出。</exception>
  private async Task<OrganizationPath?> ResolveOptionalScopeAsync(
    string? organizationCode,
    string? hospitalCode,
    string? branchCode,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage)
  {
    if (!HasAnyScope(organizationCode, hospitalCode, branchCode)) return null;

    // 院区为空时不进入院区层，医院为空时不进入医院层：各层只校验提供了取值的层，解析方法按提供的最深一层选择。
    if (string.IsNullOrWhiteSpace(branchCode))
    {
      if (string.IsNullOrWhiteSpace(hospitalCode))
      {
        return (await organizationPathResolver.ResolveOrganizationOrThrow(
          [new OrganizationPathTarget(organizationCode, null, null)],
          missingOrganizationMessage))[0];
      }

      return (await organizationPathResolver.ResolveOrganizationHospitalOrThrow(
        [new OrganizationPathTarget(organizationCode, hospitalCode, null)],
        missingOrganizationMessage,
        missingHospitalMessage))[0];
    }

    return (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(organizationCode, hospitalCode, branchCode)],
      missingOrganizationMessage,
      missingHospitalMessage,
      missingBranchMessage))[0];
  }

  /// <summary>
  /// 解析本院入口的本侧范围：组织与医院取可信上下文，院区取请求；
  /// 院区为空时只解析组织与医院两层、不进入院区层，非空时三层整体校验。
  /// </summary>
  /// <param name="trustedScope">当前请求的可信组织与可信医院。</param>
  /// <param name="branchCode">请求提交的本侧院区编码；可为空表示按可信医院全院范围。</param>
  /// <returns>已校验的本侧组织路径；院区为空时路径院区编码为空串。</returns>
  /// <exception cref="InvalidOperationException">可信组织或医院不存在、已停用，或请求院区不存在、已停用、不属于可信医院时抛出。</exception>
  private async Task<OrganizationPath> ResolveBranchLocalScopeAsync(TrustedScope trustedScope, string? branchCode)
  {
    // 院区为空的分支按可信医院全院收敛：解析目标只构造组织与医院两层，不读取也不校验院区层。
    if (string.IsNullOrWhiteSpace(branchCode))
    {
      return (await organizationPathResolver.ResolveOrganizationHospitalOrThrow(
        [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, null)],
        "业务拒绝：组织不存在或已停用。",
        "业务拒绝：医院不存在、已停用或不属于所选组织。"))[0];
    }

    return (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, trustedScope.HospitalCode, branchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：医院不存在、已停用或不属于所选组织。",
      "业务拒绝：院区不存在、已停用或不属于所选医院。"))[0];
  }

  /// <summary>
  /// 解析本院入口的来源组可选筛选：来源组织恒为可信组织，医院与院区取请求；
  /// 来源院区为空时不进入院区层、只校验来源医院属于可信组织，两层取值都为空时不做解析、不附加来源过滤。
  /// </summary>
  /// <param name="trustedScope">当前请求的可信组织与可信医院。</param>
  /// <param name="sourceHospitalCode">请求提交的来源医院编码；可为空。</param>
  /// <param name="sourceBranchCode">请求提交的来源院区编码；可为空。</param>
  /// <returns>已校验的来源组织路径；来源筛选全空时为 <see langword="null"/>，未提供层的编码与名称为空串。</returns>
  /// <exception cref="InvalidOperationException">来源医院或来源院区不存在、已停用或不属于可信组织、可信医院时抛出。</exception>
  private async Task<OrganizationPath?> ResolveBranchSourceScopeAsync(TrustedScope trustedScope, string? sourceHospitalCode, string? sourceBranchCode)
  {
    if (sourceHospitalCode is null && sourceBranchCode is null) return null;

    // 来源院区为空时不进入院区层：解析目标只构造组织与医院两层，来源医院经可信组织校验存在、启用与父子归属。
    if (string.IsNullOrWhiteSpace(sourceBranchCode))
    {
      return (await organizationPathResolver.ResolveOrganizationHospitalOrThrow(
        [new OrganizationPathTarget(trustedScope.OrganizationCode, sourceHospitalCode, null)],
        "业务拒绝：组织不存在或已停用。",
        "业务拒绝：来源医院不存在、已停用或不属于可信组织。"))[0];
    }

    return (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, sourceHospitalCode, sourceBranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：来源医院不存在、已停用或不属于可信组织。",
      "业务拒绝：来源院区不存在、已停用或不属于可信医院。"))[0];
  }

  /// <summary>
  /// 解析本院入口来源侧查询的接收组可选筛选：接收组织恒为可信组织，医院与院区取请求；
  /// 接收院区为空时不进入院区层、只校验接收医院属于可信组织，两层取值都为空时不做解析、不附加接收过滤。
  /// </summary>
  /// <param name="trustedScope">当前请求的可信组织与可信医院。</param>
  /// <param name="receiverHospitalCode">请求提交的接收医院编码；可为空。</param>
  /// <param name="receiverBranchCode">请求提交的接收院区编码；可为空。</param>
  /// <returns>已校验的接收组织路径；接收筛选全空时为 <see langword="null"/>，未提供层的编码与名称为空串。</returns>
  /// <exception cref="InvalidOperationException">接收医院或接收院区不存在、已停用或不属于可信组织、可信医院时抛出。</exception>
  private async Task<OrganizationPath?> ResolveBranchReceiverScopeAsync(TrustedScope trustedScope, string? receiverHospitalCode, string? receiverBranchCode)
  {
    if (receiverHospitalCode is null && receiverBranchCode is null) return null;

    // 接收院区为空时不进入院区层：解析目标只构造组织与医院两层，接收医院经可信组织校验存在、启用与父子归属。
    if (string.IsNullOrWhiteSpace(receiverBranchCode))
    {
      return (await organizationPathResolver.ResolveOrganizationHospitalOrThrow(
        [new OrganizationPathTarget(trustedScope.OrganizationCode, receiverHospitalCode, null)],
        "业务拒绝：组织不存在或已停用。",
        "业务拒绝：接收医院不存在、已停用或不属于可信组织。"))[0];
    }

    return (await organizationPathResolver.ResolveOrThrow(
      [new OrganizationPathTarget(trustedScope.OrganizationCode, receiverHospitalCode, receiverBranchCode)],
      "业务拒绝：组织不存在或已停用。",
      "业务拒绝：接收医院不存在、已停用或不属于可信组织。",
      "业务拒绝：接收院区不存在、已停用或不属于可信医院。"))[0];
  }

  /// <summary>
  /// 把解析路径中的一层编码转成筛选取值：该层未提供时路径编码为空串，筛选取空值表示不附加该层过滤。
  /// </summary>
  /// <param name="code">解析路径中该层的业务编码；未提供层为空串。</param>
  /// <returns>该层的筛选取值；该层未提供时为 <see langword="null"/>。</returns>
  private static string? ScopeFilterCode(string? code) => string.IsNullOrWhiteSpace(code) ? null : code;

  /// <summary>
  /// 把明细请求提交的不采纳原因代码解析为平台统一原因枚举；空白表示不施加该筛选。
  /// </summary>
  /// <param name="reasonCode">请求提交的原因代码文本；平台统一原因代码为枚举数值的十进制文本。</param>
  /// <returns>解析后的原因枚举；输入空白时为 <see langword="null"/>。</returns>
  /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">代码不是数字或不在平台统一原因取值域内时抛出。</exception>
  private static RecognitionNonAdoptionReason? ParseNonAdoptionReasonCode(string? reasonCode)
  {
    string normalized = reasonCode?.Trim() ?? string.Empty;
    if (normalized.Length == 0) return null;

    if (!int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
      || !Enum.IsDefined(typeof(RecognitionNonAdoptionReason), value))
    {
      throw new System.ComponentModel.DataAnnotations.ValidationException("参数校验失败：不采纳原因代码无效。");
    }

    return (RecognitionNonAdoptionReason)value;
  }

  /// <summary>
  /// 读取接收侧汇总的当页分组行并组装读模型：先取分组行总数与当页，再取原因行装配，最后按当页编码回填接收方名称。
  /// </summary>
  /// <remarks>
  /// 计数与取页共用同一套筛选条件，分页作用于聚合后的分组行；
  /// 原因汇总语句按分组键加原因代码取全部分组的原因行，应用层只把当页分组键所属的原因行装配进所属汇总行。
  /// </remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界、汇总维度。</param>
  /// <param name="window">已校验的分页窗口。</param>
  /// <returns>当页汇总分组行与分页信息。</returns>
  /// <exception cref="InvalidOperationException">外部组织服务不可用或当页分组行的接收归属不成立时抛出。</exception>
  private async Task<PageResultDto<RecognitionUsageSummaryReadModel>> ReadUsageSummaryPageAsync(RecognitionUsageSummaryFilter filter, PageQueryWindow window)
  {
    // 先取分组行总数，再取当页分组行：计数与取页共用同一套筛选条件，分页作用于聚合后的分组行。
    long totalCount = await repository.CountRecognitionUsageSummaryGroupsAsync(filter);
    IReadOnlyList<RecognitionUsageSummaryGroupItem> rows =
      [.. await repository.QueryRecognitionUsageSummaryPageAsync(filter, window.SkipCount, window.PageSize)];

    // 原因行独立成行返回，按分组键归并后只装配进当页所属分组行；其他页分组的原因行在此处自然丢弃。
    Dictionary<ReasonGroupKey, List<RecognitionUsageReasonItem>> reasonsByGroup =
      ClassifyReasonRows(await repository.QueryRecognitionUsageSummaryReasonsAsync(filter));

    // 接收方名称按当页分组行涉及的编码批量解析，读取次数只随组织与医院数量增长，不随行数增长。
    Dictionary<(string, string, string), OrganizationPath> receiverNames = await ResolveStatisticsPageNamesAsync(
      [.. rows.Select(row => (row.ReceiverOrganizationCode, row.ReceiverHospitalCode, row.ReceiverBranchCode))],
      "业务拒绝：汇总行的接收组织不存在或已停用。",
      "业务拒绝：汇总行的接收医院不存在、已停用或不属于该组织。",
      "业务拒绝：汇总行的接收院区不存在、已停用或不属于该医院。");

    return new PageResultDto<RecognitionUsageSummaryReadModel>
    {
      Items = [.. rows.Select(row => MapUsageSummaryRow(row, reasonsByGroup, receiverNames, filter))],
      Page = new PageInfoDto { PageIndex = window.PageIndex, PageSize = window.PageSize, TotalCount = totalCount }
    };
  }

  /// <summary>
  /// 把一条汇总分组行映射为读模型，并装配行内同期互认率、原因汇总与接收方名称。
  /// </summary>
  /// <param name="row">仓储返回的分组行投影。</param>
  /// <param name="reasonsByGroup">按分组键归并的原因行集合。</param>
  /// <param name="receiverNames">按当页编码解析出的接收方名称字典。</param>
  /// <param name="filter">本次查询的筛选条件，提供统计期间与汇总维度。</param>
  /// <returns>汇总行读模型。</returns>
  private static RecognitionUsageSummaryReadModel MapUsageSummaryRow(
    RecognitionUsageSummaryGroupItem row,
    Dictionary<ReasonGroupKey, List<RecognitionUsageReasonItem>> reasonsByGroup,
    Dictionary<(string, string, string), OrganizationPath> receiverNames,
    RecognitionUsageSummaryFilter filter)
  {
    ReasonGroupKey key = new(
      row.ReceiverOrganizationCode, row.ReceiverHospitalCode, row.ReceiverBranchCode,
      row.RecognitionDeptId, row.ItemType, row.StandardProjectCode);
    IReadOnlyList<RecognitionUsageReasonItem> rowReasons =
      reasonsByGroup.TryGetValue(key, out List<RecognitionUsageReasonItem>? reasons) ? reasons : [];

    (string OrganizationCode, string HospitalCode, string BranchCode) receiverKey = (
      row.ReceiverOrganizationCode?.Trim() ?? string.Empty,
      row.ReceiverHospitalCode?.Trim() ?? string.Empty,
      row.ReceiverBranchCode?.Trim() ?? string.Empty);
    bool hasReceiverScope = receiverKey.OrganizationCode.Length > 0;

    return new RecognitionUsageSummaryReadModel
    {
      ReceiverOrganizationCode = row.ReceiverOrganizationCode,
      ReceiverOrganizationName = hasReceiverScope ? receiverNames[receiverKey].OrganizationName : null,
      ReceiverHospitalCode = row.ReceiverHospitalCode,
      ReceiverHospitalName = hasReceiverScope ? receiverNames[receiverKey].HospitalName : null,
      ReceiverBranchCode = row.ReceiverBranchCode,
      ReceiverBranchName = row.ReceiverBranchCode is null ? null : receiverNames[receiverKey].BranchName,
      RecognitionDeptId = row.RecognitionDeptId,
      RecognitionDeptName = row.RecognitionDeptName,
      ItemType = row.ItemType,
      StandardProjectCode = row.StandardProjectCode,
      StandardProjectName = row.StandardItemName,
      CategoryName = row.CategoryName,
      GroupName = row.GroupName,
      PeriodStart = filter.PeriodStart,
      PeriodEnd = filter.PeriodEnd.AddTicks(-1),
      AdoptionCount = (int)row.AdoptionCount,
      NonAdoptionCount = (int)row.NonAdoptionCount,
      ReferenceCount = (int)row.ReferenceCount,
      ReminderCount = (int)row.ReminderCount,
      SamePeriodRecognitionRate = row.ReminderCount == 0 ? 0m : (decimal)row.AdoptionCount / row.ReminderCount,
      SamePeriodRecognitionRateCalculated = row.ReminderCount != 0,
      NonAdoptionReasons =
      [
        .. rowReasons
          .Where(reason => reason.NonAdoptionReason is not null)
          .Select(reason => MapReasonSummary(reason, row.NonAdoptionCount))
      ],
      EstimatedSavingAmount = row.EstimatedSavingAmount,
      GroupDimension = filter.GroupDimension
    };
  }

  /// <summary>
  /// 把一条原因行映射为不采纳原因汇总项，占比以行内不采纳次数为分母在行内计算。
  /// </summary>
  /// <param name="reason">仓储返回的原因行投影，原因代码已确认非空。</param>
  /// <param name="rowNonAdoptionCount">所属分组行内的不采纳总次数，占比分母。</param>
  /// <returns>不采纳原因汇总项。</returns>
  private static NonAdoptionReasonSummaryReadModel MapReasonSummary(RecognitionUsageReasonItem reason, long rowNonAdoptionCount) => new()
  {
    ReasonCode = ((int)reason.NonAdoptionReason!.Value).ToString(CultureInfo.InvariantCulture),
    ReasonName = ResolveEnumDescription(reason.NonAdoptionReason, RecognitionNonAdoptionReasonDescriptorList.List) ?? string.Empty,
    Count = (int)reason.ReasonCount,
    // 行内不采纳为零时不计算占比，取值保持零；语句谓词保证原因行只随不采纳事实产生，该分支只防御组装错位。
    Ratio = rowNonAdoptionCount == 0 ? 0m : (decimal)reason.ReasonCount / rowNonAdoptionCount
  };

  /// <summary>
  /// 按明细类型读取四类事实语句之一，并组装当页明细读模型。
  /// </summary>
  /// <remarks>
  /// 每类明细先取总数再取当页，两侧名称按当页行涉及的编码批量回填；应用层不重排仓储给出的行序。
  /// </remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <param name="detailType">明细类型，决定读取的事实语句。</param>
  /// <param name="window">已校验的分页窗口。</param>
  /// <returns>当页明细行与分页信息。</returns>
  /// <exception cref="InvalidOperationException">外部组织服务不可用、当页行的归属不成立，或明细类型不在接收侧取值范围内时抛出。</exception>
  private async Task<PageResultDto<RecognitionUsageDetailReadModel>> ReadUsageDetailPageAsync(
    RecognitionUsageDetailFilter filter, RecognitionUsageDetailType detailType, PageQueryWindow window)
  {
    switch (detailType)
    {
      case RecognitionUsageDetailType.Reminder:
      {
        long totalCount = await repository.CountRecognitionUsageReminderDetailsAsync(filter);
        IReadOnlyList<RecognitionReminderDetailItem> rows =
          [.. await repository.QueryRecognitionUsageReminderDetailsAsync(filter, window.SkipCount, window.PageSize)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return BuildUsageDetailPage(rows, totalCount, window, item => MapReminderDetail(item, names.ReceiverNames, names.SourceNames));
      }

      case RecognitionUsageDetailType.Adopted:
      {
        long totalCount = await repository.CountRecognitionUsageAdoptionDetailsAsync(filter);
        IReadOnlyList<RecognitionAdoptionDetailItem> rows =
          [.. await repository.QueryRecognitionUsageAdoptionDetailsAsync(filter, window.SkipCount, window.PageSize)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return BuildUsageDetailPage(rows, totalCount, window, item => MapAdoptionDetail(item, names.ReceiverNames, names.SourceNames));
      }

      case RecognitionUsageDetailType.NotAdopted:
      {
        long totalCount = await repository.CountRecognitionUsageNonAdoptionDetailsAsync(filter);
        IReadOnlyList<RecognitionNonAdoptionDetailItem> rows =
          [.. await repository.QueryRecognitionUsageNonAdoptionDetailsAsync(filter, window.SkipCount, window.PageSize)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return BuildUsageDetailPage(rows, totalCount, window, item => MapNonAdoptionDetail(item, names.ReceiverNames, names.SourceNames));
      }

      case RecognitionUsageDetailType.Referenced:
      {
        long totalCount = await repository.CountRecognitionUsageReferenceDetailsAsync(filter);
        IReadOnlyList<RecognitionReferenceDetailItem> rows =
          [.. await repository.QueryRecognitionUsageReferenceDetailsAsync(filter, window.SkipCount, window.PageSize)];
        (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
          await ResolveUsageDetailNamesAsync(
            rows,
            item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
            item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));
        return BuildUsageDetailPage(rows, totalCount, window, item => MapReferenceDetail(item, names.ReceiverNames, names.SourceNames));
      }

      default:
        throw new InvalidOperationException("业务拒绝：明细类型不在接收侧明细取值范围内。");
    }
  }

  /// <summary>
  /// 按当页明细行的双侧编码批量解析接收方与来源方名称。
  /// </summary>
  /// <remarks>
  /// 两个方向各做一次批量解析：读取次数只随当页涉及的组织与医院数量增长，不随行数增长；
  /// 来源编码为空的行来自缺失报告主体的历史事实，跳过其来源方解析。
  /// </summary>
  /// <typeparam name="TRow">当页明细行投影类型。</typeparam>
  /// <param name="rows">当页明细行。</param>
  /// <param name="receiverKeyOf">从行投影取接收方三层编码的取值器。</param>
  /// <param name="sourceKeyOf">从行投影取来源方三层编码的取值器。</param>
  /// <returns>接收方与来源方两个方向的名称字典，键为去空白后的三层编码。</returns>
  /// <exception cref="InvalidOperationException">任一行的归属不成立或外部组织服务不可用时抛出。</exception>
  private async Task<(
    Dictionary<(string, string, string), OrganizationPath> ReceiverNames,
    Dictionary<(string, string, string), OrganizationPath> SourceNames)> ResolveUsageDetailNamesAsync<TRow>(
    IReadOnlyList<TRow> rows,
    Func<TRow, (string?, string?, string?)> receiverKeyOf,
    Func<TRow, (string?, string?, string?)> sourceKeyOf)
  {
    Dictionary<(string, string, string), OrganizationPath> receiverNames = await ResolveStatisticsPageNamesAsync(
      [.. rows.Select(receiverKeyOf)],
      "业务拒绝：明细行的接收组织不存在或已停用。",
      "业务拒绝：明细行的接收医院不存在、已停用或不属于该组织。",
      "业务拒绝：明细行的接收院区不存在、已停用或不属于该医院。");
    Dictionary<(string, string, string), OrganizationPath> sourceNames = await ResolveStatisticsPageNamesAsync(
      [.. rows.Select(sourceKeyOf)],
      "业务拒绝：明细行的来源组织不存在或已停用。",
      "业务拒绝：明细行的来源医院不存在、已停用或不属于该组织。",
      "业务拒绝：明细行的来源院区不存在、已停用或不属于该医院。");
    return (receiverNames, sourceNames);
  }

  /// <summary>
  /// 把一批当页行的三层编码去重后批量解析为名称字典。
  /// </summary>
  /// <remarks>
  /// 编码为空的组织键表示该行缺失归属事实（如未绑定报告的来源方），跳过解析；
  /// 院区编码为空的目标按组织与医院两层解析，有院区编码的目标按三层解析，两批各自批量读取。
  /// </remarks>
  /// <param name="keys">当页行的三层编码集合，允许为空值。</param>
  /// <param name="missingOrganizationMessage">组织不存在或已停用时的业务拒绝文案。</param>
  /// <param name="missingHospitalMessage">医院不存在、已停用或不属于该组织时的业务拒绝文案。</param>
  /// <param name="missingBranchMessage">院区不存在、已停用或不属于该医院时的业务拒绝文案。</param>
  /// <returns>按去空白三层编码索引的已校验路径字典；无有效编码时为空字典。</returns>
  /// <exception cref="InvalidOperationException">任一有效编码的归属不成立或外部组织服务不可用时抛出。</exception>
  private async Task<Dictionary<(string, string, string), OrganizationPath>> ResolveStatisticsPageNamesAsync(
    IReadOnlyList<(string? OrganizationCode, string? HospitalCode, string? BranchCode)> keys,
    string missingOrganizationMessage,
    string missingHospitalMessage,
    string missingBranchMessage)
  {
    List<(string OrganizationCode, string HospitalCode, string BranchCode)> targets =
    [
      .. keys
        .Select(key => (
          OrganizationCode: key.OrganizationCode?.Trim() ?? string.Empty,
          HospitalCode: key.HospitalCode?.Trim() ?? string.Empty,
          BranchCode: key.BranchCode?.Trim() ?? string.Empty))
        .Where(target => target.OrganizationCode.Length > 0)
        .Distinct()
    ];
    if (targets.Count == 0) return [];

    // 指定了院区的目标三层整体校验；未指定院区的目标按两层解析，名称与编码从两批结果合并。
    List<(string OrganizationCode, string HospitalCode, string BranchCode)> withBranch =
      [.. targets.Where(target => target.BranchCode.Length > 0)];
    List<(string OrganizationCode, string HospitalCode, string BranchCode)> withoutBranch =
      [.. targets.Where(target => target.BranchCode.Length == 0)];

    Dictionary<(string, string, string), OrganizationPath> paths = [];
    if (withBranch.Count > 0)
    {
      IReadOnlyList<OrganizationPath> resolved = await organizationPathResolver.ResolveOrThrow(
        [.. withBranch.Select(target => new OrganizationPathTarget(target.OrganizationCode, target.HospitalCode, target.BranchCode))],
        missingOrganizationMessage,
        missingHospitalMessage,
        missingBranchMessage);
      for (int index = 0; index < withBranch.Count; index++)
      {
        paths[withBranch[index]] = resolved[index];
      }
    }

    if (withoutBranch.Count > 0)
    {
      IReadOnlyList<OrganizationPath> resolved = await organizationPathResolver.ResolveOrganizationHospitalOrThrow(
        [.. withoutBranch.Select(target => new OrganizationPathTarget(target.OrganizationCode, target.HospitalCode, null))],
        missingOrganizationMessage,
        missingHospitalMessage);
      for (int index = 0; index < withoutBranch.Count; index++)
      {
        paths[withoutBranch[index]] = resolved[index];
      }
    }

    return paths;
  }

  /// <summary>
  /// 把当页明细行与总数组装为分页响应，行序保持仓储给出的顺序。
  /// </summary>
  /// <typeparam name="TRow">当页明细行投影类型。</typeparam>
  /// <param name="rows">当页明细行。</param>
  /// <param name="totalCount">满足同一套筛选条件的总数。</param>
  /// <param name="window">已校验的分页窗口。</param>
  /// <param name="mapRow">把行投影映射为读模型的映射器。</param>
  /// <returns>当页明细与分页信息。</returns>
  private static PageResultDto<RecognitionUsageDetailReadModel> BuildUsageDetailPage<TRow>(
    IReadOnlyList<TRow> rows,
    long totalCount,
    PageQueryWindow window,
    Func<TRow, RecognitionUsageDetailReadModel> mapRow) => new()
  {
    Items = [.. rows.Select(mapRow)],
    Page = new PageInfoDto { PageIndex = window.PageIndex, PageSize = window.PageSize, TotalCount = totalCount }
  };

  /// <summary>
  /// 读取来源侧汇总的当页分组行并组装读模型：先取分组行总数与当页，再按当页编码回填来源方名称。
  /// </summary>
  /// <remarks>计数与取页共用同一套筛选条件，分页作用于聚合后的分组行；来源侧只有来源方一侧名称需要回填。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界、汇总维度。</param>
  /// <param name="window">已校验的分页窗口。</param>
  /// <returns>当页汇总分组行与分页信息。</returns>
  /// <exception cref="InvalidOperationException">外部组织服务不可用或当页分组行的来源归属不成立时抛出。</exception>
  private async Task<PageResultDto<SourceRecognitionSummaryReadModel>> ReadSourceSummaryPageAsync(SourceRecognitionSummaryFilter filter, PageQueryWindow window)
  {
    // 先取分组行总数，再取当页分组行：计数与取页共用同一套筛选条件，分页作用于聚合后的分组行。
    long totalCount = await repository.CountSourceRecognitionSummaryGroupsAsync(filter);
    IReadOnlyList<SourceRecognitionSummaryGroupItem> rows =
      [.. await repository.QuerySourceRecognitionSummaryPageAsync(filter, window.SkipCount, window.PageSize)];

    // 来源方名称按当页分组行涉及的编码批量解析，读取次数只随组织与医院数量增长，不随行数增长。
    Dictionary<(string, string, string), OrganizationPath> sourceNames = await ResolveStatisticsPageNamesAsync(
      [.. rows.Select(row => (row.SourceOrganizationCode, row.SourceHospitalCode, row.SourceBranchCode))],
      "业务拒绝：汇总行的来源组织不存在或已停用。",
      "业务拒绝：汇总行的来源医院不存在、已停用或不属于该组织。",
      "业务拒绝：汇总行的来源院区不存在、已停用或不属于该医院。");

    return new PageResultDto<SourceRecognitionSummaryReadModel>
    {
      Items = [.. rows.Select(row => MapSourceSummaryRow(row, sourceNames, filter))],
      Page = new PageInfoDto { PageIndex = window.PageIndex, PageSize = window.PageSize, TotalCount = totalCount }
    };
  }

  /// <summary>
  /// 把一条来源侧汇总分组行映射为读模型，并装配行内被互认次数与来源方名称。
  /// </summary>
  /// <param name="row">仓储返回的分组行投影。</param>
  /// <param name="sourceNames">按当页编码解析出的来源方名称字典。</param>
  /// <param name="filter">本次查询的筛选条件，提供统计期间与汇总维度。</param>
  /// <returns>来源侧汇总行读模型。</returns>
  private static SourceRecognitionSummaryReadModel MapSourceSummaryRow(
    SourceRecognitionSummaryGroupItem row,
    Dictionary<(string, string, string), OrganizationPath> sourceNames,
    SourceRecognitionSummaryFilter filter)
  {
    // 标准项目维度行没有来源归属键，名称保持空值，由维度字段本身表达分组含义。
    (string OrganizationCode, string HospitalCode, string BranchCode) sourceKey = (
      row.SourceOrganizationCode?.Trim() ?? string.Empty,
      row.SourceHospitalCode?.Trim() ?? string.Empty,
      row.SourceBranchCode?.Trim() ?? string.Empty);
    bool hasSourceScope = sourceKey.OrganizationCode.Length > 0;

    return new SourceRecognitionSummaryReadModel
    {
      SourceOrganizationCode = row.SourceOrganizationCode,
      SourceOrganizationName = hasSourceScope ? sourceNames[sourceKey].OrganizationName : null,
      SourceHospitalCode = row.SourceHospitalCode,
      SourceHospitalName = hasSourceScope ? sourceNames[sourceKey].HospitalName : null,
      SourceBranchCode = row.SourceBranchCode,
      SourceBranchName = row.SourceBranchCode is null ? null : sourceNames[sourceKey].BranchName,
      ItemType = row.ItemType,
      StandardProjectCode = row.StandardProjectCode,
      StandardProjectName = row.StandardItemName,
      CategoryName = row.CategoryName,
      GroupName = row.GroupName,
      PeriodStart = filter.PeriodStart,
      PeriodEnd = filter.PeriodEnd.AddTicks(-1),
      RecognitionCount = (int)row.RecognitionCount,
      GroupDimension = filter.GroupDimension
    };
  }

  /// <summary>
  /// 读取来源侧被互认明细的当页行并组装读模型：先取总数再取当页，两侧名称按当页行涉及的编码批量回填。
  /// </summary>
  /// <remarks>计数与取页共用同一套筛选条件；应用层不重排仓储给出的行序。</remarks>
  /// <param name="filter">已解析的筛选条件与必填的日期边界。</param>
  /// <param name="window">已校验的分页窗口。</param>
  /// <returns>当页明细行与分页信息。</returns>
  /// <exception cref="InvalidOperationException">外部组织服务不可用或当页行的归属不成立时抛出。</exception>
  private async Task<PageResultDto<SourceRecognitionDetailReadModel>> ReadSourceDetailPageAsync(SourceRecognitionDetailFilter filter, PageQueryWindow window)
  {
    long totalCount = await repository.CountSourceRecognitionDetailsAsync(filter);
    IReadOnlyList<SourceRecognitionDetailItem> rows =
      [.. await repository.QuerySourceRecognitionDetailsAsync(filter, window.SkipCount, window.PageSize)];

    (Dictionary<(string, string, string), OrganizationPath> ReceiverNames, Dictionary<(string, string, string), OrganizationPath> SourceNames) names =
      await ResolveUsageDetailNamesAsync(
        rows,
        item => (item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode),
        item => (item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode));

    return new PageResultDto<SourceRecognitionDetailReadModel>
    {
      Items = [.. rows.Select(item => MapSourceDetail(item, names.ReceiverNames, names.SourceNames))],
      Page = new PageInfoDto { PageIndex = window.PageIndex, PageSize = window.PageSize, TotalCount = totalCount }
    };
  }

  /// <summary>
  /// 把一条来源侧明细行映射为读模型：互认科室与医生取处理结果自身保存值，
  /// 患者姓名与证件号码按业务原值返回，互认时间即来源侧明细的业务时间。
  /// </summary>
  /// <param name="item">仓储返回的来源侧明细行投影。</param>
  /// <param name="receiverNames">接收方名称字典。</param>
  /// <param name="sourceNames">来源方名称字典。</param>
  /// <returns>来源侧明细读模型。</returns>
  private static SourceRecognitionDetailReadModel MapSourceDetail(
    SourceRecognitionDetailItem item,
    Dictionary<(string, string, string), OrganizationPath> receiverNames,
    Dictionary<(string, string, string), OrganizationPath> sourceNames)
  {
    // 来源侧行集由报告主体内联连接产生，两侧三层编码恒有值，名称按编码批量回填。
    OrganizationPath sourcePath = sourceNames[(item.SourceOrganizationCode.Trim(), item.SourceHospitalCode.Trim(), item.SourceBranchCode.Trim())];
    OrganizationPath receiverPath = receiverNames[(item.ReceiverOrganizationCode.Trim(), item.ReceiverHospitalCode.Trim(), item.ReceiverBranchCode.Trim())];

    return new SourceRecognitionDetailReadModel
    {
      RecognitionMatchRecordId = item.RecognitionMatchRecordId,
      RecognitionMatchItemId = item.RecognitionMatchItemId,
      SourceOrganizationCode = item.SourceOrganizationCode,
      SourceOrganizationName = sourcePath.OrganizationName,
      SourceHospitalCode = item.SourceHospitalCode,
      SourceHospitalName = sourcePath.HospitalName,
      SourceBranchCode = item.SourceBranchCode,
      SourceBranchName = sourcePath.BranchName,
      ReceiverOrganizationCode = item.ReceiverOrganizationCode,
      ReceiverOrganizationName = receiverPath.OrganizationName,
      ReceiverHospitalCode = item.ReceiverHospitalCode,
      ReceiverHospitalName = receiverPath.HospitalName,
      ReceiverBranchCode = item.ReceiverBranchCode,
      ReceiverBranchName = receiverPath.BranchName,
      StandardProjectCode = item.StandardProjectCode,
      RecognitionDeptId = item.RecognitionDeptId,
      RecognitionDeptName = item.RecognitionDeptName,
      RecognitionDoctorId = item.RecognitionDoctorId,
      RecognitionDoctorName = item.RecognitionDoctorName,
      RecognitionTime = item.RecognitionTime,
      PatientName = item.PatientName ?? string.Empty,
      IdentityDocumentNo = item.IdentityDocumentNo
    };
  }

  /// <summary>
  /// 把一条提醒明细行映射为读模型：业务时间取匹配生成时间，处理结果字段按反馈状态承载，
  /// 决策取处理结果保存值，未反馈行随左联自然为空。
  /// </summary>
  /// <param name="item">仓储返回的提醒明细行投影。</param>
  /// <param name="receiverNames">接收方名称字典。</param>
  /// <param name="sourceNames">来源方名称字典。</param>
  /// <returns>提醒明细读模型。</returns>
  private static RecognitionUsageDetailReadModel MapReminderDetail(
    RecognitionReminderDetailItem item,
    Dictionary<(string, string, string), OrganizationPath> receiverNames,
    Dictionary<(string, string, string), OrganizationPath> sourceNames) => new()
  {
    RecognitionMatchRecordId = item.RecognitionMatchRecordId,
    RecognitionMatchItemId = item.RecognitionMatchItemId,
    MatchCreatedTime = item.MatchCreatedTime,
    VisitType = item.VisitType,
    VisitSerialNo = item.VisitSerialNo,
    Source = MapSourceOrganization(item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode, sourceNames),
    Receiver = MapReceiverOrganization(item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode, receiverNames),
    Item = MapStatisticsItem(item.ItemType, item.CategoryName, item.GroupName, item.StandardProjectCode, item.StandardItemName),
    BusinessTime = item.MatchCreatedTime,
    IsUnprocessed = item.ProcessingTime is null,
    PatientName = item.PatientName ?? string.Empty,
    IdentityDocumentNo = item.IdentityDocumentNo,
    RecognitionDeptId = item.ProcessingDeptId,
    RecognitionDeptName = item.ProcessingDeptName,
    RecognitionDoctorId = item.ProcessingDoctorId,
    RecognitionDoctorName = item.ProcessingDoctorName,
    ProcessingResult = new RecognitionProcessingResultDetailReadModel
    {
      IsProcessed = item.ProcessingTime is not null,
      RecognitionTime = item.ProcessingTime,
      Decision = item.Decision
    }
  };

  /// <summary>
  /// 把一条采纳明细行映射为读模型：业务时间取互认时间，决策为采纳并随预计节省金额。
  /// </summary>
  /// <param name="item">仓储返回的采纳明细行投影。</param>
  /// <param name="receiverNames">接收方名称字典。</param>
  /// <param name="sourceNames">来源方名称字典。</param>
  /// <returns>采纳明细读模型。</returns>
  private static RecognitionUsageDetailReadModel MapAdoptionDetail(
    RecognitionAdoptionDetailItem item,
    Dictionary<(string, string, string), OrganizationPath> receiverNames,
    Dictionary<(string, string, string), OrganizationPath> sourceNames) => new()
  {
    RecognitionMatchRecordId = item.RecognitionMatchRecordId,
    RecognitionMatchItemId = item.RecognitionMatchItemId,
    MatchCreatedTime = item.MatchCreatedTime,
    VisitType = item.VisitType,
    VisitSerialNo = item.VisitSerialNo,
    Source = MapSourceOrganization(item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode, sourceNames),
    Receiver = MapReceiverOrganization(item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode, receiverNames),
    Item = MapStatisticsItem(item.ItemType, item.CategoryName, item.GroupName, item.StandardProjectCode, item.StandardItemName),
    BusinessTime = item.ProcessingTime,
    IsUnprocessed = false,
    PatientName = item.PatientName ?? string.Empty,
    IdentityDocumentNo = item.IdentityDocumentNo,
    RecognitionDeptId = item.ProcessingDeptId,
    RecognitionDeptName = item.ProcessingDeptName,
    RecognitionDoctorId = item.ProcessingDoctorId,
    RecognitionDoctorName = item.ProcessingDoctorName,
    ProcessingResult = new RecognitionProcessingResultDetailReadModel
    {
      IsProcessed = true,
      RecognitionTime = item.ProcessingTime,
      Decision = RecognitionResult.Adopted,
      EstimatedSavingAmount = item.EstimatedSavingAmount ?? 0m
    }
  };

  /// <summary>
  /// 把一条不采纳明细行映射为读模型：业务时间取互认时间，决策为不采纳并随原因代码、名称与补充说明。
  /// </summary>
  /// <param name="item">仓储返回的不采纳明细行投影。</param>
  /// <param name="receiverNames">接收方名称字典。</param>
  /// <param name="sourceNames">来源方名称字典。</param>
  /// <returns>不采纳明细读模型。</returns>
  private static RecognitionUsageDetailReadModel MapNonAdoptionDetail(
    RecognitionNonAdoptionDetailItem item,
    Dictionary<(string, string, string), OrganizationPath> receiverNames,
    Dictionary<(string, string, string), OrganizationPath> sourceNames) => new()
  {
    RecognitionMatchRecordId = item.RecognitionMatchRecordId,
    RecognitionMatchItemId = item.RecognitionMatchItemId,
    MatchCreatedTime = item.MatchCreatedTime,
    VisitType = item.VisitType,
    VisitSerialNo = item.VisitSerialNo,
    Source = MapSourceOrganization(item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode, sourceNames),
    Receiver = MapReceiverOrganization(item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode, receiverNames),
    Item = MapStatisticsItem(item.ItemType, item.CategoryName, item.GroupName, item.StandardProjectCode, item.StandardItemName),
    BusinessTime = item.ProcessingTime,
    IsUnprocessed = false,
    PatientName = item.PatientName ?? string.Empty,
    IdentityDocumentNo = item.IdentityDocumentNo,
    RecognitionDeptId = item.ProcessingDeptId,
    RecognitionDeptName = item.ProcessingDeptName,
    RecognitionDoctorId = item.ProcessingDoctorId,
    RecognitionDoctorName = item.ProcessingDoctorName,
    ProcessingResult = new RecognitionProcessingResultDetailReadModel
    {
      IsProcessed = true,
      RecognitionTime = item.ProcessingTime,
      Decision = RecognitionResult.NotAdopted,
      NonAdoptionReasonCode = ((int)item.NonAdoptionReason).ToString(CultureInfo.InvariantCulture),
      NonAdoptionReasonName = ResolveEnumDescription((RecognitionNonAdoptionReason?)item.NonAdoptionReason, RecognitionNonAdoptionReasonDescriptorList.List) ?? string.Empty,
      NonAdoptionSupplementDescription = item.NonAdoptionDescription
    }
  };

  /// <summary>
  /// 把一条引用明细行映射为读模型：业务时间取实际引用时间，互认科室与引用科室取各自业务记录保存值。
  /// </summary>
  /// <param name="item">仓储返回的引用明细行投影。</param>
  /// <param name="receiverNames">接收方名称字典。</param>
  /// <param name="sourceNames">来源方名称字典。</param>
  /// <returns>引用明细读模型。</returns>
  private static RecognitionUsageDetailReadModel MapReferenceDetail(
    RecognitionReferenceDetailItem item,
    Dictionary<(string, string, string), OrganizationPath> receiverNames,
    Dictionary<(string, string, string), OrganizationPath> sourceNames) => new()
  {
    RecognitionMatchRecordId = item.RecognitionMatchRecordId,
    RecognitionMatchItemId = item.RecognitionMatchItemId,
    MatchCreatedTime = item.MatchCreatedTime,
    VisitType = item.VisitType,
    VisitSerialNo = item.VisitSerialNo,
    Source = MapSourceOrganization(item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode, sourceNames),
    Receiver = MapReceiverOrganization(item.ReceiverOrganizationCode, item.ReceiverHospitalCode, item.ReceiverBranchCode, receiverNames),
    Item = MapStatisticsItem(item.ItemType, item.CategoryName, item.GroupName, item.StandardProjectCode, item.StandardItemName),
    BusinessTime = item.ReferenceTime,
    IsUnprocessed = false,
    PatientName = item.PatientName ?? string.Empty,
    IdentityDocumentNo = item.IdentityDocumentNo,
    RecognitionDeptId = item.ProcessingDeptId,
    RecognitionDeptName = item.ProcessingDeptName,
    RecognitionDoctorId = item.ProcessingDoctorId,
    RecognitionDoctorName = item.ProcessingDoctorName,
    ProcessingResult = new RecognitionProcessingResultDetailReadModel
    {
      IsProcessed = true,
      RecognitionTime = item.ProcessingTime,
      Decision = RecognitionResult.Adopted
    },
    Reference = new RecognitionReferenceDetailReadModel
    {
      IsReferenced = true,
      ReferenceTime = item.ReferenceTime,
      ReferenceDeptId = item.ReferenceDeptId,
      ReferenceDeptName = item.ReferenceDeptName,
      ReferenceDoctorId = item.ReferenceDoctorId,
      ReferenceDoctorName = item.ReferenceDoctorName
    }
  };

  /// <summary>
  /// 把明细行携带的互认项目资料映射为统计项目读模型。
  /// </summary>
  /// <param name="itemType">项目类型，取匹配项自身保存值。</param>
  /// <param name="categoryName">标准目录分类名称；目录缺失时为空值，按空串展示。</param>
  /// <param name="groupName">标准目录分组名称。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <param name="standardItemName">标准项目名称。</param>
  /// <returns>统计项目读模型。</returns>
  private static RecognitionStatisticsItemReadModel MapStatisticsItem(
    MedicalItemType itemType, string? categoryName, string? groupName, string standardProjectCode, string? standardItemName) => new()
  {
    ItemType = itemType,
    CategoryName = categoryName ?? string.Empty,
    GroupName = groupName ?? string.Empty,
    StandardProjectCode = standardProjectCode,
    StandardProjectName = standardItemName ?? string.Empty
  };

  /// <summary>
  /// 用名称字典组装明细行的来源方归属；来源组织编码为空表示缺失报告主体，按空归属返回。
  /// </summary>
  /// <param name="organizationCode">来源组织编码；可为空。</param>
  /// <param name="hospitalCode">来源医院编码；可为空。</param>
  /// <param name="branchCode">来源院区编码；可为空。</param>
  /// <param name="sourceNames">来源方名称字典。</param>
  /// <returns>来源方归属读模型。</returns>
  private static SourceOrganizationReadModel MapSourceOrganization(
    string? organizationCode,
    string? hospitalCode,
    string? branchCode,
    Dictionary<(string, string, string), OrganizationPath> sourceNames)
  {
    (string OrganizationCode, string HospitalCode, string BranchCode) key = (
      organizationCode?.Trim() ?? string.Empty,
      hospitalCode?.Trim() ?? string.Empty,
      branchCode?.Trim() ?? string.Empty);
    if (key.OrganizationCode.Length == 0) return new SourceOrganizationReadModel();

    OrganizationPath path = sourceNames[key];
    return new SourceOrganizationReadModel
    {
      OrganizationCode = key.OrganizationCode,
      OrganizationName = path.OrganizationName,
      HospitalCode = key.HospitalCode,
      HospitalName = path.HospitalName,
      BranchCode = key.BranchCode,
      BranchName = path.BranchName
    };
  }

  /// <summary>
  /// 用名称字典组装明细行的接收方归属；接收三层编码来自匹配记录事实，恒有值。
  /// </summary>
  /// <param name="organizationCode">接收组织编码。</param>
  /// <param name="hospitalCode">接收医院编码。</param>
  /// <param name="branchCode">接收院区编码。</param>
  /// <param name="receiverNames">接收方名称字典。</param>
  /// <returns>接收方归属读模型。</returns>
  private static ReceiverOrganizationReadModel MapReceiverOrganization(
    string organizationCode,
    string hospitalCode,
    string branchCode,
    Dictionary<(string, string, string), OrganizationPath> receiverNames)
  {
    (string OrganizationCode, string HospitalCode, string BranchCode) key =
      (organizationCode.Trim(), hospitalCode.Trim(), branchCode.Trim());
    OrganizationPath path = receiverNames[key];
    return new ReceiverOrganizationReadModel
    {
      OrganizationCode = key.OrganizationCode,
      OrganizationName = path.OrganizationName,
      HospitalCode = key.HospitalCode,
      HospitalName = path.HospitalName,
      BranchCode = key.BranchCode,
      BranchName = path.BranchName
    };
  }

  /// <summary>
  /// 把匹配记录集合视图的一条匹配项行映射为读模型：项目资料、来源归属与该项自身的反馈事实。
  /// </summary>
  /// <remarks>
  /// 决策与原因取匹配项自身处理结果，未反馈项不推断为不采纳，决策与原因为空值由页面显示未反馈；
  /// 来源方归属用名称字典组装，缺失报告主体的匹配项按空归属返回。
  /// </remarks>
  /// <param name="item">仓储返回的匹配项行投影。</param>
  /// <param name="sourceNames">来源方名称字典。</param>
  /// <returns>匹配项读模型。</returns>
  private static RecognitionMatchRecordItemReadModel MapMatchRecordItem(
    RecognitionMatchRecordViewItem item,
    Dictionary<(string, string, string), OrganizationPath> sourceNames) => new()
  {
    RecognitionMatchItemId = item.RecognitionMatchItemId,
    Item = MapStatisticsItem(item.ItemType, item.CategoryName, item.GroupName, item.StandardProjectCode, item.StandardItemName),
    Source = MapSourceOrganization(item.SourceOrganizationCode, item.SourceHospitalCode, item.SourceBranchCode, sourceNames),
    ReportId = item.ReportId,
    ReportVersionId = item.ReportVersionId,
    IsProcessed = item.IsProcessed,
    Decision = item.Decision,
    NonAdoptionReasonCode = item.NonAdoptionReason is null
      ? null
      : ((int)item.NonAdoptionReason.Value).ToString(CultureInfo.InvariantCulture),
    NonAdoptionReasonName = ResolveEnumDescription(item.NonAdoptionReason, RecognitionNonAdoptionReasonDescriptorList.List)
  };

  /// <summary>
  /// 不采纳原因行按汇总分组键归并所用的键：与汇总分组语句同套的维度取值。
  /// </summary>
  /// <param name="OrganizationCode">接收组织编码；未参与分组的维度为空。</param>
  /// <param name="HospitalCode">接收医院编码。</param>
  /// <param name="BranchCode">接收院区编码。</param>
  /// <param name="RecognitionDeptId">互认科室ID。</param>
  /// <param name="ItemType">项目类型。</param>
  /// <param name="StandardProjectCode">标准项目编码。</param>
  private sealed record ReasonGroupKey(
    string? OrganizationCode,
    string? HospitalCode,
    string? BranchCode,
    string? RecognitionDeptId,
    MedicalItemType? ItemType,
    string? StandardProjectCode);
}
