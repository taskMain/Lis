using Dy.Core.SourceGen;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Dtos;
using Dy.MedicalRecognition.Application.Contracts.MedicalRecognitionReportAggregate.Requests;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate;
using Dy.MedicalRecognition.Domain.MedicalRecognitionReportAggregate.Commands;

[assembly: ObjectMap(typeof(CreateMedicalStandardCategoryRequest), typeof(CreateMedicalStandardCategoryCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(UpdateMedicalStandardCategoryRequest), typeof(UpdateMedicalStandardCategoryCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(EnableMedicalStandardCategoryRequest), typeof(EnableMedicalStandardCategoryCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(DisableMedicalStandardCategoryRequest), typeof(DisableMedicalStandardCategoryCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(CreateMedicalStandardGroupRequest), typeof(CreateMedicalStandardGroupCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(UpdateMedicalStandardGroupRequest), typeof(UpdateMedicalStandardGroupCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(EnableMedicalStandardGroupRequest), typeof(EnableMedicalStandardGroupCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(DisableMedicalStandardGroupRequest), typeof(DisableMedicalStandardGroupCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(CreateMedicalStandardItemRequest), typeof(CreateMedicalStandardItemCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(ChangeMedicalStandardItemRemarkRequest), typeof(ChangeMedicalStandardItemRemarkCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(EnableMedicalStandardItemRequest), typeof(EnableMedicalStandardItemCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(DisableMedicalStandardItemRequest), typeof(DisableMedicalStandardItemCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(CreateMutualRecognitionItemRequest), typeof(CreateMutualRecognitionItemCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(UpdateMutualRecognitionItemConfigurationRequest), typeof(UpdateMutualRecognitionItemConfigurationCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(EnableMutualRecognitionItemRequest), typeof(EnableMutualRecognitionItemCommand), ObjectMapMode.OneWay)]
[assembly: ObjectMap(typeof(DisableMutualRecognitionItemRequest), typeof(DisableMutualRecognitionItemCommand), ObjectMapMode.OneWay)]
// 两个金额保存请求的当前金额不参与自动映射：请求侧金额可空（必填校验据此生效），
// 而生成的映射把可空金额的缺省值静默写成 0，会把"未提交金额"落成一次真实的零元写入；
// 因此该字段由应用入口在公共请求校验通过后按已确定非空的值显式写入命令。
[assembly: ObjectMap(typeof(SaveOrganizationHospitalBranchRecognitionAmountRequest), typeof(SaveOrganizationHospitalBranchRecognitionAmountCommand), ObjectMapMode.OneWay, excludedProperties: ["CurrentAmount"])]
[assembly: ObjectMap(typeof(SaveBranchRecognitionAmountRequest), typeof(SaveOrganizationHospitalBranchRecognitionAmountCommand), ObjectMapMode.OneWay, excludedProperties: ["CurrentAmount"])]
[assembly: ObjectMap(typeof(MedicalStandardCategory), typeof(MedicalStandardCategoryDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(MedicalStandardGroup), typeof(MedicalStandardGroupDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(MedicalStandardItem), typeof(MedicalStandardItemDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(MutualRecognitionItem), typeof(MutualRecognitionItemDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(OrganizationHospitalBranchRecognitionAmount), typeof(OrganizationHospitalBranchRecognitionAmountDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(PlatformPatient), typeof(PlatformPatientDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(MedicalRecognitionReport), typeof(MedicalRecognitionReportDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(MedicalReportVersion), typeof(MedicalReportVersionDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(LaboratoryReportContent), typeof(LaboratoryReportContentDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(LaboratoryResultItem), typeof(LaboratoryResultItemDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(LaboratoryBacteriaResult), typeof(LaboratoryBacteriaResultDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(LaboratoryAntimicrobialSusceptibility), typeof(LaboratoryAntimicrobialSusceptibilityDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(ExaminationReportContent), typeof(ExaminationReportContentDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(ExaminationItem), typeof(ExaminationItemDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(ExaminationSite), typeof(ExaminationSiteDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(RecognitionMatchRecord), typeof(RecognitionMatchRecordDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(RecognitionMatchItem), typeof(RecognitionMatchItemDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(RecognitionProcessingResult), typeof(RecognitionProcessingResultDto), ObjectMapMode.TwoWay)]
[assembly: ObjectMap(typeof(RecognitionReference), typeof(RecognitionReferenceDto), ObjectMapMode.TwoWay)]
