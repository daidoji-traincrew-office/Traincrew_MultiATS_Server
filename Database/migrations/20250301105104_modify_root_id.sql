-- Modify "route" table
ALTER TABLE "route" DROP COLUMN "root", ADD COLUMN "root_id" bigint NULL, ADD CONSTRAINT "route_root_id_fkey" FOREIGN KEY ("root_id") REFERENCES "route" ("id") ON UPDATE NO ACTION ON DELETE NO ACTION;
