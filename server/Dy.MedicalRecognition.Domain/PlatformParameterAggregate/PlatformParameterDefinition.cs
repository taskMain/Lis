namespace Dy.MedicalRecognition.Domain.PlatformParameterAggregate;

/// <summary>
/// 平台参数的编码、名称、取值规则与展示文案的唯一声明位置。
/// </summary>
/// <remarks>
/// 业务读取与维护保存共用本类的取值判定，避免两个入口对同一参数给出不同结论；
/// 参数编码取小写下划线形态，参数值以字符串保存。
/// </remarks>
public static class PlatformParameterDefinition
{
  /// <summary>本院报告匹配开关的参数编码。</summary>
  public const string OwnHospitalMatchSwitch = "own_hospital_match_switch";

  /// <summary>本院报告排除时长的参数编码。</summary>
  public const string OwnHospitalExcludeHours = "own_hospital_exclude_hours";

  /// <summary>引用详情有效时长的参数编码。</summary>
  public const string CitationDetailValidHours = "citation_detail_valid_hours";

  /// <summary>本应用维护的全部参数编码；维护入口只接受这些编码。</summary>
  public static IReadOnlyList<string> AllCodes { get; } = [OwnHospitalMatchSwitch, OwnHospitalExcludeHours, CitationDetailValidHours];

  /// <summary>
  /// 判断参数编码是否属于本应用维护的参数集合。
  /// </summary>
  /// <param name="parameterCode">待判断的参数编码。</param>
  /// <returns>属于集合时为 <see langword="true"/>。</returns>
  public static bool IsKnownCode(string parameterCode) => AllCodes.Contains(parameterCode, StringComparer.Ordinal);

  /// <summary>
  /// 取参数的中文名称，用于业务拒绝文案与维护界面。
  /// </summary>
  /// <param name="parameterCode">已登记的参数编码。</param>
  /// <returns>参数中文名称。</returns>
  /// <exception cref="InvalidOperationException">参数编码未登记时抛出。</exception>
  public static string GetDisplayName(string parameterCode) => parameterCode switch
  {
    OwnHospitalMatchSwitch => "本院报告匹配开关",
    OwnHospitalExcludeHours => "本院报告排除时长",
    CitationDetailValidHours => "引用详情有效时长",
    _ => throw new InvalidOperationException("业务拒绝：参数编码不存在。")
  };

  /// <summary>
  /// 判断参数值是否符合该编码对应的业务规则。
  /// </summary>
  /// <param name="parameterCode">参数编码。</param>
  /// <param name="value">待校验的参数值。</param>
  /// <returns>符合规则时为 <see langword="true"/>；编码未登记或取值不符合时返回 <see langword="false"/>。</returns>
  public static bool IsValueValid(string parameterCode, string? value) => parameterCode switch
  {
    OwnHospitalMatchSwitch => value is "0" or "1",
    OwnHospitalExcludeHours => TryParseNonNegativeHours(value, out _),
    CitationDetailValidHours => TryParsePositiveHours(value, out _),
    _ => false
  };

  /// <summary>
  /// 解析非负整数小时；仅供读取与保存共用同一校验口径。
  /// </summary>
  /// <param name="value">待解析的参数值。</param>
  /// <param name="hours">解析成功时的小时数；失败时为 0。</param>
  /// <returns>取值是非负整数时为 <see langword="true"/>。</returns>
  public static bool TryParseNonNegativeHours(string? value, out int hours) => int.TryParse(value, out hours) && hours >= 0;

  /// <summary>
  /// 解析正整数小时；仅供读取与保存共用同一校验口径。
  /// </summary>
  /// <param name="value">待解析的参数值。</param>
  /// <param name="hours">解析成功时的小时数；失败时为 0。</param>
  /// <returns>取值是正整数时为 <see langword="true"/>。</returns>
  public static bool TryParsePositiveHours(string? value, out int hours) => int.TryParse(value, out hours) && hours > 0;
}
