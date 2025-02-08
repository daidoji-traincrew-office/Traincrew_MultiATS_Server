-- Create enum type "lcr"
CREATE TYPE "lcr" AS ENUM ('left', 'center', 'right');
-- Modify "lever_state" table
ALTER TABLE "lever_state" DROP COLUMN "is_reversed", ADD COLUMN "is_reversed" "lcr" NOT NULL;
