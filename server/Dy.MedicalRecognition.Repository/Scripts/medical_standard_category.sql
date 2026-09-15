-- postgresql schema
create table mrec_medical_standard_category (
  id uuid not null,
  item_type integer not null,
  name varchar(200) not null,
  is_valid boolean not null,
  remark varchar(600),
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_medical_standard_category is '标准医疗项目分类';
comment on column mrec_medical_standard_category.id is '主键 ID';
comment on column mrec_medical_standard_category.item_type is '项目类型';
comment on column mrec_medical_standard_category.name is '分类名称';
comment on column mrec_medical_standard_category.is_valid is '是否启用';
comment on column mrec_medical_standard_category.remark is '备注';
comment on column mrec_medical_standard_category.oper_time is '操作时间';
comment on column mrec_medical_standard_category.oper_id is '操作人';

create unique index ux_mrec_medical_standard_category_name
on mrec_medical_standard_category (name);
