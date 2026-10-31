-- Create enum type "stop_type"
CREATE TYPE "stop_type" AS ENUM ('stop', 'operational_stop', 'pass');
-- Modify "diagram_train_timetable" table
ALTER TABLE "diagram_train_timetable" ADD COLUMN "stop_type" "stop_type" NOT NULL DEFAULT 'stop';
