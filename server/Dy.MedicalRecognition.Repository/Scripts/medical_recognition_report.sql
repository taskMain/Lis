-- postgresql schema
create table if not exists medical_recognition_report (
  id uuid not null,
  organization_code varchar(100) not null,
  hospital_code varchar(100) not null,
  branch_code varchar(100) not null,
  report_type integer not null,
  report_no varchar(100) not null,
  patient_id uuid not null,
  current_version_id uuid not null,
  status integer not null,
  voided_time timestamp without time zone,
  void_reason text,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
