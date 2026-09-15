-- postgresql schema
create table if not exists recognition_reference (
  id uuid not null,
  recognition_match_item_id uuid not null,
  standard_project_code varchar(100) not null,
  referenced_time timestamp without time zone not null,
  reference_dept_id varchar(100) not null,
  reference_dept_name varchar(100) not null,
  reference_doctor_id varchar(100) not null,
  reference_doctor_name varchar(100) not null,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
