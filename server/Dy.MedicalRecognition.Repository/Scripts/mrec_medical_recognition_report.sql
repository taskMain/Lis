-- postgresql schema
create table mrec_medical_recognition_report (
  id uuid not null,
  organization_code text not null,
  hospital_code text not null,
  branch_code text not null,
  report_type integer not null,
  report_no text not null,
  patient_id uuid not null,
  current_version_id uuid not null,
  report_time timestamp without time zone not null,
  patient_name text not null,
  identity_document_no text not null,
  status integer not null,
  voided_time timestamp without time zone,
  void_reason text,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_medical_recognition_report is '互认报告';
comment on column mrec_medical_recognition_report.id is '主键ID';
comment on column mrec_medical_recognition_report.organization_code is '组织编码';
comment on column mrec_medical_recognition_report.hospital_code is '医院编码';
comment on column mrec_medical_recognition_report.branch_code is '院区编码';
comment on column mrec_medical_recognition_report.report_type is '报告类型';
comment on column mrec_medical_recognition_report.report_no is '报告单号';
comment on column mrec_medical_recognition_report.patient_id is '平台患者ID';
comment on column mrec_medical_recognition_report.current_version_id is '当前版本ID';
comment on column mrec_medical_recognition_report.report_time is '报告时间';
comment on column mrec_medical_recognition_report.patient_name is '患者姓名';
comment on column mrec_medical_recognition_report.identity_document_no is '证件号码';
comment on column mrec_medical_recognition_report.status is '生命周期状态';
comment on column mrec_medical_recognition_report.voided_time is '作废时间';
comment on column mrec_medical_recognition_report.void_reason is '作废原因';
comment on column mrec_medical_recognition_report.oper_time is '操作时间';
comment on column mrec_medical_recognition_report.oper_id is '操作人';

create unique index ux_mrec_medical_recognition_report_business_key
on mrec_medical_recognition_report (organization_code, hospital_code, branch_code, report_type, report_no);

comment on index ux_mrec_medical_recognition_report_business_key is '同一组织医院院区报告类型与报告单号的报告唯一（含已作废报告）';

create index ix_mrec_medical_recognition_report_scope
on mrec_medical_recognition_report (organization_code, hospital_code, branch_code, report_time desc, id desc);

comment on index ix_mrec_medical_recognition_report_scope is '报告列表按组织医院院区筛选并按报告时间与主键倒序排序';

create index ix_mrec_medical_recognition_report_patient_name
on mrec_medical_recognition_report (patient_name text_pattern_ops);

comment on index ix_mrec_medical_recognition_report_patient_name is '报告列表按患者姓名前缀筛选';

create index ix_mrec_medical_recognition_report_identity_document_no
on mrec_medical_recognition_report (identity_document_no text_pattern_ops);

comment on index ix_mrec_medical_recognition_report_identity_document_no is '报告列表按证件号码前缀筛选';
