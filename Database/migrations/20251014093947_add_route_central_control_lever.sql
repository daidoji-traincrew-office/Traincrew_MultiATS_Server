-- Add value to enum type: "object_type"
ALTER TYPE "object_type" ADD VALUE 'route_central_control_lever';
-- Create "route_central_control_lever" table
CREATE TABLE "route_central_control_lever" ("id" bigint NOT NULL, PRIMARY KEY ("id"), CONSTRAINT "route_central_control_lever_id_fkey" FOREIGN KEY ("id") REFERENCES "interlocking_object" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
-- Create "route_central_control_lever_state" table
CREATE TABLE "route_central_control_lever_state" ("id" bigint NOT NULL, "is_inserted_key" boolean NOT NULL DEFAULT false, "is_reversed" "nr" NOT NULL DEFAULT 'normal', PRIMARY KEY ("id"), CONSTRAINT "route_central_control_lever_state_id_fkey" FOREIGN KEY ("id") REFERENCES "route_central_control_lever" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION);
