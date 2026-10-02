using System.ComponentModel.DataAnnotations;
using Dy.MedicalRecognition.Application.Contracts.Validation;

namespace Dy.MedicalRecognition.Application.Contracts.Queries.MutualRecognition;

/// <summary>
/// 可新增标准项目查询条件：返回当前可信组织**尚未建立互认配置**且三级均启用的标准项目目录。
/// </summary>
/// <remarks>
/// 组织不在入参里：与配置列表查询同一口径，组织只取自可信上下文，不接受调用方指定，也不新建授权组织集合。
/// 返回值与当前有效标准目录同形（项目类型 → 分类 → 分组 → 标准项目），不分页；关键字可选。
/// </remarks>
public sealed record SelectableStandardItemListQueryRequest
{
  /// <summary>
  /// 关键字字面包含匹配条件，同时匹配标准项目编码、标准项目名称、所属分类名称与所属分组名称，任一列命中即返回；比较不区分大小写，`%`、`_` 按普通字符处理，不作为通配符。不传表示不按关键字过滤，传入则须为非空白文本。
  /// </summary>
  [NonEmpty(ErrorMessage = "参数校验失败：关键字不能是空白。")]
  public string? Keyword { get; init; }
}
