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

-- Todo: あとで各行にコメントを書こう

-- 停車場を表す
CREATE TABLE station
(
    name VARCHAR(100) NOT NULL,
    PRIMARY KEY (name)
);

CREATE TYPE object_type AS ENUM ('route', 'switching_machine', 'track_circuit');

-- object(進路、転てつ機、軌道回路)を表す
-- 以上3つのIDの一元管理を行う
CREATE TABLE object
(
    id   BIGSERIAL PRIMARY KEY,
    type object_type NOT NULL -- 進路、転てつ機、軌道回路
);

-- 進路
-- 場内信号機、出発信号機、誘導信号機、入換信号機、入換標識
CREATE TYPE route_type AS ENUM ('arriving', 'departure', 'guide', 'switch_signal', 'switch_route');

CREATE TABLE route
(
    id                 BIGINT PRIMARY KEY REFERENCES object (id),
    station            VARCHAR(100) NOT NULL, -- 停車場の名前
    name               VARCHAR(20)  NOT NULL, -- 名前
    tc_name            VARCHAR(100),          -- Traincrewでの名前(要検討)
    description        TEXT,                  -- 説明
    route_type         route_type   NOT NULL,
    root               VARCHAR(100),
    indicator          VARCHAR(10),
    approach_lock_time INT,
    UNIQUE (station, name)
);

CREATE TABLE route_include
(
    source_lever_id INT REFERENCES lever (ID) NOT NULL,
    target_lever_id INT REFERENCES lever (ID) NOT NULL
);

-- 信号
---- Todo: SwitchGを入れるか？
CREATE TYPE signal_indication AS ENUM ('R', 'YY', 'Y', 'YG', 'G');
--- 信号機種類テーブル(現示の種類)
--- 次の信号機がこれなら、この信号機の現示はこれ、っていうリスト
CREATE TABLE signal_type
(
    name            VARCHAR(100)      NOT NULL, -- 4灯式とか、3灯式とかのやつ
    next_indication signal_indication NOT NULL,
    this_indication signal_indication NOT NULL,
    UNIQUE (name, next_indication)
);
CREATE INDEX signal_type_name_index ON signal_type (name);

--- 信号機
CREATE TABLE signal
(
    name             VARCHAR(100) PRIMARY KEY                   NOT NULL,
    next_signal_name VARCHAR(100) REFERENCES signal (name),               -- 次の信号機
    type             VARCHAR(100) REFERENCES signal_type (name) NOT NULL, -- 信号機の種類(4灯式とか)
    track_circuit_id BIGINT REFERENCES track_circuit (ID)                 -- 閉そく信号機の軌道回路
);

--- 信号機と進路の関係
CREATE TABLE signal_route
(
    signal_name VARCHAR(100) REFERENCES signal (name) NOT NULL,
    route_id    BIGINT REFERENCES route (id)          NOT NULL
);
CREATE INDEX signal_route_signal_name_index ON signal_route (signal_name);


-- 軌道回路
CREATE TABLE track_circuit
(
    id              BIGINT PRIMARY KEY REFERENCES object (id),
    station         VARCHAR(100) REFERENCES station (name),
    name            VARCHAR(20) NOT NULL UNIQUE,
    protection_zone INT         NOT NULL
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
    lock_id                INT REFERENCES lock (ID),
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