using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 校验标准目录 SqlMap 与运行时注册的架构约束：新增语句不依赖请求参数写入启用状态、
/// 启停语句保留布尔字面量，并核对运行时注册出的完整语句标识与预期一致。
/// </summary>
public sealed class Stage1SqlMapProbeTests
{
  /// <summary>
  /// 校验三个标准目录的新增语句把启用状态写成常量、不引用请求参数 <c>$IsValid</c>，
  /// 避免调用方通过新增请求决定记录初始是否启用。
  /// </summary>
  [Fact]
  public void Create_statements_default_is_valid_without_request_parameter()
  {
    foreach (var fileName in new[] { "MedicalStandardCategory.xml", "MedicalStandardGroup.xml", "MedicalStandardItem.xml" })
    {
      var document = XDocument.Load(FindRepositorySqlMap(fileName));
      var create = document.Descendants().Single(element => (string?)element.Attribute("Id") == $"Create{Path.GetFileNameWithoutExtension(fileName)}").Value;

      Assert.Contains("is_valid,", create, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("true", create, StringComparison.OrdinalIgnoreCase);
      Assert.DoesNotContain("$IsValid", create, StringComparison.Ordinal);
    }
  }

  /// <summary>
  /// 校验三个标准目录的启用语句写入 <c>true</c>、停用语句写入 <c>false</c>，
  /// 保证启停状态由动作本身决定而不是由入参决定。
  /// </summary>
  [Fact]
  public void Enable_and_disable_statements_keep_boolean_literals()
  {
    foreach (var fileName in new[] { "MedicalStandardCategory.xml", "MedicalStandardGroup.xml", "MedicalStandardItem.xml" })
    {
      var document = XDocument.Load(FindRepositorySqlMap(fileName));
      var statements = document.Descendants()
        .Where(element => element.Attribute("Id") is not null)
        .ToDictionary(element => (string)element.Attribute("Id")!, element => element.Value);

      Assert.Contains("is_valid = true", statements.Single(statement => statement.Key.StartsWith("Enable", StringComparison.Ordinal)).Value, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("is_valid = false", statements.Single(statement => statement.Key.StartsWith("Disable", StringComparison.Ordinal)).Value, StringComparison.OrdinalIgnoreCase);
    }
  }

  /// <summary>
  /// 在不连接数据库的前提下加载宿主输出目录中的程序集并构建 SqlMap 注册表，
  /// 核对注册出的完整语句标识：聚合级标识不应存在，具体实体的新增、更新与查询标识必须齐全。
  /// </summary>
  [Fact]
  public void Runtime_registration_reports_full_sql_ids_without_opening_database()
  {
    var hostDirectory = FindHostOutputDirectory();
    var earthraceAssembly = LoadAssembly(hostDirectory, "Dy.Earthrace.dll");
    var repositoryAssembly = LoadAssembly(hostDirectory, "Dy.MedicalRecognition.Repository.dll");
    var builderType = earthraceAssembly.GetType("Dy.Earthrace.EarthraceBuilder", throwOnError: true)!;
    var resourceType = LoadAssembly(hostDirectory, "Dy.Earthrace.Abstractions.dll")
      .GetType("Dy.Earthrace.Abstractions.ResourceType", throwOnError: true)!;
    var optionsType = earthraceAssembly.GetType("Dy.Earthrace.Options.SqlMapOptions", throwOnError: true)!;

    using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(hostDirectory, "EarthraceConfig.json")));
    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    Flatten(json.RootElement, "", values);
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    var options = Activator.CreateInstance(optionsType);
    configuration.GetSection("SqlMapConfigs:0").Bind(options);
    Assert.NotNull(options);
    var settings = optionsType.GetProperty("Settings")!.GetValue(options)!;
    settings.GetType().GetProperty("ParameterPrefix")!.SetValue(settings, "@");
    var database = optionsType.GetProperty("Database")!.GetValue(options)!;
    var dbProvider = database.GetType().GetProperty("DbProvider")!.GetValue(database)!;
    dbProvider.GetType().GetProperty("ParameterPrefix")!.SetValue(dbProvider, "@");

    var builder = Activator.CreateInstance(builderType)!;
    Invoke(builder, "UseHybridConfig", options!);
    Invoke(builder, "AddSqlMaps", Enum.Parse(resourceType, "Embedded"),
      "Dy.MedicalRecognition.Repository.MedicalRecognitionReportAggregate.**,Dy.MedicalRecognition.Repository",
      repositoryAssembly);
    Invoke(builder, "AddSqlMaps", Enum.Parse(resourceType, "Embedded"),
      "Dy.MedicalRecognition.Repository.Queries.**,Dy.MedicalRecognition.Repository",
      repositoryAssembly);
    Invoke(builder, "Build");

    var sqlConfig = builderType.GetProperty("SqlConfig")!.GetValue(builder)!;

    var registeredKeys = GetRegisteredKeys(sqlConfig);
    Assert.NotEmpty(registeredKeys);

    var aggregateKey = "MedicalRecognitionReport.CreateMedicalStandardCategory";
    var categoryKey = "MedicalStandardCategory.CreateMedicalStandardCategory";
    Console.WriteLine($"FullSqlId lookup: {aggregateKey} => {(registeredKeys.Contains(aggregateKey) ? "registered" : "not registered")}");
    Console.WriteLine($"FullSqlId lookup: {categoryKey} => {(registeredKeys.Contains(categoryKey) ? "registered" : "not registered")}");
    Console.WriteLine("Registered Statement keys:");
    foreach (var key in registeredKeys) Console.WriteLine(key);

    Assert.DoesNotContain(aggregateKey, registeredKeys);
    Assert.Contains(categoryKey, registeredKeys);
    Assert.Contains("MedicalStandardCategory.UpdateMedicalStandardCategory", registeredKeys);
    Assert.Contains("MedicalStandardGroup.CreateMedicalStandardGroup", registeredKeys);
    Assert.Contains("MedicalStandardItem.CreateMedicalStandardItem", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryMedicalStandardCategoryList", registeredKeys);
    Assert.Contains("MedicalRecognitionReportQuery.QueryEffectiveMedicalStandardCatalog", registeredKeys);
    Assert.Contains("MedicalRecognitionReport.QueryAllRecognitionReference", registeredKeys);
    Assert.Contains("MedicalRecognitionReport.RecognitionReferenceColumns", registeredKeys);
  }

  /// <summary>
  /// 从已构建的 SqlMap 配置对象中枚举全部语句，取出各自注册的完整语句标识。
  /// </summary>
  /// <param name="sqlConfig">宿主程序集构建出的 SqlMap 配置对象，其 <c>SqlMaps</c> 属性按作用域存放语句集合。</param>
  /// <returns>去重排序后的完整语句标识列表；没有任何语句注册时返回空数组，由调用方断言非空。</returns>
  private static IReadOnlyList<string> GetRegisteredKeys(object sqlConfig)
  {
    var sqlMaps = (System.Collections.IDictionary)sqlConfig.GetType().GetProperty("SqlMaps")!.GetValue(sqlConfig)!;
    var keys = new List<string>();
    foreach (System.Collections.DictionaryEntry entry in sqlMaps)
    {
      var sqlMap = entry.Value!;
      var statements = (System.Collections.IDictionary)sqlMap.GetType().GetProperty("Statements")!.GetValue(sqlMap)!;
      foreach (System.Collections.DictionaryEntry statement in statements)
      {
        var fullSqlId = (string?)statement.Value!.GetType().GetProperty("FullSqlId")!.GetValue(statement.Value);
        if (!string.IsNullOrWhiteSpace(fullSqlId)) keys.Add(fullSqlId);
      }
    }

    return keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
  }

  /// <summary>
  /// 按方法名与参数个数反射调用目标对象的实例方法，用于驱动宿主构建 SqlMap 注册表。
  /// </summary>
  /// <param name="target">被调用的宿主构建器实例。</param>
  /// <param name="methodName">目标方法名，按名称与实参个数唯一定位。</param>
  /// <param name="arguments">按顺序传给目标方法的实参。</param>
  /// <returns>目标方法的返回值；目标方法返回 void 时为 <see langword="null"/>。</returns>
  /// <exception cref="InvalidOperationException">目标类型上不存在名称与参数个数同时匹配的公开实例方法时，由 <c>Single</c> 抛出。</exception>
  private static object? Invoke(object target, string methodName, params object[] arguments) =>
    target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
      .Single(method => method.Name == methodName && method.GetParameters().Length == arguments.Length)
      .Invoke(target, arguments);

  /// <summary>
  /// 把宿主配置 JSON 递归展开为以冒号分隔的扁平键值集合，供内存配置源按层级读取。
  /// </summary>
  /// <param name="element">待展开的 JSON 元素，可为对象、数组或叶子值。</param>
  /// <param name="prefix">当前元素在配置中的键前缀；根节点传空串。</param>
  /// <param name="values">接收展开结果的目标集合，叶子值按原样写入，同一键重复出现时以最后一次为准。</param>
  private static void Flatten(JsonElement element, string prefix, IDictionary<string, string?> values)
  {
    if (element.ValueKind == JsonValueKind.Object)
    {
      foreach (var property in element.EnumerateObject())
        Flatten(property.Value, string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}", values);
      return;
    }

    if (element.ValueKind == JsonValueKind.Array)
    {
      var index = 0;
      foreach (var item in element.EnumerateArray()) Flatten(item, $"{prefix}:{index++}", values);
      return;
    }

    values[prefix] = element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
  }

  /// <summary>
  /// 定位已构建的宿主输出目录，该目录同时提供运行时注册所需的程序集与宿主配置。
  /// </summary>
  /// <returns>包含 <c>Dy.Earthrace.dll</c> 的宿主输出目录绝对路径。</returns>
  /// <exception cref="DirectoryNotFoundException">从测试程序集所在目录逐级向上都未找到包含 <c>Dy.Earthrace.dll</c> 的宿主输出目录时抛出；此时需先构建宿主工程。</exception>
  private static string FindHostOutputDirectory()
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var candidate = Path.Combine(directory.FullName, "Dy.MedicalRecognition", "bin", "Debug", "net10.0");
      if (File.Exists(Path.Combine(candidate, "Dy.Earthrace.dll"))) return candidate;
      directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate the built host output containing Dy.Earthrace.dll.");
  }

  /// <summary>
  /// 定位指定名称的仓储 SqlMap 文件，供断言直接读取语句文本。
  /// </summary>
  /// <param name="fileName">SqlMap 文件名，例如 <c>MedicalStandardCategory.xml</c>。</param>
  /// <returns>仓储工程中该 SqlMap 文件的绝对路径。</returns>
  /// <exception cref="FileNotFoundException">从测试程序集所在目录逐级向上都未找到该 SqlMap 文件时抛出；此时检查仓储工程的 SqlMap 是否已随源码保留在原位。</exception>
  private static string FindRepositorySqlMap(string fileName)
  {
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
      var candidate = Path.Combine(directory.FullName, "Dy.MedicalRecognition.Repository", "MedicalRecognitionReportAggregate", fileName);
      if (File.Exists(candidate)) return candidate;
      directory = directory.Parent;
    }

    throw new FileNotFoundException($"Could not locate repository SqlMap '{fileName}'.");
  }

  /// <summary>
  /// 按文件路径把指定程序集加载进测试进程的默认加载上下文，供反射读取 SqlMap 注册结果。
  /// </summary>
  /// <param name="directory">程序集所在目录。</param>
  /// <param name="fileName">程序集文件名，例如 <c>Dy.Earthrace.dll</c>。</param>
  /// <returns>已加载的程序集。</returns>
  private static Assembly LoadAssembly(string directory, string fileName)
  {
    var path = Path.Combine(directory, fileName);
    return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
  }
}
