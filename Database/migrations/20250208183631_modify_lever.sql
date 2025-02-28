-- Add value to enum type: "object_type"
ALTER TYPE "object_type" ADD VALUE 'lever';
-- Modify "route_lever_destination_button" table
ALTER TABLE "route_lever_destination_button" DROP COLUMN "lever_name";
-- Modify "lever" table
ALTER TABLE "lever" DROP COLUMN "name", DROP COLUMN "station_id";
-- Rename a column from "type" to "lever_type"
ALTER TABLE "lever" RENAME COLUMN "type" TO "lever_type";
-- Modify "route_lever_destination_button" table
ALTER TABLE "route_lever_destination_button" ADD COLUMN "lever_id" bigint NOT NULL, ADD CONSTRAINT "route_lever_destination_butto_lever_id_destination_button_n_key" UNIQUE ("lever_id", "destination_button_name"), ADD CONSTRAINT "route_lever_destination_button_lever_id_fkey" FOREIGN KEY ("lever_id") REFERENCES "lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
