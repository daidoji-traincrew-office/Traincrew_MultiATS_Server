-- Modify "signal" table
ALTER TABLE "signal" DROP COLUMN "route_id";
-- Create "signal_route" table
CREATE TABLE "signal_route" ("signal_name" character varying(100) NOT NULL, "route_id" bigint NOT NULL, CONSTRAINT "signal_route_route_id_key" UNIQUE ("route_id"), CONSTRAINT "signal_route_route_id_fkey" FOREIGN KEY ("route_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION, CONSTRAINT "signal_route_signal_name_fkey" FOREIGN KEY ("signal_name") REFERENCES "signal" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create index "signal_route_signal_name_index" to table: "signal_route"
CREATE INDEX "signal_route_signal_name_index" ON "signal_route" ("signal_name");
