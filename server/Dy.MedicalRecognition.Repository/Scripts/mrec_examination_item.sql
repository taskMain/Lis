-- postgresql schema
create table mrec_examination_item (
  id uuid not null,
  report_version_id uuid not null,
  source_project_name text not null,
  source_project_code text,
  standard_project_code text,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_examination_item is '检查项目';
comment on column mrec_examination_item.id is '主键ID';
comment on column mrec_examination_item.report_version_id is '报告版本ID';
comment on column mrec_examination_item.source_project_name is '来源项目名称';
comment on column mrec_examination_item.source_project_code is '来源项目编码';
comment on column mrec_examination_item.standard_project_code is '互认项目编码';
comment on column mrec_examination_item.oper_time is '操作时间';
comment on column mrec_examination_item.oper_id is '操作人';

create index ix_mrec_examination_item_version
on mrec_examination_item (report_version_id);

comment on index ix_mrec_examination_item_version is '按报告版本读取检查项目';
