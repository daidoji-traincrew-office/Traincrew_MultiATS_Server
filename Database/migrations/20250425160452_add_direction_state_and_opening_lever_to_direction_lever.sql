-- Modify "direction_lever_state" table
ALTER TABLE "direction_lever_state" ADD COLUMN "is_lr" "lr" NOT NULL DEFAULT 'left';
-- Modify "direction_lever" table
ALTER TABLE "direction_lever" ADD COLUMN "opening_lever_id" bigint NULL, ADD CONSTRAINT "direction_lever_opening_lever_id_fkey" FOREIGN KEY ("opening_lever_id") REFERENCES "opening_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
