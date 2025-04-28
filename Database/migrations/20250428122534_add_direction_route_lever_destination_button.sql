-- Modify "route_lever_destination_button" table
ALTER TABLE "route_lever_destination_button" ADD COLUMN "direction" "lr" NOT NULL;
-- Modify "route_lever_destination_button" table
ALTER TABLE "route_lever_destination_button" DROP CONSTRAINT "route_lever_destination_butto_lever_id_destination_button_n_key", ADD CONSTRAINT "route_lever_destination_butto_lever_id_destination_button_n_key" UNIQUE NULLS NOT DISTINCT ("lever_id", "destination_button_name", "direction");