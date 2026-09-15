namespace Dy.MedicalRecognition.Domain.Share.Enums;

/// <summary>
/// 互认使用统计的分组维度。
/// </summary>
public enum RecognitionStatisticsGroupDimension
{
  /// <summary>
  /// 按医院分组，数值 1；取自业务归属范围内的医院。
  /// </summary>
  Hospital = 1,
  /// <summary>
  /// 按院区分组，数值 2；院区隶属于该分组所用的医院。
  /// </summary>
  Branch = 2,
  /// <summary>
  /// 按互认科室分组，数值 3；取自处理结果保存的科室。
  /// </summary>
  RecognitionDepartment = 3,
  /// <summary>
  /// 按标准项目分组，数值 4；取自匹配命中的标准项目。
  /// </summary>
  StandardItem = 4,
}
