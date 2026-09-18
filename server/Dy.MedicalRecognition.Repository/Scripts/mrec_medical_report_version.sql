-- postgresql schema
create table mrec_medical_report_version (
  id uuid not null,
  report_id uuid not null,
  patient_id uuid not null,
  version_number integer not null,
  source_report_name text not null,
  patient_name text not null,
  patient_gender_code text not null,
  patient_birth_date timestamp without time zone not null,
  patient_phone_number text,
  age_at_report text,
  identity_document_type_code text not null,
  identity_document_no text not null,
  visit_type integer not null,
  visit_serial_no text not null,
  application_dept_id text not null,
  application_dept_name text not null,
  application_doctor_id text not null,
  application_doctor_name text not null,
  execution_dept_id text not null,
  execution_dept_name text not null,
  report_dept_id text not null,
  report_dept_name text not null,
  report_doctor_id text not null,
  report_doctor_name text not null,
  review_doctor_id text not null,
  review_doctor_name text not null,
  review_time timestamp without time zone,
  inpatient_no text,
  ward_name text,
  room_name text,
  bed_no text,
  application_time timestamp without time zone not null,
  report_time timestamp without time zone not null,
  source_modified_time timestamp without time zone not null,
  received_time timestamp without time zone not null,
  pdf_file_id text not null,
  pdf_file_name text not null,
  source_confidential_flag text,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_medical_report_version is '报告版本';
comment on column mrec_medical_report_version.id is '主键ID';
comment on column mrec_medical_report_version.report_id is '报告ID';
comment on column mrec_medical_report_version.patient_id is '平台患者ID';
comment on column mrec_medical_report_version.version_number is '版本序号';
comment on column mrec_medical_report_version.source_report_name is '来源报告名称';
comment on column mrec_medical_report_version.patient_name is '患者姓名';
comment on column mrec_medical_report_version.patient_gender_code is '患者性别代码';
comment on column mrec_medical_report_version.patient_birth_date is '患者出生日期';
comment on column mrec_medical_report_version.patient_phone_number is '患者联系电话';
comment on column mrec_medical_report_version.age_at_report is '报告时年龄';
comment on column mrec_medical_report_version.identity_document_type_code is '证件类型代码';
comment on column mrec_medical_report_version.identity_document_no is '证件号码';
comment on column mrec_medical_report_version.visit_type is '就诊类型';
comment on column mrec_medical_report_version.visit_serial_no is '就诊流水号';
comment on column mrec_medical_report_version.application_dept_id is '申请科室ID';
comment on column mrec_medical_report_version.application_dept_name is '申请科室名称';
comment on column mrec_medical_report_version.application_doctor_id is '申请医生ID';
comment on column mrec_medical_report_version.application_doctor_name is '申请医生名称';
comment on column mrec_medical_report_version.execution_dept_id is '执行科室ID';
comment on column mrec_medical_report_version.execution_dept_name is '执行科室名称';
comment on column mrec_medical_report_version.report_dept_id is '报告科室ID';
comment on column mrec_medical_report_version.report_dept_name is '报告科室名称';
comment on column mrec_medical_report_version.report_doctor_id is '报告医生ID';
comment on column mrec_medical_report_version.report_doctor_name is '报告医生名称';
comment on column mrec_medical_report_version.review_doctor_id is '审核医生ID';
comment on column mrec_medical_report_version.review_doctor_name is '审核医生名称';
comment on column mrec_medical_report_version.review_time is '审核时间';
comment on column mrec_medical_report_version.inpatient_no is '住院号';
comment on column mrec_medical_report_version.ward_name is '病区名称';
comment on column mrec_medical_report_version.room_name is '病房名称';
comment on column mrec_medical_report_version.bed_no is '床位号';
comment on column mrec_medical_report_version.application_time is '申请时间';
comment on column mrec_medical_report_version.report_time is '报告时间';
comment on column mrec_medical_report_version.source_modified_time is '源端报告修改时间';
comment on column mrec_medical_report_version.received_time is '平台接收时间';
comment on column mrec_medical_report_version.pdf_file_id is 'PDF文件标识';
comment on column mrec_medical_report_version.pdf_file_name is 'PDF文件名';
comment on column mrec_medical_report_version.source_confidential_flag is '来源保密标识';
comment on column mrec_medical_report_version.oper_time is '操作时间';
comment on column mrec_medical_report_version.oper_id is '操作人';

create unique index ux_mrec_medical_report_version_report_version
on mrec_medical_report_version (report_id, version_number);

comment on index ux_mrec_medical_report_version_report_version is '同一报告内版本序号唯一';
