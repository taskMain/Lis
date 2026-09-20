using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.Reports;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;

namespace Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate;

/// <summary>
/// 报告采集与生命周期的写入口契约。
/// </summary>
/// <remarks>
/// 主体是四个医院接入能力：两个完整报告提交入口与两个作废入口，四个接口成功统一返回 <see langword="true"/>，
/// 失败以业务异常表达。两个提交入口同时是事务边界；医院接入契约是宿主控制器的 multipart 动作，
/// 控制器完成 PDF 校验与落盘后才调用本入口。
/// </remarks>
public partial interface IMedicalRecognitionReportAppService
{
  /// <summary>
  /// 按患者身份与本次来源就诊请求可用的互认匹配，非空匹配时形成匹配记录与匹配项。
  /// </summary>
  /// <remarks>
  /// 接收组织、医院与院区取自可信上下文，操作人取自登录上下文；本院报告匹配开关与排除时长按平台级编码读取，
  /// 任一参数不存在、取值非法或参数服务读取失败都整次失败；查询截止点取平台收到本次查询的时间。
  /// 本入口是事务边界：匹配记录与匹配项共同成功或共同失败；没有任何报告命中时返回成功的空结果，不创建记录。
  /// </remarks>
  /// <param name="request">匹配查询请求，携带患者证件、本次来源就诊与拟开标准项目集合。</param>
  /// <returns>
  /// 匹配响应；没有任何报告命中时返回成功空结果，记录标识与生成时间为空、报告集合为空。
  /// 本方法不返回 <see langword="null"/>，入参不合法、可信身份不可解析、参数异常与业务拒绝都以异常结束。
  /// </returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">患者证件、就诊信息或拟开项目缺失、空串或纯空白，或就诊类型、项目类型不是已登记取值时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织、医院或院区不可解析，操作人不可解析，本院报告匹配开关或排除时长取值缺失、非法或参数服务读取失败，
  /// 任一拟开项目编码不存在、未纳入当前组织互认范围、配置已停用、标准目录层级不可用或与提交的项目类型不一致，
  /// 已解析到的平台患者与本次提交的证件不一致，或匹配记录与匹配项写入影响的行数不为 1 时抛出；
  /// 以上情况都不创建匹配记录、不登记事件。
  /// </exception>
  Task<RecognitionMatchesResponseReadModel> RequestRecognitionMatchesAsync(RecognitionMatchQueryRequest request);
  /// <summary>
  /// 提交一份完整检验报告。
  /// </summary>
  /// <remarks>
  /// 组织、医院、院区取自可信上下文，操作人取自登录上下文；报告文档由本入口按检验文档反序列化并交领域管理器写入。
  /// 本入口是事务边界：全部写入共同成功或共同失败；文件落盘在进入本入口之前完成，写库失败由调用方删除本次新文件。
  /// </remarks>
  /// <param name="request">提交请求，携带完整检验报告 JSON 文档与已落盘的 PDF 文件键和下载名。</param>
  /// <returns>提交成功返回 <see langword="true"/>；本方法不返回 <see langword="false"/>，入参不合法、可信身份不可解析与业务拒绝都以异常结束。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出（请求校验先于一切判定，此时不进入领域调用、不写入任何数据）。</exception>
  /// <exception cref="ValidationException">报告文档或 PDF 文件键、下载名缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 报告文档不能反序列化、可信组织或操作人不可解析、PDF 文件键在存储中不可读，
  /// 或领域层判定必填与成对字段不成立、就诊类型非法、业务时间逆序、明细集合为空、明细唯一性重复、
  /// 报告已作废、患者身份冲突与并发唯一约束冲突时抛出；此时不保存任何内容、不登记事件。
  /// </exception>
  Task<bool> SubmitCompleteLaboratoryReportAsync(CompleteReportSubmissionRequest request);

  /// <summary>
  /// 提交一份完整检查报告。
  /// </summary>
  /// <remarks>事务边界与失败语义与检验报告提交一致，报告文档按检查文档反序列化。</remarks>
  /// <param name="request">提交请求，携带完整检查报告 JSON 文档与已落盘的 PDF 文件键和下载名。</param>
  /// <returns>提交成功返回 <see langword="true"/>；本方法不返回 <see langword="false"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告文档或 PDF 文件键、下载名缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">与检验报告提交同一口径；此时不保存任何内容、不登记事件。</exception>
  Task<bool> SubmitCompleteExaminationReportAsync(CompleteReportSubmissionRequest request);

  /// <summary>
  /// 作废一份检验报告。
  /// </summary>
  /// <remarks>
  /// 组织、医院、院区取自可信上下文，报告类型由本入口确定；本次请求的接收时间由服务端取，
  /// 用于校验作废时间不晚于平台接收时点。作废只改变生命周期状态、作废时间与作废原因。
  /// </remarks>
  /// <param name="request">作废请求，携带报告单号、作废时间与作废原因。</param>
  /// <returns>作废成功或已是同一作废事实时返回 <see langword="true"/>；本方法不返回 <see langword="false"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告单号或作废原因缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织不可解析、操作人不可解析、报告不存在、作废时间早于当前版本平台接收时间或晚于本次请求接收时间，
  /// 或已作废报告的作废信息与本次不一致时抛出；此时不保存、不登记事件。
  /// </exception>
  Task<bool> VoidLaboratoryReportAsync(MedicalReportVoidRequest request);

  /// <summary>
  /// 作废一份检查报告。
  /// </summary>
  /// <param name="request">作废请求，携带报告单号、作废时间与作废原因。</param>
  /// <returns>作废成功或已是同一作废事实时返回 <see langword="true"/>；本方法不返回 <see langword="false"/>。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告单号或作废原因缺失、空串或纯空白时抛出。</exception>
  /// <exception cref="InvalidOperationException">与检验报告作废同一口径；此时不保存、不登记事件。</exception>
  Task<bool> VoidExaminationReportAsync(MedicalReportVoidRequest request);

  /// <summary>
  /// 按报告标识与报告版本标识读取该版本的 PDF 文件流与该版本保存的下载名。
  /// </summary>
  /// <remarks>
  /// 供宿主下载动作使用：报告标识用于归属定位、版本标识用于定位版本，两者必须属于同一报告；
  /// 该版本的文件键只在应用层到宿主内部流转，不进入对外契约，也不返回物理路径或对外下载地址。
  /// 已作废报告的历史版本仍可下载。
  /// </remarks>
  /// <param name="request">下载定位请求，携带报告标识与报告版本标识。</param>
  /// <returns>该版本的只读文件流与下载名；调用方负责释放文件流。</returns>
  /// <exception cref="ArgumentNullException">请求为 null 时抛出。</exception>
  /// <exception cref="ValidationException">报告标识或报告版本标识为空 Guid 时抛出。</exception>
  /// <exception cref="InvalidOperationException">
  /// 可信组织不可解析、版本不存在或不属于该报告、报告不在可信组织与医院范围内，或存储中找不到文件时抛出。
  /// </exception>
  Task<ReportPdfDownload> OpenReportVersionPdfAsync(MedicalReportVersionPdfQueryRequest request);
}
