-- Modify "station_timer_state" table
ALTER TABLE "station_timer_state" ADD COLUMN "teu_relay_raised_at" timestamp NOT NULL default now();
ALTER TABLE "station_timer_state" ALTER COLUMN "teu_relay_raised_at" DROP DEFAULT;
