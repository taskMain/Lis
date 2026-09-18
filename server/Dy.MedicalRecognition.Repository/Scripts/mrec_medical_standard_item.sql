-- postgresql schema
create table mrec_medical_standard_item (
  id uuid not null,
  category_id uuid not null,
  group_id uuid not null,
  code varchar(200) not null,
  name varchar(200) not null,
  is_valid boolean not null,
  remark varchar(600),
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_medical_standard_item is '标准医疗项目';
comment on column mrec_medical_standard_item.id is '主键 ID';
comment on column mrec_medical_standard_item.category_id is '分类ID';
comment on column mrec_medical_standard_item.group_id is '分组ID';
comment on column mrec_medical_standard_item.code is '标准项目编码';
comment on column mrec_medical_standard_item.name is '标准项目名称';
comment on column mrec_medical_standard_item.is_valid is '是否启用';
comment on column mrec_medical_standard_item.remark is '备注';
comment on column mrec_medical_standard_item.oper_time is '操作时间';
comment on column mrec_medical_standard_item.oper_id is '操作人';

create unique index ux_mrec_medical_standard_item_code
on mrec_medical_standard_item (code);
