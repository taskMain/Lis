-- postgresql schema
create table if not exists laboratory_antimicrobial_susceptibility (
  id uuid not null,
  bacteria_result_id uuid not null,
  source_detail_key varchar(100),
  drug_code varchar(100),
  drug_name varchar(100) not null,
  susceptibility_code varchar(100),
  source_conclusion_text text not null,
  resistance_result_code varchar(100),
  disk_content varchar(100),
  mic_value varchar(100),
  inhibition_zone_diameter varchar(100),
  reference_value varchar(100),
  display_order integer not null,
  inspector_id varchar(100),
  inspector_name varchar(100),
  testing_method varchar(100),
  test_panel_order varchar(100),
  oper_id uuid not null,
  oper_time timestamptz not null,
  primary key (id)
);
