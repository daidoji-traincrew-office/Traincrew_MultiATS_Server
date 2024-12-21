-- Modify "lock_condition" table
ALTER TABLE "lock_condition"
    ALTER COLUMN "is_reverse" TYPE "nr" using
        CASE
            WHEN "is_reverse" = true THEN 'reversed'::"nr"
            ELSE 'normal'::"nr"
            END;
-- Modify "route_state" table
ALTER TABLE "route_state"
    ALTER COLUMN "is_lever_reversed" TYPE "nr" using
        CASE
            WHEN "is_lever_reversed" = true THEN 'reversed'::"nr"
            ELSE 'normal'::"nr"
            END,
    ALTER COLUMN "is_reversed" TYPE "nr" using
        CASE
            WHEN "is_reversed" = true THEN 'reversed'::"nr"
            ELSE 'normal'::"nr"
            END,
    ALTER COLUMN "should_reverse" TYPE "nr" using
        CASE
            WHEN "should_reverse" = true THEN 'reversed'::"nr"
            ELSE 'normal'::"nr"
            END;
