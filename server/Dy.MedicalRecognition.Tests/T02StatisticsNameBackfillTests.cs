using Dy.IAM.Sdk;
using Dy.MedicalRecognition.Application.Contracts.Queries.Pagination;
using Dy.MedicalRecognition.Application.Contracts.Queries.RecognitionStatistics;
using Dy.MedicalRecognition.Application.Contracts.ReadModels;
using Dy.MedicalRecognition.Application.Queries;
using Dy.MedicalRecognition.Domain.Queries;
using Dy.MedicalRecognition.Domain.Share.Enums;
using Xunit;

namespace Dy.MedicalRecognition.Tests;

/// <summary>
/// 统计明细名称回填的容错口径：历史行的三层编码与当前 IAM 组织树不再自洽时，
/// 明细仍应正常返回，名称按编码的唯一候选取值或留空，不因归属不符把整片明细打成失败。
/// </summary>
/// <remarks>
/// 触发条件取自真实环境：IAM 改名后，历史行保存的院区编码仍指向旧节点，该编码在当前树中落到另一棵组织树的医院下——
/// 三层编码各自都能命中且能定论，但父子归属不符。写入口仍按严格规则拒绝，本类只覆盖读取展示面。
/// </remarks>
public sealed class T02StatisticsNameBackfillTests
{
  /// <summary>统计区间起点。</summary>
  private static readonly DateOnly PeriodFrom = new(2026, 9, 1);

  /// <summary>统计区间终点。</summary>
  private static readonly DateOnly PeriodTo = new(2026, 9, 30);

  /// <summary>跨树节点的名称；用于断言名称取自该编码的唯一候选。</summary>
  private const string DetachedBranchName = "示范院区（跨树）";

  /// <summary>
  /// 明细行的院区编码唯一命中但父节点不是该医院时，查询仍返回该行，名称取该编码的唯一候选。
  /// </summary>
  [Fact]
  public async Task Usage_details_return_row_when_branch_parent_mismatches_hospital()
  {
    FakeIamScope scope = FakeIamScope.WithManyHospitals();
    DetachBranchFromItsHospital(scope);

    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.Add(BuildReminderRow());

    MedicalRecognitionReportQueryAppService service = new(repository, new PlatformParameterRepositoryStub(), scope, scope, scope);

    PageResultDto<RecognitionUsageDetailReadModel> result = await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest());

    // 归属不符不再中断整片明细；编码原样返回，名称按编码的唯一候选给出。
    RecognitionUsageDetailReadModel row = Assert.Single(result.Items);
    Assert.Equal(FakeIamScope.SecondHospitalCode, row.Receiver.HospitalCode);
    Assert.Equal(FakeIamScope.SecondBranchCode, row.Receiver.BranchCode);
    Assert.Equal(DetachedBranchName, row.Receiver.BranchName);
  }

  /// <summary>
  /// 明细行的编码在目录中完全不存在时，名称留空而不是回填编码文本。
  /// </summary>
  [Fact]
  public async Task Usage_details_leave_blank_name_when_code_is_absent_from_directory()
  {
    FakeIamScope scope = FakeIamScope.WithManyHospitals();
    // 编码树里保留该编码，但目录中删除节点：模拟历史行引用了已不存在的组织节点。
    scope.Organizations.Remove(FakeIamScope.SecondBranchId);

    FakeStatisticsQueryRepository repository = new();
    repository.ReminderItems.Add(BuildReminderRow());

    MedicalRecognitionReportQueryAppService service = new(repository, new PlatformParameterRepositoryStub(), scope, scope, scope);

    PageResultDto<RecognitionUsageDetailReadModel> result = await service.QueryRecognitionUsageDetailsAsync(BuildDetailsRequest());

    RecognitionUsageDetailReadModel row = Assert.Single(result.Items);
    Assert.Equal(FakeIamScope.SecondBranchCode, row.Receiver.BranchCode);
    Assert.Equal(string.Empty, row.Receiver.BranchName);
  }

  /// <summary>
  /// 把院区节点的父引用改到另一棵组织树的医院下，复现 IAM 改名后的跨树组合。
  /// </summary>
  /// <param name="scope">待改写的 IAM 替身。</param>
  private static void DetachBranchFromItsHospital(FakeIamScope scope)
  {
    Guid branchId = FakeIamScope.SecondBranchId;
    scope.Organizations[branchId] = new IamDirectoryOrganization(
      branchId,
      DetachedBranchName,
      FakeIamScope.OtherHospitalId,
      true,
      [
        new IamDirectoryPathItem(FakeIamScope.OtherOrganizationId, "组织-其他医共体"),
        new IamDirectoryPathItem(FakeIamScope.OtherHospitalId, "组织-其他医院"),
        new IamDirectoryPathItem(branchId, "组织-院区（跨树）")
      ])
    {
      OrganizationType = new IamDirectoryOrganizationType(Guid.NewGuid(), "Branch", "Branch", true)
    };
  }

  /// <summary>
  /// 构造一行使用「同组织第二家医院 + 院区编码」的提醒明细。
  /// </summary>
  /// <returns>提醒明细行。</returns>
  private static RecognitionReminderDetailItem BuildReminderRow() => new()
  {
    RecognitionMatchRecordId = Guid.NewGuid(),
    RecognitionMatchItemId = Guid.NewGuid(),
    MatchCreatedTime = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Unspecified),
    VisitType = VisitType.Outpatient,
    VisitSerialNo = "OUT-001",
    ReceiverOrganizationCode = FakeIamScope.OrganizationCode,
    ReceiverHospitalCode = FakeIamScope.SecondHospitalCode,
    ReceiverBranchCode = FakeIamScope.SecondBranchCode,
    SourceOrganizationCode = FakeIamScope.OrganizationCode,
    SourceHospitalCode = FakeIamScope.HospitalCode,
    SourceBranchCode = FakeIamScope.BranchCode,
    ItemType = MedicalItemType.Laboratory,
    CategoryName = "示范分类",
    GroupName = "示范分组",
    StandardProjectCode = "P001",
    StandardItemName = "示范项目",
    PatientName = "示范患者",
    IdentityDocumentNo = "530101199001150000"
  };

  /// <summary>构造平台版使用方明细请求。</summary>
  /// <returns>按通用日期范围的提醒明细请求。</returns>
  private static RecognitionUsageDetailsQueryRequest BuildDetailsRequest() => new()
  {
    StartTime = PeriodFrom,
    EndTime = PeriodTo,
    DetailType = RecognitionUsageDetailType.Reminder,
    Page = new PageRequestDto { PageIndex = 1, PageSize = 20 }
  };
}
