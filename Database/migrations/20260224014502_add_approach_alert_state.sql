-- Create "approach_alert_state" table
CREATE TABLE "approach_alert_state" (
  "id" bigserial NOT NULL,
  "station_id" character varying(10) NOT NULL,
  "is_up" boolean NOT NULL,
  "should_ring" boolean NOT NULL DEFAULT false,
  "is_ringing" boolean NOT NULL DEFAULT false,
  PRIMARY KEY ("id"),
  CONSTRAINT "approach_alert_state_station_id_is_up_key" UNIQUE ("station_id", "is_up"),
  CONSTRAINT "approach_alert_state_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION
);
