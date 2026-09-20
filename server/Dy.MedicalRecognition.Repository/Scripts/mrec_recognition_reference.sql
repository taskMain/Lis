-- postgresql schema
create table mrec_recognition_reference (
  id uuid not null,
  recognition_match_item_id uuid not null,
  referenced_time timestamp without time zone not null,
  reference_dept_id text not null,
  reference_dept_name text not null,
  reference_doctor_id text not null,
  reference_doctor_name text not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_recognition_reference is '互认引用事实';
comment on column mrec_recognition_reference.id is '主键ID';
comment on column mrec_recognition_reference.recognition_match_item_id is '互认匹配项ID';
comment on column mrec_recognition_reference.referenced_time is '实际引用时间';
comment on column mrec_recognition_reference.reference_dept_id is '引用科室ID';
comment on column mrec_recognition_reference.reference_dept_name is '引用科室名称';
comment on column mrec_recognition_reference.reference_doctor_id is '引用医生ID';
comment on column mrec_recognition_reference.reference_doctor_name is '引用医生名称';
comment on column mrec_recognition_reference.oper_time is '操作时间';
comment on column mrec_recognition_reference.oper_id is '操作人';

create unique index ux_mrec_recognition_reference_match_item
on mrec_recognition_reference (recognition_match_item_id);

comment on index ux_mrec_recognition_reference_match_item is '同一互认匹配项至多一条引用事实并兜底并发写入撞键';
