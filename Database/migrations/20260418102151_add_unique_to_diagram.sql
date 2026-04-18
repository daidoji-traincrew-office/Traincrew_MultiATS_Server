-- Modify "diagram" table
ALTER TABLE "diagram" ADD CONSTRAINT "diagram_name_time_range_key" UNIQUE ("name", "time_range");
