-- Create enum type "lr"
CREATE TYPE "lr" AS ENUM ('left', 'right');
-- Create "direction_lever" table
CREATE TABLE "direction_lever" ("id" bigint NOT NULL, "l_lock_lever_id" bigint NULL, "l_single_locked_lever_id" bigint NULL, "r_lock_lever_id" bigint NULL, "r_single_locked_lever_id" bigint NULL, PRIMARY KEY ("id"));
-- Create "direction_lever_direction" table
CREATE TABLE "direction_lever_direction" ("id" bigint NOT NULL, "lever_id" bigint NOT NULL, "is_lr" "lr" NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "direction_lever_direction_lever_id_is_lr_key" UNIQUE ("lever_id", "is_lr"));
-- Create "direction_lever_state" table
CREATE TABLE "direction_lever_state" ("id" bigint NOT NULL, "is_fl_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_lfys_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_rfys_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_ly_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_ry_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_l_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_r_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', PRIMARY KEY ("id"));
-- Create "opening_lever" table
CREATE TABLE "opening_lever" ("id" bigint NOT NULL, PRIMARY KEY ("id"));
-- Create "opening_lever_direction" table
CREATE TABLE "opening_lever_direction" ("id" bigint NOT NULL, "lever_id" bigint NOT NULL, "is_nr" "nr" NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "opening_lever_direction_lever_id_is_nr_key" UNIQUE ("lever_id", "is_nr"));
-- Create "opening_lever_state" table
CREATE TABLE "opening_lever_state" ("id" bigint NOT NULL, "is_inserted_key" boolean NOT NULL DEFAULT false, "is_reversed" "nr" NOT NULL DEFAULT 'normal', PRIMARY KEY ("id"));
-- Modify "direction_lever" table
ALTER TABLE "direction_lever" ADD CONSTRAINT "direction_lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "direction_lever_l_lock_lever_id_fkey" FOREIGN KEY ("l_lock_lever_id") REFERENCES "direction_lever_direction" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "direction_lever_l_single_locked_lever_id_fkey" FOREIGN KEY ("l_single_locked_lever_id") REFERENCES "direction_lever_direction" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "direction_lever_r_lock_lever_id_fkey" FOREIGN KEY ("r_lock_lever_id") REFERENCES "direction_lever_direction" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "direction_lever_r_single_locked_lever_id_fkey" FOREIGN KEY ("r_single_locked_lever_id") REFERENCES "direction_lever_direction" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Modify "direction_lever_direction" table
ALTER TABLE "direction_lever_direction" ADD CONSTRAINT "direction_lever_direction_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "direction_lever_direction_lever_id_fkey" FOREIGN KEY ("lever_id") REFERENCES "direction_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Modify "direction_lever_state" table
ALTER TABLE "direction_lever_state" ADD CONSTRAINT "direction_lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "direction_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Modify "opening_lever" table
ALTER TABLE "opening_lever" ADD CONSTRAINT "opening_lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Modify "opening_lever_direction" table
ALTER TABLE "opening_lever_direction" ADD CONSTRAINT "opening_lever_direction_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, ADD CONSTRAINT "opening_lever_direction_lever_id_fkey" FOREIGN KEY ("lever_id") REFERENCES "opening_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Modify "opening_lever_state" table
ALTER TABLE "opening_lever_state" ADD CONSTRAINT "opening_lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "opening_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
