-- Create enum type "nr"
CREATE TYPE "nr" AS ENUM ('reversed', 'normal');
-- Create enum type "nrc"
CREATE TYPE "nrc" AS ENUM ('reversed', 'center', 'normal');
-- Modify "switching_machine_state" table
ALTER TABLE "switching_machine_state"
    ALTER COLUMN "is_reverse" TYPE "nr" using
        CASE
            WHEN "is_reverse" = true THEN 'reversed'::"nr"
            ELSE 'normal'::"nr"
            END,
    ALTER COLUMN "is_lever_reversed" TYPE "nrc" using
        CASE
            WHEN "is_lever_reversed" = true THEN 'reversed'::"nrc"
            WHEN "is_lever_reversed" = false THEN 'normal'::"nrc"
            ELSE 'center'::"nrc"
            END,
    ALTER COLUMN "is_lever_reversed" SET NOT NULL,
    ADD COLUMN "is_switching" boolean NOT NULL;
