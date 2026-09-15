-- postgresql schema
create table if not exists platform_patient (
  id uuid not null,
  identity_document_type_code varchar(100) not null,
  identity_document_no varchar(100) not null,
  patient_name varchar(100) not null,
  patient_gender_code varchar(100) not null,
  patient_birth_date timestamp without time zone not null,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
