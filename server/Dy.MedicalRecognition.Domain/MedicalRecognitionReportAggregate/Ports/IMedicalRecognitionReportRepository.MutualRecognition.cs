using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

namespace Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Ports;

/// <summary>
/// 互认项目配置的读写契约。
/// </summary>
/// <remarks>只做数据读写并返回行数或查询结果，不判断业务状态。</remarks>
public partial interface IMedicalRecognitionReportRepository
{
  /// <summary>
  /// 插入一条互认项目配置记录。
  /// </summary>
  /// <remarks>同一可信组织与同一标准项目只能存在一条配置，并发重复写入由组织与标准项目唯一约束拒绝，数据库异常按原样向外传播。</remarks>
  /// <param name="mutualRecognitionItem">待插入的互认项目配置实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> CreateMutualRecognitionItemAsync(MutualRecognitionItem mutualRecognitionItem);

  /// <summary>
  /// 在指定组织范围内按配置标识读取互认项目配置。
  /// </summary>
  /// <remarks>供修改、启用与停用前校验配置归属；其他组织的同标识配置不会被返回，不存在或越组织统一返回 <see langword="null"/>。</remarks>
  /// <param name="id">配置标识。</param>
  /// <param name="organizationCode">可信组织编码。</param>
  /// <returns>互认项目配置实体。</returns>
  Task<MutualRecognitionItem?> GetMutualRecognitionItemByIdAsync(Guid id, string organizationCode);

  /// <summary>
  /// 按配置标识与可信组织更新可互认时间天数；不改变组织编码、标准项目与启用状态。
  /// </summary>
  /// <param name="mutualRecognitionItem">携带配置标识、可信组织、本次天数与操作字段的实体。</param>
  /// <returns>受影响行数。</returns>
  Task<int> UpdateMutualRecognitionItemConfigurationAsync(MutualRecognitionItem mutualRecognitionItem);

  /// <summary>
  /// 将指定互认项目配置由停用改为启用；仅当前为停用态且属于可信组织时才实际更新。
  /// </summary>
  /// <param name="enableMutualRecognitionItemCommand">启用命令，携带配置标识、可信组织与操作字段。</param>
  /// <returns>受影响行数。</returns>
  Task<int> EnableMutualRecognitionItemAsync(EnableMutualRecognitionItemCommand enableMutualRecognitionItemCommand);

  /// <summary>
  /// 将指定互认项目配置由启用改为停用；仅当前为启用态且属于可信组织时才实际更新。
  /// </summary>
  /// <param name="disableMutualRecognitionItemCommand">停用命令，携带配置标识、可信组织与操作字段。</param>
  /// <returns>受影响行数。</returns>
  Task<int> DisableMutualRecognitionItemAsync(DisableMutualRecognitionItemCommand disableMutualRecognitionItemCommand);

  /// <summary>
  /// 按组织编码与标准项目编码读取互认项目配置。
  /// </summary>
  /// <remarks>
  /// 两处使用：新增配置写入前判断该组织是否已配置该标准项目（重复配置拒绝），以及金额保存前判断"当前组织已建立该标准项目的互认配置"这一保存前提；
  /// 启用中与已停用的配置都会被返回，是否有效不参与这两个判断；未建立时返回 <see langword="null"/>。
  /// </remarks>
  /// <param name="organizationCode">组织编码。</param>
  /// <param name="standardProjectCode">标准项目编码。</param>
  /// <returns>互认项目配置实体。</returns>
  Task<MutualRecognitionItem?> GetMutualRecognitionItemByOrganizationAndProjectAsync(string organizationCode, string standardProjectCode);
}
