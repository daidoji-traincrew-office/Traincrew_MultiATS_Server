-- Create enum type "both_odd_even"
CREATE TYPE "both_odd_even" AS ENUM ('both', 'odd', 'even');
-- Create "approach_alert_condition" table
CREATE TABLE "approach_alert_condition" (
  "id" bigserial NOT NULL,
  "station_id" character varying(10) NOT NULL,
  "is_up" boolean NOT NULL,
  "track_circuit_id" bigint NOT NULL,
  "train_number_condition" "both_odd_even" NOT NULL DEFAULT 'both',
  PRIMARY KEY ("id"),
  CONSTRAINT "approach_alert_condition_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION,
  CONSTRAINT "approach_alert_condition_track_circuit_id_fkey" FOREIGN KEY ("track_circuit_id") REFERENCES "track_circuit" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
-- Create index "approach_alert_condition_station_id_is_up_index" to table: "approach_alert_condition"
CREATE INDEX "approach_alert_condition_station_id_is_up_index" ON "approach_alert_condition" ("station_id", "is_up");
-- Modify "lock_condition" table
ALTER TABLE "lock_condition" ALTER COLUMN "lock_id" DROP NOT NULL, ADD COLUMN "approach_alert_condition_id" bigint NULL, ADD CONSTRAINT "lock_condition_approach_alert_condition_id_fkey" FOREIGN KEY ("approach_alert_condition_id") REFERENCES "approach_alert_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "lock_condition_approach_alert_condition_id_index" to table: "lock_condition"
CREATE INDEX "lock_condition_approach_alert_condition_id_index" ON "lock_condition" ("approach_alert_condition_id");
