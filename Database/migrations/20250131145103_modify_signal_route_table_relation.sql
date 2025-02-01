-- Modify "signal" table
ALTER TABLE "signal" ADD COLUMN "route_id" bigint NULL, ADD CONSTRAINT "signal_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
-- Drop "signal_route" table
DROP TABLE "signal_route";
