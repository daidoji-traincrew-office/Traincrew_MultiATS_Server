-- Create enum type "operation_information_type"
CREATE TYPE "operation_information_type" AS ENUM ('advertisement', 'normal', 'delay', 'suspended');
-- Create "operation_information_state" table
CREATE TABLE "operation_information_state" ("id" bigserial NOT NULL, "type" "operation_information_type" NOT NULL, "content" text NOT NULL, "start_time" timestamp NOT NULL, "end_time" timestamp NOT NULL, PRIMARY KEY ("id"));
-- Create index "operation_information_state_end_time_index" to table: "operation_information_state"
CREATE INDEX "operation_information_state_end_time_index" ON "operation_information_state" ("end_time");
-- Create index "operation_information_state_start_time_index" to table: "operation_information_state"
CREATE INDEX "operation_information_state_start_time_index" ON "operation_information_state" ("start_time");
