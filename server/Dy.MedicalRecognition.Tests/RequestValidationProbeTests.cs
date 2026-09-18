using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Application.Contracts.Queries.StandardCatalog;
using Dy.MedicalRecognition.Application.Contracts.Validation;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 写请求与查询请求的声明式校验契约（矩阵 V48 与 C42/C57 的输入边界）。
/// </summary>
/// <remarks>直接调用 <see cref="MedicalRecognitionRequestValidator"/>，不经过 HTTP，也不写入任何数据。</remarks>
public class RequestValidationProbeTests
{
  /// <summary>合法的新建分类请求应当通过校验。</summary>
  [Fact]
  public void Valid_create_category_request_passes()
  {
    CreateMedicalStandardCategoryRequest request = new() { ItemType = MedicalItemType.Laboratory, Name = "probe-nocat", Remark = null };
    Assert.Null(Record.Exception(() => MedicalRecognitionRequestValidator.Validate(request)));
  }

  /// <summary>名称为空串、纯空白或未传时均以同一文案拒绝。</summary>
  [Theory]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData(null)]
  public void Missing_or_blank_name_is_rejected(string? name)
  {
    CreateMedicalStandardCategoryRequest request = new() { ItemType = MedicalItemType.Laboratory, Name = name!, Remark = null };
    ValidationException error = Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(request));
    Assert.Contains("参数校验失败：名称不能为空。", error.Message);
  }

  /// <summary>超过 200 字符的名称按长度规则拒绝，而不是按必填规则。</summary>
  [Fact]
  public void Overlong_name_is_rejected_by_length()
  {
    CreateMedicalStandardCategoryRequest request = new() { ItemType = MedicalItemType.Laboratory, Name = new string('N', 201), Remark = null };
    ValidationException error = Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(request));
    Assert.Contains("参数校验失败：名称长度不能超过 200。", error.Message);
  }

  /// <summary>超过 600 字符的备注按长度规则拒绝。</summary>
  [Fact]
  public void Overlong_remark_is_rejected_by_length()
  {
    ChangeMedicalStandardItemRemarkRequest request = new() { Id = Guid.NewGuid(), Remark = new string('R', 601) };
    ValidationException error = Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(request));
    Assert.Contains("参数校验失败：备注长度不能超过 600。", error.Message);
  }

  /// <summary>未定义的项目类型枚举值拒绝。</summary>
  [Fact]
  public void Undefined_item_type_is_rejected()
  {
    CreateMedicalStandardCategoryRequest request = new() { ItemType = (MedicalItemType)7, Name = "probe-enum", Remark = null };
    ValidationException error = Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(request));
    Assert.Contains("参数校验失败：项目类型无效。", error.Message);
  }

  /// <summary>启停请求的空 Guid 标识拒绝。</summary>
  [Fact]
  public void Empty_id_on_enable_request_is_rejected()
  {
    EnableMedicalStandardCategoryRequest request = new() { Id = Guid.Empty };
    ValidationException error = Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(request));
    Assert.Contains("参数校验失败：分类ID不能为空。", error.Message);
    MedicalRecognitionRequestValidator.Validate(new EnableMedicalStandardCategoryRequest { Id = Guid.NewGuid() });
  }

  /// <summary>查询条件的空串文本拒绝，null 表示不过滤。</summary>
  [Fact]
  public void Query_empty_code_is_rejected_but_null_passes()
  {
    ValidationException error = Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(new MedicalStandardItemListQueryRequest { Code = "" }));
    Assert.Contains("参数校验失败：编码不能是空白。", error.Message);
    Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(new MedicalStandardItemListQueryRequest { Name = "   " }));
    MedicalRecognitionRequestValidator.Validate(new MedicalStandardItemListQueryRequest { Code = null, Name = null, IsValid = null });
  }

  /// <summary>查询条件的空 Guid 拒绝，null 表示不过滤。</summary>
  [Fact]
  public void Query_empty_guid_is_rejected_but_null_passes()
  {
    ValidationException error = Assert.Throws<ValidationException>(() => MedicalRecognitionRequestValidator.Validate(new MedicalStandardItemListQueryRequest { CategoryId = Guid.Empty }));
    Assert.Contains("参数校验失败：分类ID不能为空Guid。", error.Message);
    MedicalRecognitionRequestValidator.Validate(new MedicalStandardItemListQueryRequest { CategoryId = Guid.NewGuid(), GroupId = null });
  }

  /// <summary>非空特性对 Guid 与字符串的语义。</summary>
  [Fact]
  public void NonEmpty_attribute_semantics()
  {
    NonEmptyAttribute attribute = new();
    Assert.True(attribute.IsValid(null));
    Assert.True(attribute.IsValid(Guid.NewGuid()));
    Assert.False(attribute.IsValid(Guid.Empty));
    Assert.True(attribute.IsValid("abc"));
    Assert.False(attribute.IsValid(""));
    Assert.False(attribute.IsValid("   "));
  }
}
