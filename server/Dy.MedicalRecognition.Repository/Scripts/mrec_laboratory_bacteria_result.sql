-- postgresql schema
create table mrec_laboratory_bacteria_result (
  id uuid not null,
  report_version_id uuid not null,
  source_detail_key text,
  source_organism_code text,
  source_organism_name text,
  source_result_text text not null,
  detection_conclusion text not null,
  colony_count text,
  culture_medium text,
  culture_time text,
  culture_condition text,
  discovery_method text,
  detection_method text,
  description text,
  instrument_code text,
  instrument_name text,
  test_panel_code text,
  test_panel_name text,
  inspector_id text,
  inspector_name text,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_laboratory_bacteria_result is '细菌鉴定结果';
comment on column mrec_laboratory_bacteria_result.id is '主键ID';
comment on column mrec_laboratory_bacteria_result.report_version_id is '报告版本ID';
comment on column mrec_laboratory_bacteria_result.source_detail_key is '来源明细标识';
comment on column mrec_laboratory_bacteria_result.source_organism_code is '来源菌种编码';
comment on column mrec_laboratory_bacteria_result.source_organism_name is '来源菌种名称';
comment on column mrec_laboratory_bacteria_result.source_result_text is '来源结果原文';
comment on column mrec_laboratory_bacteria_result.detection_conclusion is '检测结论';
comment on column mrec_laboratory_bacteria_result.colony_count is '菌落计数';
comment on column mrec_laboratory_bacteria_result.culture_medium is '培养基';
comment on column mrec_laboratory_bacteria_result.culture_time is '培养时间';
comment on column mrec_laboratory_bacteria_result.culture_condition is '培养条件';
comment on column mrec_laboratory_bacteria_result.discovery_method is '发现方式';
comment on column mrec_laboratory_bacteria_result.detection_method is '检测方法';
comment on column mrec_laboratory_bacteria_result.description is '详细描述';
comment on column mrec_laboratory_bacteria_result.instrument_code is '检测仪器编码';
comment on column mrec_laboratory_bacteria_result.instrument_name is '检测仪器名称';
comment on column mrec_laboratory_bacteria_result.test_panel_code is '试验板编码';
comment on column mrec_laboratory_bacteria_result.test_panel_name is '试验板名称';
comment on column mrec_laboratory_bacteria_result.inspector_id is '检测人ID';
comment on column mrec_laboratory_bacteria_result.inspector_name is '检测人名称';
comment on column mrec_laboratory_bacteria_result.oper_time is '操作时间';
comment on column mrec_laboratory_bacteria_result.oper_id is '操作人';

create index ix_mrec_laboratory_bacteria_result_version
on mrec_laboratory_bacteria_result (report_version_id);

comment on index ix_mrec_laboratory_bacteria_result_version is '按报告版本读取细菌鉴定结果';
