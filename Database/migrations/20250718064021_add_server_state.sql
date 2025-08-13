-- Create enum type "server_mode"
CREATE TYPE "server_mode" AS ENUM ('off', 'private', 'public');
-- Create "server_state" table
CREATE TABLE "server_state" ("id" serial NOT NULL, "mode" "server_mode" NOT NULL, PRIMARY KEY ("id"));
