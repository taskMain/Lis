-- postgresql schema
create table mrec_examination_site (
  id uuid not null,
  examination_item_id uuid not null,
  source_site_code text,
  site_name text not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_examination_site is '检查部位';
comment on column mrec_examination_site.id is '主键ID';
comment on column mrec_examination_site.examination_item_id is '检查项目ID';
comment on column mrec_examination_site.source_site_code is '来源部位编码';
comment on column mrec_examination_site.site_name is '部位名称';
comment on column mrec_examination_site.oper_time is '操作时间';
comment on column mrec_examination_site.oper_id is '操作人';

create index ix_mrec_examination_site_item
on mrec_examination_site (examination_item_id);

comment on index ix_mrec_examination_site_item is '按检查项目读取检查部位';
