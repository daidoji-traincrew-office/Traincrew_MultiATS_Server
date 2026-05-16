-- Modify "diagram" table
ALTER TABLE "diagram" DROP COLUMN "time_range", DROP COLUMN "index", ADD CONSTRAINT "diagram_name_key" UNIQUE ("name");
