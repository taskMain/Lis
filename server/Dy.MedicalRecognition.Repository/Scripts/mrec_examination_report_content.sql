-- postgresql schema
create table mrec_examination_report_content (
  id uuid not null,
  report_version_id uuid not null,
  source_examination_type_code text,
  source_examination_type_name text,
  report_remark text,
  overall_abnormal_flag text,
  findings text not null,
  conclusion text not null,
  condition_description text,
  examination_purpose text,
  source_diagnosis_code text,
  source_diagnosis_name text not null,
  examination_time timestamp without time zone not null,
  examiner_id text not null,
  examiner_name text not null,
  source_image_status integer not null,
  image_access_url text,
  examination_method text,
  device_code text,
  device_name text,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_examination_report_content is '检查报告专项内容';
comment on column mrec_examination_report_content.id is '主键ID';
comment on column mrec_examination_report_content.report_version_id is '报告版本ID';
comment on column mrec_examination_report_content.source_examination_type_code is '来源检查类型编码';
comment on column mrec_examination_report_content.source_examination_type_name is '来源检查类型名称';
comment on column mrec_examination_report_content.report_remark is '报告备注';
comment on column mrec_examination_report_content.overall_abnormal_flag is '整体异常标识';
comment on column mrec_examination_report_content.findings is '检查所见';
comment on column mrec_examination_report_content.conclusion is '检查结论';
comment on column mrec_examination_report_content.condition_description is '病情描述';
comment on column mrec_examination_report_content.examination_purpose is '检查目的';
comment on column mrec_examination_report_content.source_diagnosis_code is '来源诊断编码';
comment on column mrec_examination_report_content.source_diagnosis_name is '来源诊断名称';
comment on column mrec_examination_report_content.examination_time is '实际检查时间';
comment on column mrec_examination_report_content.examiner_id is '检查医生ID';
comment on column mrec_examination_report_content.examiner_name is '检查医生名称';
comment on column mrec_examination_report_content.source_image_status is '来源影像状态';
comment on column mrec_examination_report_content.image_access_url is '影像调阅地址';
comment on column mrec_examination_report_content.examination_method is '检查方法';
comment on column mrec_examination_report_content.device_code is '设备编码';
comment on column mrec_examination_report_content.device_name is '设备名称';
comment on column mrec_examination_report_content.oper_time is '操作时间';
comment on column mrec_examination_report_content.oper_id is '操作人';

create index ix_mrec_examination_report_content_version
on mrec_examination_report_content (report_version_id);

comment on index ix_mrec_examination_report_content_version is '按报告版本读取检查专项内容';
