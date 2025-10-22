-- Modify "route_state" table
ALTER TABLE "route_state" ADD COLUMN "is_throw_out_x_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', ADD COLUMN "is_throw_out_s_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop';
