-- Modify "route_state" table
ALTER TABLE "route_state" ADD COLUMN "is_ctc_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop';
