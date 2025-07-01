-- Modify "route_state" table
ALTER TABLE "route_state" ADD COLUMN "is_route_relay_without_switching_machine_raised" "raise_drop" NOT NULL DEFAULT 'drop';
