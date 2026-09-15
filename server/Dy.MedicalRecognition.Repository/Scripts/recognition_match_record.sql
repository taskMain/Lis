-- postgresql schema
create table if not exists recognition_match_record (
  id uuid not null,
  receiver_organization_code varchar(100) not null,
  receiver_hospital_code varchar(100) not null,
  receiver_branch_code varchar(100) not null,
  identity_document_type_code varchar(100) not null,
  identity_document_no varchar(100) not null,
  visit_type integer not null,
  visit_serial_no varchar(100) not null,
  match_created_time timestamp without time zone not null,
  decision_saved_time timestamp without time zone,
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
