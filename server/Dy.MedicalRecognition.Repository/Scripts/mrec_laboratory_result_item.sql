-- postgresql schema
create table mrec_laboratory_result_item (
  id uuid not null,
  report_version_id uuid not null,
  source_detail_key text,
  source_project_name text not null,
  source_project_code text,
  standard_project_code text,
  source_result_text text not null,
  result_type integer not null,
  loinc_code text,
  unit text,
  reference_range text,
  testing_method text,
  instrument_code text,
  instrument_name text,
  display_order integer not null,
  abnormal_flag integer,
  critical_value_flag boolean,
  laboratory_charge_item_code text,
  insurance_charge_item_code text,
  inspector_id text,
  inspector_name text,
  oper_time timestamptz not null,
  oper_id uuid not null,
  primary key (id)
);

comment on table mrec_laboratory_result_item is '普通检验结果';
comment on column mrec_laboratory_result_item.id is '主键ID';
comment on column mrec_laboratory_result_item.report_version_id is '报告版本ID';
comment on column mrec_laboratory_result_item.source_detail_key is '来源明细标识';
comment on column mrec_laboratory_result_item.source_project_name is '来源项目名称';
comment on column mrec_laboratory_result_item.source_project_code is '来源项目编码';
comment on column mrec_laboratory_result_item.standard_project_code is '互认项目编码';
comment on column mrec_laboratory_result_item.source_result_text is '来源结果原文';
comment on column mrec_laboratory_result_item.result_type is '结果类型';
comment on column mrec_laboratory_result_item.loinc_code is 'LOINC编码';
comment on column mrec_laboratory_result_item.unit is '单位';
comment on column mrec_laboratory_result_item.reference_range is '参考范围';
comment on column mrec_laboratory_result_item.testing_method is '检测方法';
comment on column mrec_laboratory_result_item.instrument_code is '检测仪器编码';
comment on column mrec_laboratory_result_item.instrument_name is '检测仪器名称';
comment on column mrec_laboratory_result_item.display_order is '展示序号';
comment on column mrec_laboratory_result_item.abnormal_flag is '异常标志';
comment on column mrec_laboratory_result_item.critical_value_flag is '危急值标志';
comment on column mrec_laboratory_result_item.laboratory_charge_item_code is '检验收费项目编码';
comment on column mrec_laboratory_result_item.insurance_charge_item_code is '医保收费项目编码';
comment on column mrec_laboratory_result_item.inspector_id is '检测人ID';
comment on column mrec_laboratory_result_item.inspector_name is '检测人名称';
comment on column mrec_laboratory_result_item.oper_time is '操作时间';
comment on column mrec_laboratory_result_item.oper_id is '操作人';

create index ix_mrec_laboratory_result_item_version
on mrec_laboratory_result_item (report_version_id);

comment on index ix_mrec_laboratory_result_item_version is '按报告版本读取普通检验结果';
