-- postgresql schema
create table mrec_medical_standard_group (
  id uuid not null,
  category_id uuid not null,
  name varchar(200) not null,
  is_valid boolean not null,
  remark varchar(600),
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_medical_standard_group is '标准医疗项目分组';
comment on column mrec_medical_standard_group.id is '主键 ID';
comment on column mrec_medical_standard_group.category_id is '分类ID';
comment on column mrec_medical_standard_group.name is '分组名称';
comment on column mrec_medical_standard_group.is_valid is '是否启用';
comment on column mrec_medical_standard_group.remark is '备注';
comment on column mrec_medical_standard_group.oper_time is '操作时间';
comment on column mrec_medical_standard_group.oper_id is '操作人';

create unique index ux_mrec_medical_standard_group_category_name
on mrec_medical_standard_group (category_id, name);
