-- Modify "server_state" table
ALTER TABLE "server_state" ADD COLUMN "switch_move_time" integer NOT NULL DEFAULT 5000, ADD COLUMN "switch_return_time" integer NOT NULL DEFAULT 500, ADD COLUMN "use_one_second_relay" boolean NOT NULL DEFAULT false;
