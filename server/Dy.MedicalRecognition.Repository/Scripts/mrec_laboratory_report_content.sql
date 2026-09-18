-- postgresql schema
create table mrec_laboratory_report_content (
  id uuid not null,
  report_version_id uuid not null,
  report_category_code text,
  report_category_name text,
  report_remark text,
  overall_abnormal_flag text,
  source_order_serial_no text,
  specimen_collected_time timestamp without time zone,
  specimen_submitted_time timestamp without time zone,
  laboratory_received_time timestamp without time zone,
  source_specimen_no text not null,
  specimen_type_code text not null,
  specimen_type_name text not null,
  testing_completed_time timestamp without time zone not null,
  inspector_id text not null,
  inspector_name text not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_laboratory_report_content is '检验报告专项内容';
comment on column mrec_laboratory_report_content.id is '主键ID';
comment on column mrec_laboratory_report_content.report_version_id is '报告版本ID';
comment on column mrec_laboratory_report_content.report_category_code is '报告类别编码';
comment on column mrec_laboratory_report_content.report_category_name is '报告类别名称';
comment on column mrec_laboratory_report_content.report_remark is '报告备注';
comment on column mrec_laboratory_report_content.overall_abnormal_flag is '整体异常标识';
comment on column mrec_laboratory_report_content.source_order_serial_no is '来源医嘱流水号';
comment on column mrec_laboratory_report_content.specimen_collected_time is '标本采集时间';
comment on column mrec_laboratory_report_content.specimen_submitted_time is '标本送检时间';
comment on column mrec_laboratory_report_content.laboratory_received_time is '检验科接收时间';
comment on column mrec_laboratory_report_content.source_specimen_no is '院内标本号';
comment on column mrec_laboratory_report_content.specimen_type_code is '标本类型编码';
comment on column mrec_laboratory_report_content.specimen_type_name is '标本类型名称';
comment on column mrec_laboratory_report_content.testing_completed_time is '检测完成时间';
comment on column mrec_laboratory_report_content.inspector_id is '检验人ID';
comment on column mrec_laboratory_report_content.inspector_name is '检验人名称';
comment on column mrec_laboratory_report_content.oper_time is '操作时间';
comment on column mrec_laboratory_report_content.oper_id is '操作人';

create index ix_mrec_laboratory_report_content_version
on mrec_laboratory_report_content (report_version_id);

comment on index ix_mrec_laboratory_report_content_version is '按报告版本读取检验专项内容';
