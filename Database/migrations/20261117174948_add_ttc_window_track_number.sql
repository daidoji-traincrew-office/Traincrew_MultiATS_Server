-- Modify "ttc_window" table
ALTER TABLE "ttc_window" ADD COLUMN "track_number" character varying(10) NOT NULL DEFAULT '';
-- Create index "ttc_window_station_id_track_number_key" to table: "ttc_window"
CREATE UNIQUE INDEX "ttc_window_station_id_track_number_key" ON "ttc_window" ("station_id", "track_number") WHERE ((track_number)::text <> ''::text);
