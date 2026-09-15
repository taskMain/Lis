-- postgresql schema
create table if not exists recognition_processing_result (
  id uuid not null,
  recognition_match_record_id uuid not null,
  recognition_match_item_id uuid not null,
  standard_project_code varchar(100) not null,
  recognition_time timestamp without time zone not null,
  recognition_result integer not null,
  recognition_dept_id varchar(100) not null,
  recognition_dept_name varchar(100) not null,
  recognition_doctor_id varchar(100) not null,
  recognition_doctor_name varchar(100) not null,
  non_adoption_reason integer,
  non_adoption_description text,
  estimated_saving_amount numeric(18,2),
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
