namespace Dy.MedicalRecognition.Application.OrganizationScope;

/// <summary>
/// IAM 组织类型编码：本项目把组织节点归类为医共体、医院、院区三层时识别的类型值。
/// </summary>
/// <remarks>取值来自 IAM 组织类型字典，本项目不维护组织类型数据；科室与病区两类组织不参与本项目的三层归类。</remarks>
internal static class IamOrganizationTypeCodes
{
  /// <summary>医共体类型编码。</summary>
  internal const string CountyMedicalCommunity = "CountyMedicalCommunity";

  /// <summary>医院类型编码。</summary>
  internal const string Hospital = "Hospital";

  /// <summary>院区类型编码。</summary>
  internal const string Branch = "Branch";
}
