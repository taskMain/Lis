using System.Reflection;
using Dy.MedicalRecognition.Application.Contracts.Queries;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 校验标准目录写入口的架构约束：事务声明的范围、公共请求校验的执行顺序、
/// 业务代码不自行控制事务，以及读写契约与实现的分层分离。
/// </summary>
public sealed class Stage1ArchitectureTests
{
  /// <summary>
  /// 校验阶段 1 的写入口只按业务原子边界声明事务：仅"先校验父级启用再写入"的
  /// 创建分组与创建标准项目需要事务，其余单条原子写语句不声明 <c>WorkUnitAttribute</c>。
  /// </summary>
  [Fact]
  public void Write_entrypoints_declare_only_the_transactional_work_units()
  {
    Type appService = typeof(Dy.MedicalRecognition.Application.MedicalRecognitionReportAggregate.MedicalRecognitionReportAppService);
    string[] transactional = ["CreateMedicalStandardGroupAsync", "CreateMedicalStandardItemAsync"];
    string[] plain = Array.FindAll(Stage1WriteEntrypoints, name => !transactional.Contains(name));

    foreach (string name in transactional)
    {
      MethodInfo method = appService.GetMethod(name)!;
      Attribute workUnit = Assert.Single(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
      Assert.True((bool)workUnit.GetType().GetProperty("UseTransaction")!.GetValue(workUnit)!);
    }

    foreach (string name in plain)
    {
      MethodInfo method = appService.GetMethod(name)!;
      Assert.DoesNotContain(method.GetCustomAttributes(), attribute => attribute.GetType().Name == "WorkUnitAttribute");
    }
  }

  /// <summary>
  /// 阶段 1 的 12 个写入口名称，供事务声明与校验入口断言共用。
  /// </summary>
  private static readonly string[] Stage1WriteEntrypoints =
  [
    "CreateMedicalStandardCategoryAsync", "UpdateMedicalStandardCategoryAsync", "EnableMedicalStandardCategoryAsync", "DisableMedicalStandardCategoryAsync",
    "CreateMedicalStandardGroupAsync", "UpdateMedicalStandardGroupAsync", "EnableMedicalStandardGroupAsync", "DisableMedicalStandardGroupAsync",
    "CreateMedicalStandardItemAsync", "ChangeMedicalStandardItemRemarkAsync", "EnableMedicalStandardItemAsync", "DisableMedicalStandardItemAsync"
  ];

  /// <summary>
  /// 阶段 1 的 4 个查询入口名称，供查询侧请求校验断言使用。
  /// </summary>
  private static readonly string[] Stage1QueryEntrypoints =
  [
    "QueryMedicalStandardCategoryListAsync", "QueryMedicalStandardGroupListAsync",
    "QueryMedicalStandardItemListAsync", "QueryEffectiveMedicalStandardCatalogAsync"
  ];

  /// <summary>
  /// 校验阶段 1 的 12 个写入口都在映射领域命令之前执行了公共请求校验，
  /// 避免非法入参绕过 Request 层约束直接进入领域判断。
  /// 该断言读取应用服务源码文本，因为反射无法观察方法体内的调用顺序。
  /// </summary>
  [Fact]
  public void Write_entrypoints_validate_the_public_request_before_mapping()
  {
    string root = FindRepositoryRoot();
    string source = File.ReadAllText(Path.Combine(
      root, "server", "Dy.MedicalRecognition.Application", "MedicalRecognitionReportAggregate", "MedicalRecognitionReportAppService.cs"));

    List<string> violations = [];
    foreach (string name in Stage1WriteEntrypoints)
    {
      int start = source.IndexOf($" {name}(", StringComparison.Ordinal);
      if (start < 0)
      {
        violations.Add($"{name}: 未在应用服务源码中找到方法");
        continue;
      }

      int nextMethod = source.IndexOf("\n  public ", start + 1, StringComparison.Ordinal);
      string body = nextMethod < 0 ? source[start..] : source[start..nextMethod];
      int validateIndex = body.IndexOf("MedicalRecognitionRequestValidator.Validate(request)", StringComparison.Ordinal);
      int mapIndex = body.IndexOf(".MapTo", StringComparison.Ordinal);

      if (validateIndex < 0)
      {
        violations.Add($"{name}: 缺少公共请求校验调用");
      }
      else if (mapIndex >= 0 && validateIndex > mapIndex)
      {
        violations.Add($"{name}: 公共请求校验晚于命令映射");
      }
    }

    Assert.Empty(violations);
  }

  /// <summary>
  /// 校验阶段 1 的 4 个查询入口都先执行公共请求校验，避免空 Guid、空串或纯空白、未定义枚举等筛选条件
  /// 被当作“不过滤”处理并返回超出调用方预期的数据。该断言读取查询应用服务源码文本，因为反射无法观察方法体内的调用。
  /// </summary>
  [Fact]
  public void Query_entrypoints_validate_the_public_request()
  {
    string root = FindRepositoryRoot();
    string source = File.ReadAllText(Path.Combine(
      root, "server", "Dy.MedicalRecognition.Application", "Queries", "MedicalRecognitionReportQueryAppService.cs"));

    List<string> violations = [];
    foreach (string name in Stage1QueryEntrypoints)
    {
      int start = source.IndexOf($" {name}(", StringComparison.Ordinal);
      if (start < 0)
      {
        violations.Add($"{name}: 未在查询应用服务源码中找到方法");
        continue;
      }

      int nextMethod = source.IndexOf("\n  public ", start + 1, StringComparison.Ordinal);
      string body = nextMethod < 0 ? source[start..] : source[start..nextMethod];
      if (!body.Contains("MedicalRecognitionRequestValidator.Validate(request)", StringComparison.Ordinal))
      {
        violations.Add($"{name}: 缺少公共请求校验调用");
      }
    }

    Assert.Empty(violations);
  }

  /// <summary>
  /// 事务生命周期只能由框架的工作单元机制管理，业务代码不得出现手工事务 API。
  /// 只扫描生产工程：测试工程本身要写出这些关键字才能断言它们被禁止。
  /// </summary>
  [Fact]
  public void Backend_source_does_not_control_transactions_manually()
  {
    string root = FindRepositoryRoot();
    string[] banned = ["TransactionScope", "BeginTransaction", "IDbTransaction", "DbTransaction", "UnitOfWork", "TransactionExecutor"];
    List<string> violations = [];

    foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "server"), "*.cs", SearchOption.AllDirectories))
    {
      string relative = Path.GetRelativePath(root, file);
      if (relative.Contains("Tests", StringComparison.Ordinal) ||
          file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
          file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
          file.EndsWith(".g.cs", StringComparison.Ordinal))
      {
        continue;
      }

      string source = File.ReadAllText(file);
      foreach (string keyword in banned)
      {
        if (source.Contains(keyword, StringComparison.Ordinal))
        {
          violations.Add($"{relative}: {keyword}");
        }
      }
    }

    Assert.Empty(violations);
  }

  /// <summary>
  /// 定位仓库根目录，供需要读取源码文件的架构断言使用。
  /// </summary>
  /// <returns>包含 <c>AGENTS.md</c> 的仓库根目录绝对路径，作为读取生产源码的基准目录。</returns>
  /// <exception cref="InvalidOperationException">从测试程序集所在目录逐级向上都未找到 <c>AGENTS.md</c>，无法确定仓库根目录时抛出。</exception>
  private static string FindRepositoryRoot()
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
    {
      directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("未能定位仓库根目录（未找到 AGENTS.md）。");
  }

  /// <summary>
  /// 校验读写契约分离：查询契约只继承应用服务标记而不含写方法，查询契约与查询仓储实现可相互赋值，
  /// 查询契约的方法数量固定为 4，且写入口契约中不得出现查询方法。
  /// </summary>
  [Fact]
  public void Query_contract_is_separated_from_write_service_and_repository_implementation()
  {
    Assert.Contains(typeof(IMedicalRecognitionReportQueryAppService).GetInterfaces(), type => type.Name == "IApplicationService");
    Assert.True(typeof(IMedicalRecognitionReportQueryAppService).IsAssignableFrom(typeof(MedicalRecognitionReportQueryAppService)));
    Assert.True(typeof(IMedicalRecognitionReportQueryRepository).IsAssignableFrom(typeof(Dy.MedicalRecognition.Repository.Queries.MedicalRecognitionReportQueryRepository)));
    Assert.Equal(4, typeof(IMedicalRecognitionReportQueryAppService).GetMethods().Length);
    Assert.DoesNotContain(typeof(Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.IMedicalRecognitionReportAppService).GetMethods(), method => method.Name.StartsWith("Query", StringComparison.Ordinal));
  }

  /// <summary>
  /// 校验分组改名契约不携带所属分类：修改分组请求与对应领域命令都不含分类属性，从契约层面阻止分组跨分类迁移。
  /// </summary>
  [Fact]
  public void Update_group_contract_cannot_change_parent_category()
  {
    Assert.Null(typeof(Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests.UpdateMedicalStandardGroupRequest).GetProperty("CategoryId"));
    Assert.Null(typeof(Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands.UpdateMedicalStandardGroupCommand).GetProperty("CategoryId"));
  }
}
