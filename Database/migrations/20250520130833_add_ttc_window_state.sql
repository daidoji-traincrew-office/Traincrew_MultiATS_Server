-- Create "ttc_window_state" table
CREATE TABLE "ttc_window_state" ("name" character varying(100) NOT NULL, "train_number" character varying(100) NULL, CONSTRAINT "ttc_window_state_name_fkey" FOREIGN KEY ("name") REFERENCES "ttc_window" ("name") ON UPDATE NO ACTION ON DELETE NO ACTION);
