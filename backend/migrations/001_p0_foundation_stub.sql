-- P0 migration stub: Identity, Tenancy, Outbox, Audit
-- Apply via EF migrations CLI or migrate tooling in CI — NEVER Database.Migrate() at startup.
--
-- RLS notes (§5.3):
--   1. Application sets per-connection: SET app.tenant_id = '<uuid>';
--   2. Policies: USING (tenant_id = current_setting('app.tenant_id')::uuid)
--   3. Platform-admin path uses a separate role / audited bypass (FORCE ROW LEVEL SECURITY on).
--   4. EF global query filter is the first line of defence; RLS is the second.
--
-- TODO: generate EF Core migration once Postgres is available locally.
-- Placeholder DDL for review:

CREATE TABLE IF NOT EXISTS tenants (
  id uuid PRIMARY KEY,
  code varchar(64) NOT NULL UNIQUE,
  name varchar(256) NOT NULL,
  is_active boolean NOT NULL DEFAULT true,
  created_at_utc timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS tenant_brandings (
  tenant_id uuid PRIMARY KEY REFERENCES tenants(id),
  logo_object_key text,
  brand_colour_hex varchar(16) NOT NULL
);

CREATE TABLE IF NOT EXISTS users (
  id uuid PRIMARY KEY,
  tenant_id uuid NOT NULL REFERENCES tenants(id),
  email varchar(320) NOT NULL,
  password_hash text NOT NULL,
  display_name text NOT NULL,
  is_active boolean NOT NULL DEFAULT true,
  totp_secret_encrypted text,
  totp_enabled boolean NOT NULL DEFAULT false,
  created_at_utc timestamptz NOT NULL,
  UNIQUE (tenant_id, email)
);

CREATE TABLE IF NOT EXISTS otp_challenges (
  id uuid PRIMARY KEY,
  tenant_id uuid NULL,
  mobile_e164 varchar(20) NOT NULL,
  code_hash text NOT NULL,
  expires_at_utc timestamptz NOT NULL,
  consumed_at_utc timestamptz NULL,
  attempt_count int NOT NULL DEFAULT 0,
  ip_address text NULL,
  device_fingerprint text NULL,
  created_at_utc timestamptz NOT NULL
);

CREATE TABLE IF NOT EXISTS outbox_messages (
  id uuid PRIMARY KEY,
  tenant_id uuid NULL,
  event_name varchar(200) NOT NULL,
  payload_json text NOT NULL,
  occurred_at_utc timestamptz NOT NULL,
  processed_at_utc timestamptz NULL,
  last_error text NULL,
  attempt_count int NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS audit_events (
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  tenant_id uuid NULL,
  action varchar(128) NOT NULL,
  actor_type varchar(64) NOT NULL,
  actor_id uuid NULL,
  entity_type varchar(128) NOT NULL,
  entity_id text NULL,
  payload_json text NOT NULL,
  occurred_at_utc timestamptz NOT NULL,
  trace_id text NULL
);

-- Append-only guard (stub): revoke UPDATE/DELETE on audit_events from app role once roles exist.
-- Example RLS (enable after app.tenant_id GUC helper exists):
-- ALTER TABLE users ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE users FORCE ROW LEVEL SECURITY;
-- CREATE POLICY tenant_isolation ON users
--   USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
