-- Add value to enum type: "lever_type"
ALTER TYPE "lever_type" ADD VALUE 'direction_lever';
-- Modify "direction_route" table
ALTER TABLE "direction_route" ADD COLUMN "lever_id" bigint NOT NULL, ADD CONSTRAINT "direction_route_lever_id_fkey" FOREIGN KEY ("lever_id") REFERENCES "lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
