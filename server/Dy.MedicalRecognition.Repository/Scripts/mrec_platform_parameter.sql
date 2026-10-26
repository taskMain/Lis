-- postgresql schema
create table mrec_platform_parameter (
  id uuid not null,
  parameter_code text not null,
  parameter_value text not null,
  description text,
  is_valid boolean not null,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_platform_parameter is '平台参数';
comment on column mrec_platform_parameter.id is '主键ID';
comment on column mrec_platform_parameter.parameter_code is '参数编码';
comment on column mrec_platform_parameter.parameter_value is '参数值';
comment on column mrec_platform_parameter.description is '参数说明';
comment on column mrec_platform_parameter.is_valid is '是否启用';
comment on column mrec_platform_parameter.oper_time is '操作时间';
comment on column mrec_platform_parameter.oper_id is '操作人';

create unique index ux_mrec_platform_parameter_code
on mrec_platform_parameter (parameter_code);

comment on index ux_mrec_platform_parameter_code is '平台参数编码唯一';
