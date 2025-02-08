-- Create enum type "object_type"
CREATE TYPE "object_type" AS ENUM ('route', 'switching_machine', 'track_circuit');
-- Create enum type "route_type"
CREATE TYPE "route_type" AS ENUM ('arriving', 'departure', 'guide', 'switch_signal', 'switch_route');
-- Create enum type "lock_type"
CREATE TYPE "lock_type" AS ENUM ('lock', 'signal_control', 'detector', 'route', 'approach');
-- Create enum type "signal_indication"
CREATE TYPE "signal_indication" AS ENUM ('R', 'YY', 'Y', 'YG', 'G');
-- Create "station" table
CREATE TABLE "station" ("id" character varying(10) NOT NULL, "name" character varying(100) NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "station_name_key" UNIQUE ("name"));
-- Create "interlocking_object" table
CREATE TABLE "interlocking_object" ("id" bigserial NOT NULL, "type" "object_type" NOT NULL, "name" character varying(100) NOT NULL, "station_id" character varying(10) NULL, "description" text NULL, PRIMARY KEY ("id"), CONSTRAINT "interlocking_object_station_id_name_key" UNIQUE ("station_id", "name"), CONSTRAINT "interlocking_object_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "lock" table
CREATE TABLE "lock" ("id" bigserial NOT NULL, "object_id" bigint NULL, "type" "lock_type" NOT NULL, "route_lock_group" integer NULL, "or_condition_group" integer NULL, PRIMARY KEY ("id"), CONSTRAINT "lock_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "lock_object_id_type_index" to table: "lock"
CREATE INDEX "lock_object_id_type_index" ON "lock" ("object_id", "type");
-- Create "lock_condition" table
CREATE TABLE "lock_condition" ("id" bigserial NOT NULL, "lock_id" bigint NULL, "type" character varying(50) NOT NULL, "object_id" bigint NULL, "timer_seconds" integer NULL, "is_reverse" boolean NOT NULL, "is_total_control" boolean NOT NULL, "is_single_lock" boolean NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "lock_condition_lock_id_fkey" FOREIGN KEY ("lock_id") REFERENCES "lock" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "lock_condition_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "lock_condition_lock_id_index" to table: "lock_condition"
CREATE UNIQUE INDEX "lock_condition_lock_id_index" ON "lock_condition" ("lock_id");
-- Create "lock_condition_execute" table
CREATE TABLE "lock_condition_execute" ("source_id" bigint NOT NULL, "target_id" bigint NOT NULL, CONSTRAINT "lock_condition_execute_source_id_target_id_key" UNIQUE ("source_id", "target_id"), CONSTRAINT "lock_condition_execute_source_id_fkey" FOREIGN KEY ("source_id") REFERENCES "lock_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "lock_condition_execute_target_id_fkey" FOREIGN KEY ("target_id") REFERENCES "lock_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "lock_condition_execute_source_id_index" to table: "lock_condition_execute"
CREATE INDEX "lock_condition_execute_source_id_index" ON "lock_condition_execute" ("source_id");
-- Create "lock_state" table
CREATE TABLE "lock_state" ("id" bigserial NOT NULL, "target_route_id" bigint NOT NULL, "source_route_id" bigint NOT NULL, "lock_type" "lock_type" NOT NULL, "end_time" timestamp NULL, PRIMARY KEY ("id"), CONSTRAINT "lock_state_source_route_id_fkey" FOREIGN KEY ("source_route_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "lock_state_target_route_id_fkey" FOREIGN KEY ("target_route_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "lock_state_target_route_id_index" to table: "lock_state"
CREATE INDEX "lock_state_target_route_id_index" ON "lock_state" ("target_route_id");
-- Create "track_circuit" table
CREATE TABLE "track_circuit" ("id" bigint NOT NULL, "protection_zone" integer NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "track_circuit_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "signal_type" table
CREATE TABLE "signal_type" ("name" character varying(100) NOT NULL, "r_indication" "signal_indication" NOT NULL, "yy_indication" "signal_indication" NOT NULL, "y_indication" "signal_indication" NOT NULL, "yg_indication" "signal_indication" NOT NULL, "g_indication" "signal_indication" NOT NULL, PRIMARY KEY ("name"));
-- Create "signal" table
CREATE TABLE "signal" ("name" character varying(100) NOT NULL, "type" character varying(100) NOT NULL, "track_circuit_id" bigint NULL, PRIMARY KEY ("name"), CONSTRAINT "signal_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "signal_type_fkey" FOREIGN KEY ("type") REFERENCES "signal_type" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
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
CREATE TABLE "switching_machine_state" ("id" bigint NOT NULL, "is_reverse" boolean NOT NULL, "is_lever_reversed" boolean NULL, "switch_end_time" timestamp NULL, PRIMARY KEY ("id"), CONSTRAINT "switching_machine_state_id_fkey" FOREIGN KEY ("id") REFERENCES "switching_machine" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "track_circuit_state" table
CREATE TABLE "track_circuit_state" ("id" bigint NOT NULL, "train_number" character varying(100) NULL, "is_short_circuit" boolean NULL, PRIMARY KEY ("id"), CONSTRAINT "track_circuit_state_id_fkey" FOREIGN KEY ("id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
