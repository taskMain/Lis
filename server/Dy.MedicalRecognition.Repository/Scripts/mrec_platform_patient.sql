-- postgresql schema
create table mrec_platform_patient (
  id uuid not null,
  identity_document_type_code text not null,
  identity_document_no text not null,
  patient_name text not null,
  patient_gender_code text not null,
  patient_birth_date timestamp without time zone not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_platform_patient is '平台患者';
comment on column mrec_platform_patient.id is '主键ID';
comment on column mrec_platform_patient.identity_document_type_code is '证件类型代码';
comment on column mrec_platform_patient.identity_document_no is '证件号码';
comment on column mrec_platform_patient.patient_name is '患者姓名';
comment on column mrec_platform_patient.patient_gender_code is '患者性别代码';
comment on column mrec_platform_patient.patient_birth_date is '患者出生日期';
comment on column mrec_platform_patient.oper_time is '操作时间';
comment on column mrec_platform_patient.oper_id is '操作人';

create unique index ux_mrec_platform_patient_document
on mrec_platform_patient (identity_document_type_code, identity_document_no);

comment on index ux_mrec_platform_patient_document is '同一证件类型与证件号码的平台患者唯一';
