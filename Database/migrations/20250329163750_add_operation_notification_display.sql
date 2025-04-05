-- Create enum type "operation_notification_type"
CREATE TYPE "operation_notification_type" AS ENUM ('none', 'yokushi', 'tsuuchi', 'tsuuchi_kaijo', 'kaijo', 'shuppatsu', 'shuppatsu_jikoku', 'torikeshi', 'tenmatsusho');
-- Modify "route_state" table
ALTER TABLE "route_state" ALTER COLUMN "is_throw_out_xr_relay_raised" DROP DEFAULT, ALTER COLUMN "is_throw_out_ys_relay_raised" DROP DEFAULT;
-- Create "operation_notification_display" table
CREATE TABLE "operation_notification_display" ("name" character varying(100) NOT NULL, "station_id" character varying(10) NOT NULL, "is_up" boolean NOT NULL, "is_down" boolean NOT NULL, PRIMARY KEY ("name"), CONSTRAINT "operation_notification_display_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "operation_notification_state" table
CREATE TABLE "operation_notification_state" ("display_name" character varying(100) NOT NULL, "type" "operation_notification_type" NOT NULL, "content" text NOT NULL, "operated_at" timestamp NOT NULL, PRIMARY KEY ("display_name"), CONSTRAINT "operation_notification_state_display_name_fkey" FOREIGN KEY ("display_name") REFERENCES "operation_notification_display" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Modify "track_circuit" table
ALTER TABLE "track_circuit" ADD COLUMN "operation_notification_display_name" character varying(100) NULL, ADD CONSTRAINT "track_circuit_operation_notification_display_name_fkey" FOREIGN KEY ("operation_notification_display_name") REFERENCES "operation_notification_display" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "track_circuit_operation_notification_display_name_index" to table: "track_circuit"
CREATE INDEX "track_circuit_operation_notification_display_name_index" ON "track_circuit" ("operation_notification_display_name");
