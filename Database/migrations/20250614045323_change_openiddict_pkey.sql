-- Rename a constraint from "OpenIddictApplications_pkey" to "PK_OpenIddictApplications"
ALTER TABLE "OpenIddictApplications" RENAME CONSTRAINT "OpenIddictApplications_pkey" TO "PK_OpenIddictApplications";
-- Rename a constraint from "OpenIddictAuthorizations_pkey" to "PK_OpenIddictAuthorizations"
ALTER TABLE "OpenIddictAuthorizations" RENAME CONSTRAINT "OpenIddictAuthorizations_pkey" TO "PK_OpenIddictAuthorizations";
-- Rename a constraint from "OpenIddictScopes_pkey" to "PK_OpenIddictScopes"
ALTER TABLE "OpenIddictScopes" RENAME CONSTRAINT "OpenIddictScopes_pkey" TO "PK_OpenIddictScopes";
-- Rename a constraint from "OpenIddictTokens_pkey" to "PK_OpenIddictTokens"
ALTER TABLE "OpenIddictTokens" RENAME CONSTRAINT "OpenIddictTokens_pkey" TO "PK_OpenIddictTokens";
-- Rename a constraint from "direction_lever_pkey" to "direction_route_pkey"
ALTER TABLE "direction_route" RENAME CONSTRAINT "direction_lever_pkey" TO "direction_route_pkey";
-- Rename a constraint from "direction_lever_state_pkey" to "direction_route_state_pkey"
ALTER TABLE "direction_route_state" RENAME CONSTRAINT "direction_lever_state_pkey" TO "direction_route_state_pkey";
-- Rename a constraint from "opening_lever_pkey" to "direction_self_control_lever_pkey"
ALTER TABLE "direction_self_control_lever" RENAME CONSTRAINT "opening_lever_pkey" TO "direction_self_control_lever_pkey";
-- Rename a constraint from "opening_lever_state_pkey" to "direction_self_control_lever_state_pkey"
ALTER TABLE "direction_self_control_lever_state" RENAME CONSTRAINT "opening_lever_state_pkey" TO "direction_self_control_lever_state_pkey";
