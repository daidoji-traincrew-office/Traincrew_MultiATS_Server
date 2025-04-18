-- Modify "route_state" table
ALTER TABLE "route_state" ALTER COLUMN "is_approach_lock_mr_raised" DROP DEFAULT, ALTER COLUMN "is_approach_lock_ms_raised" DROP DEFAULT;
-- Create "station_timer_state" table
CREATE TABLE "station_timer_state" ("id" bigserial NOT NULL, "station_id" character varying(10) NOT NULL, "seconds" integer NOT NULL, "is_teu_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_ten_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', "is_ter_relay_raised" "raise_drop" NOT NULL DEFAULT 'drop', PRIMARY KEY ("id"), CONSTRAINT "station_timer_state_station_id_seconds_key" UNIQUE ("station_id", "seconds"), CONSTRAINT "station_timer_state_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
