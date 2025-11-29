-- Modify "route_central_control_lever_state" table
ALTER TABLE "route_central_control_lever_state" ADD COLUMN "is_chr_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop';
