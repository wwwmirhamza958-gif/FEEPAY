-- P1 migration stub: Academic, Billing, Integrations (Agent C)
-- Apply via EF migrations CLI or migrate tooling in CI — NEVER Database.Migrate() at startup.
-- Aligns with A's 001_p0_foundation_stub.sql style.
-- Money columns: numeric(18,2). Status values: CONTRACT §2.2 / §2.4 / §2.5 PascalCase.
-- Overdue is NEVER a stored invoice status — derive in API (is_overdue).

-- Academic
CREATE TABLE IF NOT EXISTS programs (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  code varchar(32) NOT NULL,
  name varchar(256) NOT NULL,
  external_id varchar(128) NULL,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, code)
);

CREATE TABLE IF NOT EXISTS intakes (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  program_id uuid NOT NULL REFERENCES programs(id),
  code varchar(64) NOT NULL,
  name varchar(256) NOT NULL,
  external_id varchar(128) NULL,
  year int NOT NULL,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, code)
);

CREATE TABLE IF NOT EXISTS terms (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  code varchar(64) NOT NULL,
  name varchar(256) NOT NULL,
  external_id varchar(128) NULL,
  start_date date NOT NULL,
  end_date date NOT NULL,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, code)
);

CREATE TABLE IF NOT EXISTS courses (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  code varchar(64) NOT NULL,
  name varchar(256) NOT NULL,
  external_id varchar(128) NULL,
  credit_hours numeric(18,2) NOT NULL,
  category varchar(64) NOT NULL,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, code)
);

CREATE TABLE IF NOT EXISTS students (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  external_id varchar(128) NOT NULL,
  display_name varchar(256) NOT NULL,
  program_id uuid NOT NULL REFERENCES programs(id),
  intake_id uuid NULL REFERENCES intakes(id),
  attributes_json text NOT NULL DEFAULT '{}',
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, external_id)
);

CREATE TABLE IF NOT EXISTS guardians (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  external_id varchar(128) NULL,
  display_name varchar(256) NOT NULL,
  mobile_e164 varchar(20) NOT NULL,
  created_at_utc timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS student_guardians (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  student_id uuid NOT NULL REFERENCES students(id),
  guardian_id uuid NOT NULL REFERENCES guardians(id),
  relation varchar(64) NOT NULL,
  verified boolean NOT NULL DEFAULT false,
  UNIQUE (tenant_id, student_id, guardian_id)
);

CREATE TABLE IF NOT EXISTS enrolments (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  student_id uuid NOT NULL REFERENCES students(id),
  course_id uuid NOT NULL REFERENCES courses(id),
  term_id uuid NOT NULL REFERENCES terms(id),
  is_repeat boolean NOT NULL DEFAULT false,
  status varchar(32) NOT NULL,
  added_date date NULL,
  dropped_date date NULL,
  external_id varchar(128) NULL,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, student_id, course_id, term_id)
);

-- Billing
CREATE TABLE IF NOT EXISTS instalment_plans (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  code varchar(64) NOT NULL,
  name varchar(256) NOT NULL,
  program_id uuid NULL,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, code)
);

CREATE TABLE IF NOT EXISTS instalment_plan_parts (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  instalment_plan_id uuid NOT NULL REFERENCES instalment_plans(id),
  sequence int NOT NULL,
  share numeric(18,4) NOT NULL,
  due_offset_days int NOT NULL,
  label varchar(128) NOT NULL
);

CREATE TABLE IF NOT EXISTS invoices (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  student_id uuid NOT NULL REFERENCES students(id),
  term_id uuid NOT NULL REFERENCES terms(id),
  version int NOT NULL,
  status varchar(32) NOT NULL, -- Draft|Issued|PartiallyPaid|Paid|Superseded
  total_amount numeric(18,2) NOT NULL,
  remaining_balance numeric(18,2) NOT NULL,
  currency varchar(3) NOT NULL DEFAULT 'PKR',
  due_date date NOT NULL,
  previous_invoice_id uuid NULL,
  adjustment_of_invoice_id uuid NULL,
  diff_json text NULL,
  reason text NULL,
  instalment_plan_id uuid NULL REFERENCES instalment_plans(id),
  created_at_utc timestamptz NOT NULL,
  issued_at_utc timestamptz NULL
);

CREATE TABLE IF NOT EXISTS invoice_lines (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  invoice_id uuid NOT NULL REFERENCES invoices(id),
  fee_head_code varchar(64) NOT NULL,
  amount numeric(18,2) NOT NULL,
  trace varchar(1024) NOT NULL,
  kind varchar(32) NOT NULL DEFAULT 'Charge',
  rule_ref varchar(128) NULL,
  sort_order int NOT NULL
);

CREATE TABLE IF NOT EXISTS instalments (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  invoice_id uuid NOT NULL REFERENCES invoices(id),
  sequence int NOT NULL,
  due_date date NOT NULL,
  amount numeric(18,2) NOT NULL,
  paid_amount numeric(18,2) NOT NULL DEFAULT 0,
  label varchar(128) NOT NULL
);

CREATE TABLE IF NOT EXISTS deferments (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  student_id uuid NOT NULL REFERENCES students(id),
  item_type varchar(64) NOT NULL,
  item_id uuid NOT NULL,
  status varchar(32) NOT NULL, -- Requested|Approved|Rejected|Withdrawn
  reason varchar(1024) NOT NULL,
  proposed_due_date date NOT NULL,
  approved_due_date date NULL,
  decided_by_user_id uuid NULL,
  decision_note text NULL,
  requested_at_utc timestamptz NOT NULL,
  decided_at_utc timestamptz NULL
);

CREATE TABLE IF NOT EXISTS student_credits (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  student_id uuid NOT NULL REFERENCES students(id),
  amount numeric(18,2) NOT NULL,
  currency varchar(3) NOT NULL DEFAULT 'PKR',
  source_note text NULL,
  source_attempt_id uuid NULL,
  created_at_utc timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS preview_runs (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  term_id uuid NOT NULL REFERENCES terms(id),
  student_count int NOT NULL,
  total_amount numeric(18,2) NOT NULL,
  outlier_count int NOT NULL,
  created_at_utc timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS preview_student_results (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  preview_run_id uuid NOT NULL REFERENCES preview_runs(id),
  student_id uuid NOT NULL REFERENCES students(id),
  total_amount numeric(18,2) NOT NULL,
  is_outlier boolean NOT NULL,
  outlier_reason text NULL,
  lines_json text NOT NULL
);

-- Integrations
CREATE TABLE IF NOT EXISTS import_jobs (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  entity varchar(64) NOT NULL,
  status varchar(32) NOT NULL, -- Uploaded|Validating|Validated|FailedValidation|Committing|Succeeded|Failed|Reverted
  original_file_name varchar(512) NOT NULL,
  content_type varchar(128) NOT NULL,
  raw_content text NOT NULL,
  column_mapping_json text NULL,
  created_count int NOT NULL DEFAULT 0,
  updated_count int NOT NULL DEFAULT 0,
  skipped_count int NOT NULL DEFAULT 0,
  error_count int NOT NULL DEFAULT 0,
  created_at_utc timestamptz NOT NULL,
  validated_at_utc timestamptz NULL,
  completed_at_utc timestamptz NULL
);

CREATE TABLE IF NOT EXISTS import_row_errors (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  import_job_id uuid NOT NULL REFERENCES import_jobs(id),
  row_number int NOT NULL,
  field varchar(128) NOT NULL,
  code varchar(64) NOT NULL,
  message varchar(1024) NOT NULL
);

CREATE TABLE IF NOT EXISTS field_mappings (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  entity varchar(64) NOT NULL,
  mapping_json text NOT NULL,
  updated_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, entity)
);

CREATE TABLE IF NOT EXISTS connector_configs (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  code varchar(64) NOT NULL,
  display_name varchar(256) NOT NULL,
  enabled boolean NOT NULL DEFAULT false,
  config_json text NOT NULL,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, code)
);

CREATE TABLE IF NOT EXISTS sync_runs (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  connector_config_id uuid NOT NULL REFERENCES connector_configs(id),
  direction varchar(16) NOT NULL,
  entity varchar(64) NOT NULL,
  created_count int NOT NULL DEFAULT 0,
  updated_count int NOT NULL DEFAULT 0,
  error_count int NOT NULL DEFAULT 0,
  error_summary text NULL,
  started_at_utc timestamptz NOT NULL,
  completed_at_utc timestamptz NULL
);

-- Example RLS (enable after app.tenant_id GUC helper exists — same as 001):
-- ALTER TABLE students ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;
-- CREATE POLICY tenant_isolation ON students
--   USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
