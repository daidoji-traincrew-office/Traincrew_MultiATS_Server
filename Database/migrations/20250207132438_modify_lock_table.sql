-- Create enum type "raise_drop"
CREATE TYPE "raise_drop" AS ENUM ('raise', 'drop');
-- Modify "route_state" table
ALTER TABLE "route_state" DROP COLUMN "is_lever_reversed", DROP COLUMN "is_reversed", DROP COLUMN "should_reverse", ADD COLUMN "is_lever_relay_raised" "raise_drop" NOT NULL, ADD COLUMN "is_route_relay_raised" "raise_drop" NOT NULL, ADD COLUMN "is_signal_control_raised" "raise_drop" NOT NULL, ADD COLUMN "is_approach_lock_raised" "raise_drop" NOT NULL, ADD COLUMN "is_route_lock_raised" "raise_drop" NOT NULL;
-- Create enum type "lock_condition_type"
CREATE TYPE "lock_condition_type" AS ENUM ('and', 'or', 'object');
-- Modify "lock" table
ALTER TABLE "lock" DROP COLUMN "or_condition_group";
-- Modify "track_circuit_state" table
ALTER TABLE "track_circuit_state" ADD COLUMN "is_locked" boolean NOT NULL DEFAULT FALSE;
ALTER TABLE "track_circuit_state" ALTER COLUMN "is_locked" DROP DEFAULT;
-- Create enum type "lever_type"
CREATE TYPE "lever_type" AS ENUM ('route', 'switching_machine');
-- Create "destination_button" table
CREATE TABLE "destination_button" ("name" character varying(100) NOT NULL, "station_id" character varying(10) NOT NULL, PRIMARY KEY ("name"), CONSTRAINT "destination_button_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "destination_button_state" table
CREATE TABLE "destination_button_state" ("name" character varying(100) NOT NULL, "is_raised" "raise_drop" NOT NULL, "operated_at" timestamp NOT NULL, PRIMARY KEY ("name"), CONSTRAINT "destination_button_state_name_fkey" FOREIGN KEY ("name") REFERENCES "destination_button" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "lever" table
CREATE TABLE "lever" ("id" bigint NOT NULL, "name" character varying(100) NOT NULL, "station_id" character varying(10) NOT NULL, "type" "lever_type" NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "lever_name_key" UNIQUE ("name"), CONSTRAINT "lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "lever_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "lever_state" table
CREATE TABLE "lever_state" ("id" bigint NOT NULL, "is_reversed" "nrc" NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Drop index "lock_condition_lock_id_index" from table: "lock_condition"
DROP INDEX "lock_condition_lock_id_index";
-- Modify "lock_condition" table
ALTER TABLE "lock_condition" DROP COLUMN "object_id", ADD COLUMN "parent_id" BIGINT, ALTER COLUMN "lock_id" SET NOT NULL, ALTER COLUMN "type" TYPE "lock_condition_type" USING "type"::"lock_condition_type", DROP COLUMN "timer_seconds", DROP COLUMN "is_reverse", DROP COLUMN "is_total_control", DROP COLUMN "is_single_lock", ADD CONSTRAINT "lock_condition_parent_id_fkey" FOREIGN KEY ("parent_id") REFERENCES "lock_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "lock_condition_lock_id_index" to table: "lock_condition"
CREATE INDEX "lock_condition_lock_id_index" ON "lock_condition" ("lock_id");
-- Create "lock_condition_object" table
CREATE TABLE "lock_condition_object" ("lock_condition_id" bigint NOT NULL, "object_id" bigint NOT NULL, "timer_seconds" integer NULL, "is_reverse" "nr" NOT NULL, "is_single_lock" boolean NOT NULL, PRIMARY KEY ("lock_condition_id"), CONSTRAINT "lock_condition_object_lock_condition_id_fkey" FOREIGN KEY ("lock_condition_id") REFERENCES "lock_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "lock_condition_object_object_id_fkey" FOREIGN KEY ("object_id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "route_lever_destination_button" table
CREATE TABLE "route_lever_destination_button" ("id" bigserial NOT NULL, "route_id" bigint NOT NULL, "lever_name" character varying(100) NOT NULL, "destination_button_name" character varying(100) NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "route_lever_destination_butto_lever_name_destination_button_key" UNIQUE ("lever_name", "destination_button_name"), CONSTRAINT "route_lever_destination_button_route_id_key" UNIQUE ("route_id"), CONSTRAINT "route_lever_destination_button_destination_button_name_fkey" FOREIGN KEY ("destination_button_name") REFERENCES "destination_button" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "route_lever_destination_button_lever_name_fkey" FOREIGN KEY ("lever_name") REFERENCES "lever" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "route_lever_destination_button_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "total_control" table
CREATE TABLE "total_control" ("id" bigserial NOT NULL, "source_route_id" bigint NOT NULL, "target_route_id" bigint NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "total_control_source_route_id_key" UNIQUE ("source_route_id"), CONSTRAINT "total_control_target_route_id_key" UNIQUE ("target_route_id"), CONSTRAINT "total_control_source_route_id_fkey" FOREIGN KEY ("source_route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "total_control_target_route_id_fkey" FOREIGN KEY ("target_route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Drop "lock_condition_execute" table
DROP TABLE "lock_condition_execute";

