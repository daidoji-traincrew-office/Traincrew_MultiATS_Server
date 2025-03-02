-- Modify "signal_route" table
ALTER TABLE "signal_route" DROP CONSTRAINT "signal_route_route_id_key", ADD CONSTRAINT "signal_route_signal_name_route_id_key" UNIQUE ("signal_name", "route_id");
