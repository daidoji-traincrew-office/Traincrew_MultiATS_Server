-- Modify "ttc_window_link" table
ALTER TABLE "ttc_window_link" ADD CONSTRAINT "ttc_window_link_source_ttc_window_name_target_ttc_window_na_key" UNIQUE ("source_ttc_window_name", "target_ttc_window_name");
