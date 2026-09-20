-- postgresql schema
create table mrec_recognition_match_item (
  id uuid not null,
  recognition_match_record_id uuid not null,
  item_type integer not null,
  standard_project_code text not null,
  report_id uuid not null,
  report_version_id uuid not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_recognition_match_item is '互认匹配项';
comment on column mrec_recognition_match_item.id is '主键ID';
comment on column mrec_recognition_match_item.recognition_match_record_id is '互认匹配记录ID';
comment on column mrec_recognition_match_item.item_type is '项目类型';
comment on column mrec_recognition_match_item.standard_project_code is '标准项目编码';
comment on column mrec_recognition_match_item.report_id is '报告ID';
comment on column mrec_recognition_match_item.report_version_id is '报告版本ID';
comment on column mrec_recognition_match_item.oper_time is '操作时间';
comment on column mrec_recognition_match_item.oper_id is '操作人';

create index ix_mrec_recognition_match_item_record
on mrec_recognition_match_item (recognition_match_record_id);

comment on index ix_mrec_recognition_match_item_record is '按所属互认匹配记录读取匹配项';

create index ix_mrec_recognition_match_item_project
on mrec_recognition_match_item (standard_project_code);

comment on index ix_mrec_recognition_match_item_project is '按标准项目编码汇总与关联匹配项';
