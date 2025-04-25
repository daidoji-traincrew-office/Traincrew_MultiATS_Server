-- Modify "direction_lever" table
ALTER TABLE "direction_lever" ADD COLUMN "l_lock_lever_direction" "lr" NULL, ADD COLUMN "l_single_locked_lever_direction" "lr" NULL, ADD COLUMN "r_lock_lever_direction" "lr" NULL, ADD COLUMN "r_single_locked_lever_direction" "lr" NULL;
-- Modify "lock_condition_object" table
ALTER TABLE "lock_condition_object" ADD COLUMN "is_lr" "lr" NOT NULL;
-- Drop "direction_lever_direction" table
DROP TABLE "direction_lever_direction";
-- Drop "opening_lever_direction" table
DROP TABLE "opening_lever_direction";
