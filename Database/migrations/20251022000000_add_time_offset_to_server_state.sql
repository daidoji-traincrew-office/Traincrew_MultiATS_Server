-- Modify "server_state" table
ALTER TABLE "server_state" ADD COLUMN "time_offset" integer NOT NULL DEFAULT 0;
