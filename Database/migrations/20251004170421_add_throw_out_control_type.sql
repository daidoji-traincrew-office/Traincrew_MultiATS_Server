-- Create enum type "throw_out_control_type"
CREATE TYPE "throw_out_control_type" AS ENUM ('with_lever', 'without_lever', 'direction');
-- Modify "throw_out_control" table
ALTER TABLE "throw_out_control" ADD COLUMN "control_type" "throw_out_control_type" NOT NULL;
