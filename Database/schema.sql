CREATE TABLE "OpenIddictApplications"
(
    id                        text NOT NULL,
    application_type          character varying(50),
    client_id                 character varying(100),
    client_secret             text,
    client_type               character varying(50),
    concurrency_token         character varying(50),
    consent_type              character varying(50),
    display_name              text,
    display_names             text,
    json_web_key_set          text,
    permissions               text,
    post_logout_redirect_uris text,
    properties                text,
    redirect_uris             text,
    requirements              text,
    settings                  text,
    CONSTRAINT "PK_OpenIddictApplications" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX "IX_OpenIddictApplications_client_id" ON "OpenIddictApplications" (client_id);

CREATE TABLE "OpenIddictScopes"
(
    id                text NOT NULL,
    concurrency_token character varying(50),
    description       text,
    descriptions      text,
    display_name      text,
    display_names     text,
    name              character varying(200),
    properties        text,
    resources         text,
    CONSTRAINT "PK_OpenIddictScopes" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX "IX_OpenIddictScopes_name" ON "OpenIddictScopes" (name);

CREATE TABLE "OpenIddictAuthorizations"
(
    id                text NOT NULL,
    application_id    text,
    concurrency_token character varying(50),
    creation_date     timestamp with time zone,
    properties        text,
    scopes            text,
    status            character varying(50),
    subject           character varying(400),
    type              character varying(50),
    CONSTRAINT "PK_OpenIddictAuthorizations" PRIMARY KEY (id),
    CONSTRAINT "FK_OpenIddictAuthorizations_OpenIddictApplications_application~" FOREIGN KEY (application_id) REFERENCES "OpenIddictApplications" (id)
);
CREATE INDEX "IX_OpenIddictAuthorizations_application_id_status_subject_type" ON "OpenIddictAuthorizations" (application_id, status, subject, type);

CREATE TABLE "OpenIddictTokens"
(
    id                text NOT NULL,
    application_id    text,
    authorization_id  text,
    concurrency_token character varying(50),
    creation_date     timestamp with time zone,
    expiration_date   timestamp with time zone,
    payload           text,
    properties        text,
    redemption_date   timestamp with time zone,
    reference_id      character varying(100),
    status            character varying(50),
    subject           character varying(400),
    type              character varying(50),
    CONSTRAINT "PK_OpenIddictTokens" PRIMARY KEY (id),
    CONSTRAINT "FK_OpenIddictTokens_OpenIddictApplications_application_id" FOREIGN KEY (application_id) REFERENCES "OpenIddictApplications" (id),
    CONSTRAINT "FK_OpenIddictTokens_OpenIddictAuthorizations_authorization_id" FOREIGN KEY (authorization_id) REFERENCES "OpenIddictAuthorizations" (id)
);

CREATE INDEX "IX_OpenIddictTokens_authorization_id" ON "OpenIddictTokens" (authorization_id);
CREATE UNIQUE INDEX "IX_OpenIddictTokens_reference_id" ON "OpenIddictTokens" (reference_id);

CREATE TABLE lever
(
    id                 SERIAL PRIMARY KEY,
    station            VARCHAR(100) NOT NULL,
    name               VARCHAR(20)  NOT NULL,
    description        TEXT         NOT NULL,
    type               VARCHAR(50)  NOT NULL,
    root               VARCHAR(100),
    indicator          VARCHAR(10),
    approach_lock_time INT,
    UNIQUE (station, name)
);

CREATE TABLE lever_include
(
    source_lever_id INT REFERENCES lever (ID) NOT NULL,
    target_lever_id INT REFERENCES lever (ID) NOT NULL
);

CREATE TABLE station
(
    name VARCHAR(100) NOT NULL,
    PRIMARY KEY (name)
);

CREATE TABLE track_circuit
(
    id              SERIAL PRIMARY KEY,
    station         VARCHAR(100) REFERENCES station (name),
    name            VARCHAR(20) NOT NULL UNIQUE,
    protection_zone VARCHAR(20) NOT NULL
);

CREATE TABLE lock
(
    id                 SERIAL PRIMARY KEY,
    lever_id           INT REFERENCES lever (ID),
    type               VARCHAR(255),
    route_lock_group   INT,
    or_condition_group INT
);

CREATE TABLE lock_condition
(
    ID                     SERIAL PRIMARY KEY,
    type                   VARCHAR(50) NOT NULL,
    lever_id               INT REFERENCES lever (ID),
    track_circuit_id       VARCHAR(255) REFERENCES track_circuit (name),
    timer_seconds          INT,
    is_reverse             BOOLEAN     NOT NULL,
    is_total_control       BOOLEAN     NOT NULL,
    is_single_lock         BOOLEAN     NOT NULL,
    condition_time_seconds INT
);

CREATE TABLE lock_condition_execute
(
    source_id INT REFERENCES lock_condition (ID) NOT NULL,
    target_id INT REFERENCES lock_condition (ID) NOT NULL
);