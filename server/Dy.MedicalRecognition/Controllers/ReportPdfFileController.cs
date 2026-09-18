using System.Text.Json;
using Dy.Core.Abstractions.Http;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Dy.MedicalRecognition.Controllers;

/// <summary>
/// 报告 PDF 上传与下载的手工控制器。
/// </summary>
/// <remarks>
/// 只做 multipart 绑定、PDF 校验、文件落盘与失败补偿，以及下载定位与文件流响应：
/// 业务规则、写入顺序与事务边界都在公开应用服务入口上，控制器不承载事务，也不做领域判断与 SQL。
/// 路由带版本前缀，避开框架按接口名自动推导的端点前缀，因此不与既有自动端点重复或遮蔽。
/// 响应统一走框架既有形态：上传统一返回布尔真，下载返回 PDF 文件流。
/// </remarks>
[ApiController]
[Route("api/v1/report-pdf")]
public sealed class ReportPdfFileController : ControllerBase
{
  /// <summary>
  /// 允许上传的 PDF 文件内容类型；调用方声明的类型必须与文件扩展名一致。
  /// </summary>
  private const string PdfContentType = "application/pdf";

  /// <summary>
  /// 允许上传的 PDF 文件扩展名，含前导点。
  /// </summary>
  private const string PdfExtension = ".pdf";

  /// <summary>
  /// PDF 文件头的固定签名；不以该签名开头的文件一律拒绝。
  /// </summary>
  private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

  /// <summary>
  /// 报告提交与下载的应用入口。
  /// </summary>
  private readonly IMedicalRecognitionReportAppService appService;

  /// <summary>
  /// 报告 PDF 的本地文件存储端口，用于落盘与失败补偿。
  /// </summary>
  private readonly IReportPdfFileStore reportPdfFileStore;

  /// <summary>
  /// 报告 PDF 存储配置，提供单文件上限与根目录校验。
  /// </summary>
  private readonly Repository.MedicalRecognitionReportAggregate.ReportPdfFileOptions reportPdfFileOptions;

  /// <summary>
  /// 控制器日志，用于记录文件补偿删除失败这类不改变对外结果的残留事实。
  /// </summary>
  private readonly ILogger<ReportPdfFileController> logger;

  /// <summary>
  /// 初始化控制器。
  /// </summary>
  /// <param name="appService">报告提交与下载的应用入口。</param>
  /// <param name="reportPdfFileStore">报告 PDF 的本地文件存储端口。</param>
  /// <param name="reportPdfFileOptions">报告 PDF 存储配置。</param>
  /// <param name="logger">控制器日志。</param>
  public ReportPdfFileController(
    IMedicalRecognitionReportAppService appService,
    IReportPdfFileStore reportPdfFileStore,
    Repository.MedicalRecognitionReportAggregate.ReportPdfFileOptions reportPdfFileOptions,
    ILogger<ReportPdfFileController> logger)
  {
    this.appService = appService;
    this.reportPdfFileStore = reportPdfFileStore;
    this.reportPdfFileOptions = reportPdfFileOptions;
    this.logger = logger;
  }

  /// <summary>
  /// 提交一份完整检验报告：接收完整报告 JSON 文档与 PDF 文件流。
  /// </summary>
  /// <remarks>
  /// 事务边界声明在本动作上：框架的端点过滤器按**路由端点元数据**取事务声明，控制器动作就是本用例的端点，
  /// 应用服务方法上的同名声明在该调用路径上不被读取，因此这里必须声明，否则写入不会回滚。
  /// 校验通过后先落盘取文件键、再调用应用服务提交入口；写库失败时删除本次新文件并原样抛出原始异常，
  /// 删除文件失败只记录日志、不改变对外结果与原始异常。
  /// </remarks>
  /// <param name="form">multipart 表单，部件为 reportJson 与 pdfFile。</param>
  /// <returns>提交成功返回布尔真。</returns>
  [HttpPost("laboratory-report")]
  [Consumes("multipart/form-data")]
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> SubmitLaboratoryReportAsync([FromForm] ReportSubmissionForm form)
  {
    return await SubmitReportAsync(form, isLaboratory: true);
  }

  /// <summary>
  /// 提交一份完整检查报告：接收完整报告 JSON 文档与 PDF 文件流。
  /// </summary>
  /// <remarks>事务边界与失败补偿口径同检验报告提交动作。</remarks>
  /// <param name="form">multipart 表单，部件为 reportJson 与 pdfFile。</param>
  /// <returns>提交成功返回布尔真。</returns>
  [HttpPost("examination-report")]
  [Consumes("multipart/form-data")]
  [WorkUnit(UseTransaction = true)]
  public async Task<bool> SubmitExaminationReportAsync([FromForm] ReportSubmissionForm form)
  {
    return await SubmitReportAsync(form, isLaboratory: false);
  }

  /// <summary>
  /// 下载某个报告版本的原始 PDF 文件流。
  /// </summary>
  /// <remarks>
  /// 归属校验、版本定位与文件读取都在应用层完成；这里只设置内容类型、下载名与禁止缓存响应头，
  /// 不返回文件键、物理路径或对外下载地址。已作废报告的历史版本仍可下载。
  /// </remarks>
  /// <param name="reportId">报告标识，用于归属定位。</param>
  /// <param name="reportVersionId">报告版本标识，用于定位版本。</param>
  /// <returns>PDF 文件流。</returns>
  [HttpGet("{reportId:guid}/versions/{reportVersionId:guid}/pdf")]
  public async Task<IActionResult> DownloadReportVersionPdfAsync(Guid reportId, Guid reportVersionId)
  {
    ReportPdfDownload download = await appService.OpenReportVersionPdfAsync(new MedicalReportVersionPdfQueryRequest
    {
      ReportId = reportId,
      ReportVersionId = reportVersionId
    });

    // 禁止缓存：历史版本文件可能在后续阶段被清理策略影响，响应不得被中间层或浏览器缓存。
    Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    return File(download.Content, PdfContentType, download.FileName);
  }

  /// <summary>
  /// 执行一次完整报告提交：校验部件、解析回退下载名、落盘、调用应用服务并在写库失败时补偿。
  /// </summary>
  /// <param name="form">multipart 表单。</param>
  /// <param name="isLaboratory">本次提交是否为检验报告。</param>
  /// <returns>提交成功返回布尔真。</returns>
  /// <exception cref="InvalidOperationException">
  /// 部件缺失、文件本身校验失败（内容类型、扩展名、文件头签名、空长度）或超过单文件上限时抛出；
  /// 此时不进入业务写入、不落盘，既有报告与文件不受影响。
  /// </exception>
  private async Task<bool> SubmitReportAsync(ReportSubmissionForm form, bool isLaboratory)
  {
    if (form is null) throw new InvalidOperationException("业务拒绝：提交表单不能为空。");
    if (string.IsNullOrWhiteSpace(form.ReportJson)) throw new InvalidOperationException("业务拒绝：报告文档部件不能为空。");
    if (form.PdfFile is null) throw new InvalidOperationException("业务拒绝：PDF 文件部件不能为空。");

    // 文件本身校验先于读取文件流：非 PDF、内容类型或扩展名不符、文件为空、超过单文件上限都在这里拒绝。
    ValidatePdfFile(form.PdfFile);

    // 报告单号用于文件名回退，因此先解析文档；文档非法时同样不落盘、不进入业务写入。
    string reportNo = ReadReportNo(form.ReportJson, isLaboratory);
    string downloadFileName = ResolveDownloadFileName(form.PdfFile.FileName, reportNo);

    string fileKey;
    await using (Stream content = form.PdfFile.OpenReadStream())
    {
      fileKey = await reportPdfFileStore.SaveAsync(content);
    }

    try
    {
      CompleteReportSubmissionRequest request = new()
      {
        ReportJson = form.ReportJson,
        PdfFileId = fileKey,
        PdfFileName = downloadFileName
      };
      return isLaboratory
        ? await appService.SubmitCompleteLaboratoryReportAsync(request)
        : await appService.SubmitCompleteExaminationReportAsync(request);
    }
    catch
    {
      // 写库失败时删除本次新文件并原样抛出原始异常；删除失败只记录日志，不改变对外结果与原始异常。
      await DeleteNewFileAsync(fileKey);
      throw;
    }
  }

  /// <summary>
  /// 校验 PDF 文件的长度、单文件上限、内容类型、扩展名与文件头签名。
  /// </summary>
  /// <remarks>
  /// 单文件上限取自配置节 <c>ReportPdfFiles:MaxFileBytes</c>，不在代码中硬编码；
  /// 校验在读取文件流之前完成，因此超限请求不会被读入内存。
  /// </remarks>
  /// <param name="file">待校验的文件部件。</param>
  /// <exception cref="InvalidOperationException">任一项不成立时抛出。</exception>
  private void ValidatePdfFile(IFormFile file)
  {
    if (file.Length <= 0) throw new InvalidOperationException("业务拒绝：PDF 文件不能为空。");
    if (file.Length > reportPdfFileOptions.MaxFileBytes) throw new InvalidOperationException("业务拒绝：PDF 文件超过单文件上限。");

    string fileName = file.FileName ?? string.Empty;
    if (!string.Equals(file.ContentType, PdfContentType, StringComparison.OrdinalIgnoreCase))
      throw new InvalidOperationException("业务拒绝：PDF 文件的内容类型不是 application/pdf。");
    if (!string.Equals(Path.GetExtension(fileName), PdfExtension, StringComparison.OrdinalIgnoreCase))
      throw new InvalidOperationException("业务拒绝：PDF 文件的扩展名不是 .pdf。");

    Span<byte> signature = stackalloc byte[PdfSignature.Length];
    using Stream stream = file.OpenReadStream();
    int read = stream.Read(signature);
    if (read != PdfSignature.Length || !signature.SequenceEqual(PdfSignature))
      throw new InvalidOperationException("业务拒绝：PDF 文件头签名无效。");
  }

  /// <summary>
  /// 从报告文档中读取来源报告单号，供下载名回退使用。
  /// </summary>
  /// <param name="reportJson">报告 JSON 文档原文。</param>
  /// <param name="isLaboratory">本次提交是否为检验报告。</param>
  /// <returns>来源报告单号；文档缺失该字段时返回空串。</returns>
  /// <exception cref="InvalidOperationException">文档不是合法 JSON 时抛出。</exception>
  private static string ReadReportNo(string reportJson, bool isLaboratory)
  {
    try
    {
      using JsonDocument document = JsonDocument.Parse(reportJson);
      foreach (JsonProperty property in document.RootElement.EnumerateObject())
      {
        // 契约声明的字段名为 camelCase；比较不区分大小写，使调用方提交 PascalCase 时同样可识别。
        if (string.Equals(property.Name, "reportNo", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
          return property.Value.GetString() ?? string.Empty;
      }

      return string.Empty;
    }
    catch (JsonException exception)
    {
      throw new InvalidOperationException($"业务拒绝：{(isLaboratory ? "检验" : "检查")}报告文档不是合法 JSON。", exception);
    }
  }

  /// <summary>
  /// 解析该版本的下载名：文件名安全时按文件名本身保存，缺失或不安全时回退为「报告单号加扩展名」。
  /// </summary>
  /// <remarks>
  /// 文件名只保留名称本身，不保留客户端目录；文件名问题不拒绝报告。
  /// multipart 头部可能把控制字符按百分号编码后传到这里（实测文件名 <c>bad/na\rme\n.pdf</c> 到达时为 <c>na%0Dme%0A.pdf</c>），
  /// 因此除字面控制字符外还要拒绝百分号编码形式，否则不安全名字会被当作安全名保存。
  /// </remarks>
  /// <param name="uploadedFileName">上传时携带的文件名。</param>
  /// <param name="reportNo">来源报告单号。</param>
  /// <returns>该版本的下载名。</returns>
  private static string ResolveDownloadFileName(string? uploadedFileName, string reportNo)
  {
    string fileName = Path.GetFileName(uploadedFileName?.Trim() ?? string.Empty);

    // 含字面控制字符、路径分隔符残留或百分号编码的控制字符时视为不安全，回退到按报告单号生成的名字。
    bool unsafeName = fileName.Length == 0
      || fileName.Contains('%')
      || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
      || fileName.IndexOfAny(['\r', '\n', '\t', '/', '\\']) >= 0;
    if (!unsafeName) return fileName;

    // 回退名同样要过滤：报告单号来自请求，不能让它把不可用字符带进下载名。
    string fallbackReportNo = SanitizeReportNoForFileName(reportNo);
    return fallbackReportNo.Length == 0 ? $"report{PdfExtension}" : $"{fallbackReportNo}{PdfExtension}";
  }

  /// <summary>
  /// 过滤报告单号中不能进入文件名的字符，供回退下载名使用。
  /// </summary>
  /// <param name="reportNo">来源报告单号。</param>
  /// <returns>只保留可用字符的报告单号；全部被过滤时返回空串。</returns>
  private static string SanitizeReportNoForFileName(string reportNo)
  {
    char[] invalid = [.. Path.GetInvalidFileNameChars(), '\r', '\n', '\t', '/', '\\', '%'];
    return new string([.. reportNo.Trim().Where(character => !invalid.Contains(character))]);
  }

  /// <summary>
  /// 删除本次新落盘的文件，用于写库失败后的补偿。
  /// </summary>
  /// <remarks>删除失败只记录日志，不改变对外结果与原始异常。</remarks>
  /// <param name="fileKey">本次新文件的文件键。</param>
  private async Task DeleteNewFileAsync(string fileKey)
  {
    try
    {
      await reportPdfFileStore.DeleteAsync(fileKey);
    }
    catch (Exception exception)
    {
      // 补偿失败不掩盖原始业务失败：残留文件按已接受风险登记，由运维按存储目录与记录一致性核对处理。
      logger.LogWarning(exception, "报告 PDF 文件补偿删除失败，文件键：{FileKey}", fileKey);
    }
  }
}
