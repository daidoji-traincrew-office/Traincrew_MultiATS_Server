-- Create enum type "raise_drop_with_force"
CREATE TYPE "raise_drop_with_force" AS ENUM ('force_drop', 'drop', 'raise');
-- Modify "server_state" table
ALTER TABLE "server_state" ADD COLUMN "is_all_signal_relay_raised" "raise_drop_with_force" NOT NULL DEFAULT 'drop';
