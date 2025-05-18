-- Modify "switching_machine_route" table
ALTER TABLE "switching_machine_route" ADD COLUMN "on_route_lock" boolean NOT NULL DEFAULT false;
