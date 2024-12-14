-- Modify "lock_condition_execute" table
ALTER TABLE "lock_condition_execute" ALTER COLUMN "source_id" TYPE bigint, ALTER COLUMN "target_id" TYPE bigint, ADD CONSTRAINT "lock_condition_execute_source_id_target_id_key" UNIQUE ("source_id", "target_id");
-- Create index "lock_condition_execute_source_id_index" to table: "lock_condition_execute"
CREATE INDEX "lock_condition_execute_source_id_index" ON "lock_condition_execute" ("source_id");
-- Create enum type "route_type"
CREATE TYPE "route_type" AS ENUM ('arriving', 'departure', 'guide', 'switch_signal', 'switch_route');
-- Create enum type "lock_type"
CREATE TYPE "lock_type" AS ENUM ('lock', 'signal_control', 'detector', 'route', 'approach');
-- Create enum type "signal_indication"
CREATE TYPE "signal_indication" AS ENUM ('R', 'YY', 'Y', 'YG', 'G');
-- Create enum type "object_type"
CREATE TYPE "object_type" AS ENUM ('route', 'switching_machine', 'track_circuit');
-- Modify "station" table
ALTER TABLE "station" DROP CONSTRAINT "station_pkey", ADD COLUMN "id" character varying(10) NOT NULL, ADD PRIMARY KEY ("id"), ADD CONSTRAINT "station_name_key" UNIQUE ("name");
-- Create "interlocking_object" table
CREATE TABLE "interlocking_object" ("id" bigserial NOT NULL, "type" "object_type" NOT NULL, "name" character varying(100) NOT NULL, "station_id" character varying(10) NULL, "description" text NULL, PRIMARY KEY ("id"), CONSTRAINT "interlocking_object_station_id_name_key" UNIQUE ("station_id", "name"), CONSTRAINT "interlocking_object_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Modify "lock" table
ALTER TABLE "lock" ALTER COLUMN "id" TYPE bigint, DROP COLUMN "lever_id", ALTER COLUMN "type" TYPE "lock_type" USING "type"::"lock_type", ALTER COLUMN "type" SET NOT NULL, ADD COLUMN "object_id" bigint NULL, ADD CONSTRAINT "lock_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "lock_object_id_type_index" to table: "lock"
CREATE INDEX "lock_object_id_type_index" ON "lock" ("object_id", "type");
-- Modify "lock_condition" table
ALTER TABLE "lock_condition" ALTER COLUMN "id" TYPE bigint, DROP COLUMN "lever_id", DROP COLUMN "track_circuit_id", DROP COLUMN "condition_time_seconds", ADD COLUMN "lock_id" bigint NULL, ADD COLUMN "object_id" bigint NULL, ADD CONSTRAINT "lock_condition_lock_id_fkey" FOREIGN KEY ("lock_id") REFERENCES "lock" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "lock_condition_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "lock_condition_lock_id_index" to table: "lock_condition"
CREATE UNIQUE INDEX "lock_condition_lock_id_index" ON "lock_condition" ("lock_id");
-- Create "signal_type" table
CREATE TABLE "signal_type" ("name" character varying(100) NOT NULL, "r_indication" "signal_indication" NOT NULL, "yy_indication" "signal_indication" NOT NULL, "y_indication" "signal_indication" NOT NULL, "yg_indication" "signal_indication" NOT NULL, "g_indication" "signal_indication" NOT NULL, PRIMARY KEY ("name"));
-- Modify "signal" table
ALTER TABLE "signal" DROP COLUMN "next_signal_name", ALTER COLUMN "track_circuit_id" TYPE bigint, DROP COLUMN "lever_id", ADD COLUMN "type" character varying(100) NOT NULL, ADD CONSTRAINT "signal_type_fkey" FOREIGN KEY ("type") REFERENCES "signal_type" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create "next_signal" table
CREATE TABLE "next_signal" ("signal_name" character varying(100) NOT NULL, "next_signal_name" character varying(100) NOT NULL, CONSTRAINT "next_signal_signal_name_next_signal_name_key" UNIQUE ("signal_name", "next_signal_name"), CONSTRAINT "next_signal_next_signal_name_fkey" FOREIGN KEY ("next_signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "next_signal_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "next_signal_signal_name_index" to table: "next_signal"
CREATE INDEX "next_signal_signal_name_index" ON "next_signal" ("signal_name");
-- Create "route" table
CREATE TABLE "route" ("id" bigint NOT NULL, "tc_name" character varying(100) NOT NULL, "route_type" "route_type" NOT NULL, "root" character varying(100) NULL, "indicator" character varying(10) NULL, "approach_lock_time" integer NULL, PRIMARY KEY ("id"), CONSTRAINT "route_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "route_include" table
CREATE TABLE "route_include" ("source_lever_id" bigint NOT NULL, "target_lever_id" bigint NOT NULL, CONSTRAINT "route_include_source_lever_id_target_lever_id_key" UNIQUE ("source_lever_id", "target_lever_id"), CONSTRAINT "route_include_source_lever_id_fkey" FOREIGN KEY ("source_lever_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "route_include_target_lever_id_fkey" FOREIGN KEY ("target_lever_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "route_include_source_lever_id_index" to table: "route_include"
CREATE INDEX "route_include_source_lever_id_index" ON "route_include" ("source_lever_id");
-- Create "route_lock_state" table
CREATE TABLE "route_lock_state" ("id" bigserial NOT NULL, "target_route_id" bigint NOT NULL, "source_route_id" bigint NOT NULL, "lock_type" "lock_type" NOT NULL, "end_time" timestamp NULL, PRIMARY KEY ("id"), CONSTRAINT "route_lock_state_source_route_id_fkey" FOREIGN KEY ("source_route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "route_lock_state_target_route_id_fkey" FOREIGN KEY ("target_route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "route_lock_state_target_route_id_index" to table: "route_lock_state"
CREATE INDEX "route_lock_state_target_route_id_index" ON "route_lock_state" ("target_route_id");
-- Create "route_state" table
CREATE TABLE "route_state" ("id" bigint NOT NULL, "is_lever_reversed" boolean NOT NULL, "is_reversed" boolean NOT NULL, "should_reverse" boolean NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "route_state_id_fkey" FOREIGN KEY ("id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "signal_route" table
CREATE TABLE "signal_route" ("signal_name" character varying(100) NOT NULL, "route_id" bigint NOT NULL, CONSTRAINT "signal_route_signal_name_route_id_key" UNIQUE ("signal_name", "route_id"), CONSTRAINT "signal_route_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "signal_route_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "signal_route_signal_name_index" to table: "signal_route"
CREATE INDEX "signal_route_signal_name_index" ON "signal_route" ("signal_name");
-- Create "signal_state" table
CREATE TABLE "signal_state" ("signal_name" character varying(100) NOT NULL, "is_lighted" boolean NOT NULL, PRIMARY KEY ("signal_name"), CONSTRAINT "signal_state_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "switching_machine" table
CREATE TABLE "switching_machine" ("id" bigint NOT NULL, "tc_name" character varying(100) NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "switching_machine_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "switching_machine_state" table
CREATE TABLE "switching_machine_state" ("id" bigint NOT NULL, "is_reversed" boolean NOT NULL, "is_lever_reversed" boolean NULL, "switch_end_time" timestamp NULL, PRIMARY KEY ("id"), CONSTRAINT "switching_machine_state_id_fkey" FOREIGN KEY ("id") REFERENCES "switching_machine" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Modify "track_circuit" table
ALTER TABLE "track_circuit" ALTER COLUMN "id" DROP DEFAULT, ALTER COLUMN "id" TYPE bigint, DROP COLUMN "station", DROP COLUMN "name", ADD CONSTRAINT "track_circuit_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Drop sequence used by serial column "id"
DROP SEQUENCE IF EXISTS "track_circuit_id_seq";
-- Create "track_circuit_state" table
CREATE TABLE "track_circuit_state" ("id" bigint NOT NULL, "train_number" character varying(100) NULL, "is_short_circuit" boolean NULL, PRIMARY KEY ("id"), CONSTRAINT "track_circuit_state_id_fkey" FOREIGN KEY ("id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Drop "lever_include" table
DROP TABLE "lever_include";
-- Drop "lever" table
DROP TABLE "lever";
