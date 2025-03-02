-- Rename a column from "lock_condition_id" to "id"
ALTER TABLE "lock_condition_object" RENAME COLUMN "lock_condition_id" TO "id";
-- Modify "lock_condition_object" table
ALTER TABLE "lock_condition_object" DROP CONSTRAINT "lock_condition_object_lock_condition_id_fkey", ADD CONSTRAINT "lock_condition_object_id_fkey" FOREIGN KEY ("id") REFERENCES "lock_condition" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;