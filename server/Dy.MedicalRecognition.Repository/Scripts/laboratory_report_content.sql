-- postgresql schema
create table if not exists laboratory_report_content (
  id uuid not null,
  report_version_id uuid not null,
  report_category_code varchar(100),
  report_category_name varchar(100),
  report_remark text,
  overall_abnormal_flag varchar(100),
  source_order_serial_no varchar(100),
  specimen_collected_time timestamp without time zone,
  specimen_submitted_time timestamp without time zone,
  laboratory_received_time timestamp without time zone,
  source_specimen_no varchar(100) not null,
  specimen_type_code varchar(100) not null,
  specimen_type_name varchar(100) not null,
  testing_completed_time timestamp without time zone not null,
  inspector_id varchar(100) not null,
  inspector_name varchar(100) not null,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
