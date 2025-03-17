-- Modify "signal" table
ALTER TABLE "signal" ADD COLUMN "station_id" character varying(10) NULL, ADD CONSTRAINT "signal_station_id_fkey" FOREIGN KEY ("station_id") REFERENCES "station" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Create index "signal_station_id_index" to table: "signal"
CREATE INDEX "signal_station_id_index" ON "signal" ("station_id");
