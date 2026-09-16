-- postgresql schema
create table mrec_mutual_recognition_item (
  id uuid not null,
  organization_code text not null,
  standard_item_id uuid not null,
  standard_project_code text not null,
  recognition_duration_days integer not null,
  is_valid boolean not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_mutual_recognition_item is '互认项目配置';
comment on column mrec_mutual_recognition_item.id is '主键ID';
comment on column mrec_mutual_recognition_item.organization_code is '组织编码';
comment on column mrec_mutual_recognition_item.standard_item_id is '标准医疗项目ID';
comment on column mrec_mutual_recognition_item.standard_project_code is '标准项目编码';
comment on column mrec_mutual_recognition_item.recognition_duration_days is '可互认时间天数';
comment on column mrec_mutual_recognition_item.is_valid is '是否启用';
comment on column mrec_mutual_recognition_item.oper_time is '操作时间';
comment on column mrec_mutual_recognition_item.oper_id is '操作人';

create unique index ux_mrec_mutual_recognition_org_project
on mrec_mutual_recognition_item (organization_code, standard_project_code);

comment on index ux_mrec_mutual_recognition_org_project is '同一组织标准项目互认配置唯一（含停用配置）';
