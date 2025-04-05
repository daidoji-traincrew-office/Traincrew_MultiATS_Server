-- Modify "route_state" table
ALTER TABLE "route_state" ADD COLUMN "is_approach_lock_mr_raised" "raise_drop" NOT NULL DEFAULT 'drop', ADD COLUMN "is_approach_lock_ms_raised" "raise_drop" NOT NULL DEFAULT 'drop', DROP COLUMN "is_approach_lock_raised";
