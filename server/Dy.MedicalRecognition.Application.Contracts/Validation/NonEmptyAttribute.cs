using System.ComponentModel.DataAnnotations;

namespace Dy.MedicalRecognition.Application.Contracts.Validation;

/// <summary>
/// 声明入参在传入时不得为空值：<see cref="Guid"/> 不允许 <see cref="Guid.Empty"/>，字符串不允许空串或纯空白。
/// </summary>
/// <remarks>
/// 未传（<see langword="null"/>）表示可选条件不生效，按通过处理。字符串的空白判定使用
/// <see cref="string.IsNullOrWhiteSpace(string?)"/>，与 <see cref="RequiredAttribute"/> 对纯空白串的拒绝口径一致。
/// 特性由 <see cref="MedicalRecognitionRequestValidator"/> 显式触发，反序列化与参数绑定都不会自动执行。
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class NonEmptyAttribute : ValidationAttribute
{
  /// <summary>
  /// 初始化校验特性，并使用统一的参数校验失败兜底文案；调用方应逐属性指定更具体的消息。
  /// </summary>
  public NonEmptyAttribute()
  {
    ErrorMessage = "参数校验失败：值不能为空。";
  }

  /// <summary>
  /// 判断传入值是否为允许的值。
  /// </summary>
  /// <param name="value">待校验的值；未传时为 <see langword="null"/>。</param>
  /// <returns>未传、非空 <see cref="Guid"/> 或含有效字符的字符串返回 <see langword="true"/>。</returns>
  public override bool IsValid(object? value) => value switch
  {
    null => true,
    Guid id => id != Guid.Empty,
    string text => !string.IsNullOrWhiteSpace(text),
    _ => false
  };
}
