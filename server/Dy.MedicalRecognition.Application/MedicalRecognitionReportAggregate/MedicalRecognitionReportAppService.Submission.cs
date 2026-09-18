using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.Reports;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Application.Validation;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;
using Dy.MedicalRecognition.Domain.Share.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Queries.Ports;

namespace Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告采集与生命周期的写入口：两个完整报告提交入口、两个报告作废入口与报告版本 PDF 读取。
/// </summary>
/// <remarks>
/// 两个提交入口同时是事务边界，全部写入与事件登记共同成功或共同失败；
/// 控制器只做 multipart 绑定、PDF 校验与落盘、失败补偿，业务规则与写入顺序都在领域管理器内。
/// 两个作废入口只有一条写语句，依靠语句级原子性，不声明显式事务。
/// 报告文档按契约声明的字段名反序列化，未知字段一律拒绝，避免调用方提交的名称拼写错误被静默忽略。
/// </remarks>
public partial class MedicalRecognitionReportAppService
{
  /// <summary>
  /// 完整报告文档的反序列化选项：字段名为 camelCase，未知字段一律拒绝。
  /// </summary>
  private static readonly JsonSerializerOptions ReportDocumentJsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
  };

  /// <summary>
  /// 提交一份完整检验报告。
  /// </summary>
  /// <param name="request">提交请求，携带完整检验报告 JSON 文档与已落盘的 PDF 文件键和下载名。</param>
  /// <returns>提交成功返回 <see langword="true"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告文档或 PDF 文件键、下载名缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">文档不可反序列化、可信归属不可解析、文件键不可读或领域层拒绝时抛出。</exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> SubmitCompleteLaboratoryReportAsync(CompleteReportSubmissionRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    LaboratoryReportVersionRequest document = DeserializeReportDocument<LaboratoryReportVersionRequest>(request.ReportJson);
    (OrganizationPath path, DateTime receivedTime) = await ResolveReportScopeAsync();
    Guid operId = ResolveOperatorId();
    await EnsurePdfFileReadableAsync(request.PdfFileId);

    SubmitCompleteLaboratoryReportCommand command = new()
    {
      ReportNo = document.ReportNo,
      Version = document.Version,
      Content = document.Content,
      Results = document.Results,
      BacteriaResults = document.BacteriaResults,
      OrganizationCode = path.OrganizationCode,
      HospitalCode = path.HospitalCode,
      BranchCode = path.BranchCode,
      PdfFileId = request.PdfFileId,
      PdfFileName = request.PdfFileName,
      OperId = operId,
      OperTime = DateTimeOffset.UtcNow,
      ReceivedTime = receivedTime
    };
    return await manager.SubmitCompleteLaboratoryReportAsync(command);
  }

  /// <summary>
  /// 提交一份完整检查报告。
  /// </summary>
  /// <param name="request">提交请求，携带完整检查报告 JSON 文档与已落盘的 PDF 文件键和下载名。</param>
  /// <returns>提交成功返回 <see langword="true"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告文档或 PDF 文件键、下载名缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">文档不可反序列化、可信归属不可解析、文件键不可读或领域层拒绝时抛出。</exception>
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> SubmitCompleteExaminationReportAsync(CompleteReportSubmissionRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    ExaminationReportVersionRequest document = DeserializeReportDocument<ExaminationReportVersionRequest>(request.ReportJson);
    (OrganizationPath path, DateTime receivedTime) = await ResolveReportScopeAsync();
    Guid operId = ResolveOperatorId();
    await EnsurePdfFileReadableAsync(request.PdfFileId);

    SubmitCompleteExaminationReportCommand command = new()
    {
      ReportNo = document.ReportNo,
      Version = document.Version,
      Content = document.Content,
      Items = document.Items,
      OrganizationCode = path.OrganizationCode,
      HospitalCode = path.HospitalCode,
      BranchCode = path.BranchCode,
      PdfFileId = request.PdfFileId,
      PdfFileName = request.PdfFileName,
      OperId = operId,
      OperTime = DateTimeOffset.UtcNow,
      ReceivedTime = receivedTime
    };
    return await manager.SubmitCompleteExaminationReportAsync(command);
  }

  /// <summary>
  /// 作废一份检验报告。
  /// </summary>
  /// <param name="request">作废请求，携带报告单号、作废时间与作废原因。</param>
  /// <returns>作废成功或已是同一作废事实时返回 <see langword="true"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告单号或作废原因缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">可信归属不可解析、报告不存在、作废时间越界或作废信息冲突时抛出。</exception>
  public async Task<bool> VoidLaboratoryReportAsync(MedicalReportVoidRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    (OrganizationPath path, DateTime receivedTime) = await ResolveReportScopeAsync();

    VoidLaboratoryReportCommand command = new()
    {
      ReportNo = request.ReportNo,
      VoidedTime = request.VoidedTime,
      VoidReason = request.VoidReason,
      OrganizationCode = path.OrganizationCode,
      HospitalCode = path.HospitalCode,
      BranchCode = path.BranchCode,
      OperId = ResolveOperatorId(),
      OperTime = DateTimeOffset.UtcNow,
      ReceivedTime = receivedTime
    };
    return await manager.VoidLaboratoryReportAsync(command);
  }

  /// <summary>
  /// 作废一份检查报告。
  /// </summary>
  /// <param name="request">作废请求，携带报告单号、作废时间与作废原因。</param>
  /// <returns>作废成功或已是同一作废事实时返回 <see langword="true"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告单号或作废原因缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">可信归属不可解析、报告不存在、作废时间越界或作废信息冲突时抛出。</exception>
  public async Task<bool> VoidExaminationReportAsync(MedicalReportVoidRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    (OrganizationPath path, DateTime receivedTime) = await ResolveReportScopeAsync();

    VoidExaminationReportCommand command = new()
    {
      ReportNo = request.ReportNo,
      VoidedTime = request.VoidedTime,
      VoidReason = request.VoidReason,
      OrganizationCode = path.OrganizationCode,
      HospitalCode = path.HospitalCode,
      BranchCode = path.BranchCode,
      OperId = ResolveOperatorId(),
      OperTime = DateTimeOffset.UtcNow,
      ReceivedTime = receivedTime
    };
    return await manager.VoidExaminationReportAsync(command);
  }

  /// <summary>
  /// 按报告标识与报告版本标识读取该版本的 PDF 文件流与该版本保存的下载名。
  /// </summary>
  /// <remarks>
  /// 归属校验复用三层可信范围：报告的组织、医院、院区必须与可信上下文解析出的路径一致，跨医院或跨院区定位不可得；
  /// 已作废报告的历史版本仍可下载，作废不改变文件键与下载名。
  /// </remarks>
  /// <param name="request">下载定位请求，携带报告标识与报告版本标识。</param>
  /// <returns>该版本的只读文件流与下载名；调用方负责释放文件流。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告标识或报告版本标识为空 Guid 时抛出。</exception>
  /// <exception cref="InvalidOperationException">可信归属不可解析、版本不存在或不属于该报告、报告越出可信范围或文件缺失时抛出。</exception>
  public async Task<ReportPdfDownload> OpenReportVersionPdfAsync(MedicalReportVersionPdfQueryRequest request)
  {
    MedicalRecognitionRequestValidator.Validate(request);
    (OrganizationPath path, DateTime _) = await ResolveReportScopeAsync();

    IMedicalRecognitionReportQueryRepository queryRepository = queryReportRepository
      ?? throw new InvalidOperationException("无法读取报告版本文件信息。");
    MedicalReportVersionFileItem file = await queryRepository.GetMedicalReportVersionFileAsync(request.ReportId, request.ReportVersionId)
      ?? throw new InvalidOperationException("业务拒绝：报告版本不存在或不属于该报告。");

    bool inTrustedScope = string.Equals(file.OrganizationCode, path.OrganizationCode, StringComparison.Ordinal)
      && string.Equals(file.HospitalCode, path.HospitalCode, StringComparison.Ordinal)
      && string.Equals(file.BranchCode, path.BranchCode, StringComparison.Ordinal);
    if (!inTrustedScope) throw new InvalidOperationException("业务拒绝：报告不在当前可信组织、医院与院区范围内。");

    Stream content = await reportPdfFileStore.OpenReadAsync(file.PdfFileId);
    return new ReportPdfDownload(content, file.PdfFileName);
  }

  /// <summary>
  /// 按契约声明的字段名反序列化完整报告文档。
  /// </summary>
  /// <typeparam name="TDocument">报告文档类型，检验或检查各一个。</typeparam>
  /// <param name="reportJson">报告 JSON 文档原文。</param>
  /// <returns>反序列化后的报告文档。</returns>
  /// <exception cref="InvalidOperationException">文档不是合法 JSON、含未知字段或缺少必填结构时抛出。</exception>
  private static TDocument DeserializeReportDocument<TDocument>(string reportJson)
  {
    try
    {
      return JsonSerializer.Deserialize<TDocument>(reportJson, ReportDocumentJsonOptions)
        ?? throw new InvalidOperationException("业务拒绝：报告文档不能为空。");
    }
    catch (JsonException exception)
    {
      // 文档非法或含未知字段时按业务拒绝表达，不向调用方暴露序列化细节与字段路径。
      throw new InvalidOperationException("业务拒绝：报告文档不是合法的报告结构。", exception);
    }
  }

  /// <summary>
  /// 确认本次提交的 PDF 文件键在存储中可读。
  /// </summary>
  /// <remarks>
  /// 直接调用公开提交入口时文件键由调用方给出，因此必须在这里确认可读；
  /// 文件键指向不存在或不可读的文件时拒绝，且不产生版本与业务数据，既有报告与文件不受影响。
  /// </remarks>
  /// <param name="pdfFileId">PDF 文件键。</param>
  /// <returns>文件可读时完成的异步操作。</returns>
  /// <exception cref="InvalidOperationException">存储中找不到该文件时抛出。</exception>
  private async Task EnsurePdfFileReadableAsync(string pdfFileId)
  {
    await using Stream content = await reportPdfFileStore.OpenReadAsync(pdfFileId);
  }

  /// <summary>
  /// 从登录上下文解析操作人标识。
  /// </summary>
  /// <returns>操作人标识。</returns>
  /// <exception cref="InvalidOperationException">用户标识不能解析为非空 Guid 时抛出。</exception>
  private Guid ResolveOperatorId()
  {
    if (!Guid.TryParse(HttpRequestInfo?.UserId, out Guid userId) || userId == Guid.Empty) throw new InvalidOperationException("无法确定有效的操作人。");
    return userId;
  }
}
