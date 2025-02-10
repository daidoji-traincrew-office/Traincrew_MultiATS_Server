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


-- 停車場を表す
CREATE TABLE station
(
    id                   VARCHAR(10) PRIMARY KEY,
    name                 VARCHAR(100) NOT NULL UNIQUE,
    is_station           BOOLEAN      NOT NULL, -- 停車場かどうか
    is_passenger_station BOOLEAN      NOT NULL  -- 旅客駅かどうか
);

CREATE TYPE object_type AS ENUM ('route', 'switching_machine', 'track_circuit', 'lever');

-- object(進路、転てつ機、軌道回路、てこ)を表す
-- 以上4つのIDの一元管理を行う
-- Todo: 命名を連動オブジェクトなどに変更する
CREATE TABLE interlocking_object
(
    id          BIGSERIAL PRIMARY KEY,
    type        object_type  NOT NULL,        -- 進路、転てつ機、軌道回路、てこ
    name        VARCHAR(100) NOT NULL UNIQUE, -- 名前
    description TEXT                          -- 説明
);

CREATE TABLE station_interlocking_object
(
    station_id VARCHAR(10) REFERENCES station (id)        NOT NULL,
    object_id  BIGINT REFERENCES interlocking_object (id) NOT NULL,
    UNIQUE (station_id, object_id)
);
CREATE INDEX station_interlocking_object_station_id_index ON station_interlocking_object (station_id);

-- 着点ボタン
CREATE TABLE destination_button
(
    name       VARCHAR(100) PRIMARY KEY,                    -- 着点ボタンの名前
    station_id VARCHAR(10) REFERENCES station (id) NOT NULL -- 所属する停車場
);

-- 進路
-- 場内信号機、出発信号機、誘導信号機、入換信号機、入換標識
CREATE TYPE route_type AS ENUM ('arriving', 'departure', 'guide', 'switch_signal', 'switch_route');

CREATE TABLE route
(
    id                 BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    tc_name            VARCHAR(100) NOT NULL, -- Traincrewでの名前
    route_type         route_type   NOT NULL,
    root               VARCHAR(100),          -- 親進路
    indicator          VARCHAR(10),           -- 進路表示機(Todo: ここに持たせるべきなのか?)
    approach_lock_time INT                    -- 接近鎖状の時間
);

-- 進路の親子関係
CREATE TABLE route_include
(
    source_lever_id BIGINT REFERENCES route (ID) NOT NULL,
    target_lever_id BIGINT REFERENCES route (ID) NOT NULL,
    UNIQUE (source_lever_id, target_lever_id)
);
CREATE INDEX route_include_source_lever_id_index ON route_include (source_lever_id);

-- 軌道回路
CREATE TABLE track_circuit
(
    id              BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    protection_zone INT NOT NULL -- 防護無線区間
);

-- 転てつ機
CREATE TABLE switching_machine
(
    id      BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    tc_name VARCHAR(100) NOT NULL -- Traincrewでの名前
);

-- 鎖状、信号制御、てっさ鎖状、進路鎖状、接近鎖状、保留鎖状
CREATE TYPE lock_type AS ENUM ('lock', 'signal_control', 'detector', 'route', 'approach', 'stick');

-- てこ
CREATE TYPE lever_type AS ENUM ('route', 'switching_machine');
CREATE TABLE lever
(
    id         BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    lever_type lever_type NOT NULL,  -- てこの種類 
    switching_machine_id BIGINT REFERENCES switching_machine (ID) --転てつ機のID
);


-- 進路に対するてこと着点ボタンのリスト
CREATE TABLE route_lever_destination_button
(
    id                      BIGSERIAL PRIMARY KEY,
    route_id                BIGINT REFERENCES route (ID)                      NOT NULL, -- 進路のID
    lever_id                BIGINT REFERENCES lever (ID)                      NOT NULL, -- てこのID
    destination_button_name VARCHAR(100) REFERENCES destination_button (name) NOT NULL, -- 着点ボタンの名前
    UNIQUE (route_id),
    UNIQUE (lever_id, destination_button_name)
);

-- 信号
CREATE TYPE signal_indication AS ENUM ('R', 'YY', 'Y', 'YG', 'G');
--- 信号機種類テーブル(現示の種類)
--- 次の信号機がこれなら、この信号機の現示はこれ、っていうリスト
CREATE TABLE signal_type
(
    name          VARCHAR(100) PRIMARY KEY, -- 4灯式とか、3灯式とかのやつ
    r_indication  signal_indication NOT NULL,
    yy_indication signal_indication NOT NULL,
    y_indication  signal_indication NOT NULL,
    yg_indication signal_indication NOT NULL,
    g_indication  signal_indication NOT NULL
);
--- 信号機
CREATE TABLE signal
(
    name             VARCHAR(100) PRIMARY KEY                   NOT NULL,
    type             VARCHAR(100) REFERENCES signal_type (name) NOT NULL, -- 信号機の種類(4灯式とか)
    track_circuit_id BIGINT REFERENCES track_circuit (ID),                -- 閉そく信号機の軌道回路
    route_id         BIGINT REFERENCES route (ID)                         -- 絶対信号機の進路
);

-- 次の信号リスト
CREATE TABLE next_signal
(
    id                 BIGSERIAL PRIMARY KEY,
    signal_name        VARCHAR(100) REFERENCES signal (name) NOT NULL, -- 探索キー
    source_signal_name VARCHAR(100) REFERENCES signal (name) NOT NULL, -- 探索キーからn-1番目の信号機
    target_signal_name VARCHAR(100) REFERENCES signal (name) NOT NULL, -- 探索キーからn番目の信号機
    depth              INT                                   NOT NULL, -- n番目の信号か
    UNIQUE (signal_name, target_signal_name)
);
CREATE INDEX next_signal_signal_name_index ON next_signal (signal_name);

-- 軌道回路に対する信号機のリスト
CREATE TABLE track_circuit_signal
(
    id               BIGSERIAL PRIMARY KEY,
    track_circuit_id BIGINT REFERENCES track_circuit (ID)  NOT NULL, -- 軌道回路のID
    is_up            BOOLEAN                               NOT NULL, -- 上りか下りか
    signal_name      VARCHAR(100) REFERENCES signal (name) NOT NULL, -- 信号機の名前
    UNIQUE (track_circuit_id, is_up, signal_name)
);

-- 各進路、転てつ機の鎖状条件(すべての鎖状条件をここにいれる)
CREATE TABLE lock
(
    id               BIGSERIAL PRIMARY KEY,
    object_id        BIGINT REFERENCES interlocking_object (id), -- 進路、転てつ機、軌道回路のID
    type             lock_type NOT NULL,                         -- 鎖状の種類
    route_lock_group INT                                         -- 進路鎖状のグループ(カッコで囲まれてるやつを同じ数字にする)
);
CREATE INDEX lock_object_id_type_index ON lock (object_id, type);

CREATE TYPE nr AS ENUM ('reversed', 'normal');
CREATE TYPE nrc AS ENUM ('reversed', 'center', 'normal');
CREATE TYPE raise_drop AS ENUM ('raise', 'drop');
CREATE TYPE lock_condition_type AS ENUM ('and', 'or', 'object');
CREATE TYPE lcr as ENUM ('left', 'center', 'right');

-- 鎖状条件詳細(and, or, object)
CREATE TABLE lock_condition
(
    id        BIGSERIAL PRIMARY KEY,
    lock_id   BIGINT REFERENCES lock (ID) NOT NULL,  -- 鎖状のID(グラフの根、鎖状条件の一番上の階層)
    parent_id BIGINT REFERENCES lock_condition (ID), -- 親のID(いれば)
    type      lock_condition_type         NOT NULL   -- 鎖状条件の種類(and, or, object)
);
CREATE INDEX lock_condition_lock_id_index ON lock_condition (lock_id);
-- 鎖状条件のobjectの詳細
CREATE TABLE lock_condition_object
(
    lock_condition_id BIGINT PRIMARY KEY REFERENCES lock_condition (ID),   -- 鎖状条件のID
    object_id         BIGINT REFERENCES interlocking_object (id) NOT NULL, -- 進路、転てつ機、軌道回路、てこのID
    timer_seconds     INT,                                                 -- タイマーの秒数
    is_reverse        nr                                         NOT NULL, -- 定反
    is_single_lock    BOOLEAN                                    NOT NULL  -- 片鎖状がどうか    
);
-- 統括制御テーブル
CREATE TABLE total_control
(
    id              BIGSERIAL PRIMARY KEY,
    source_route_id BIGINT REFERENCES route (id) NOT NULL UNIQUE, -- 統括制御の元となる進路
    target_route_id BIGINT REFERENCES route (id) NOT NULL UNIQUE  -- 統括制御の対象となる進路
);

-- 転てつ機に対して要求元進路と要求向きのリスト
CREATE TABLE switching_machine_route
(
    id                   BIGSERIAL PRIMARY KEY,
    switching_machine_id BIGINT REFERENCES switching_machine (id) NOT NULL, -- 転てつ機のID
    route_id             BIGINT REFERENCES route (id)             NOT NULL, -- 進路のID
    is_reverse           nr                                       NOT NULL,  -- 定反
    UNIQUE (switching_machine_id, route_id)
);
CREATE INDEX switching_machine_route_switching_machine_id_index ON switching_machine_route (switching_machine_id);

-- ここから状態系
-- てこ状態
CREATE TABLE lever_state
(
    id          BIGINT PRIMARY KEY REFERENCES lever (ID), -- てこのID
    is_reversed lcr NOT NULL                              -- てこの位置
);

-- 着点ボタン状態
CREATE TABLE destination_button_state
(
    name        VARCHAR(100) PRIMARY KEY REFERENCES destination_button (name), -- 着点ボタンの名前
    is_raised   raise_drop NOT NULL,                                           -- 着点ボタンの扛上、落下
    operated_at TIMESTAMP  NOT NULL                                            -- 最終操作時刻
);

-- 軌道回路状態
CREATE TABLE track_circuit_state
(
    id               BIGINT PRIMARY KEY REFERENCES track_circuit (ID), -- 軌道回路のID
    train_number     VARCHAR(100),                                     -- 列車番号
    is_short_circuit BOOLEAN NOT NULL,                                 -- 短絡状態
    is_locked        BOOLEAN NOT NULL                                  -- 鎖状しているかどうか
);
CREATE INDEX track_circuit_state_train_number_index ON track_circuit_state USING hash (train_number);


-- 転てつ機状態
CREATE TABLE switching_machine_state
(
    id                BIGINT PRIMARY KEY REFERENCES switching_machine (ID), -- 転てつ機のID
    is_switching      BOOLEAN NOT NULL,                                     -- 転換中
    is_reverse        nr      NOT NULL,                                     -- 定反
    switch_end_time   TIMESTAMP                                             -- 転換終了時刻
);

-- Todo: 進路状態
CREATE TABLE route_state
(
    id                       BIGINT PRIMARY KEY REFERENCES route (ID), -- 進路のID
    is_lever_relay_raised    raise_drop NOT NULL,                      -- てこリレーが上がっているか
    is_route_relay_raised    raise_drop NOT NULL,                      -- 進路照査リレーが上がっているか
    is_signal_control_raised raise_drop NOT NULL,                      -- 信号制御リレーが上がっているか
    is_approach_lock_raised  raise_drop NOT NULL,                      -- 接近鎖状が上がっているか
    is_route_lock_raised     raise_drop NOT NULL                       -- 進路鎖状が上がっているか
);

-- 信号機状態
CREATE TABLE signal_state
(
    signal_name VARCHAR(100) PRIMARY KEY REFERENCES signal (name), -- 信号機の名前
    is_lighted  BOOLEAN NOT NULL                                   -- 点灯状態
);

-- 鎖状状態
-- 進路鎖状の場合、接近鎖状の場合、てっさ鎖状、保留鎖状
-- テッサ鎖状は、軌道回路の短絡状態を見ればよいので含めない
-- 接近鎖状は、鎖状しているオブジェクトのlock_typeをすべて接近鎖状に変更し、end_timeを設定する
-- 進路鎖状は、進路から軌道回路に鎖状をかける(特段追加カラム必要なし)
-- 保留鎖状は、接近鎖状とほぼ同じ
CREATE TABLE lock_state
(
    id               BIGSERIAL PRIMARY KEY,
    target_object_id BIGINT REFERENCES interlocking_object (ID) NOT NULL, -- 鎖状されるオブジェクトID
    source_object_id BIGINT REFERENCES interlocking_object (ID) NOT NULL, -- 鎖状する要因のオブジェクトID
    is_reverse       nr                                         NOT NULL, -- 定反
    lock_type        lock_type                                  NOT NULL, -- 鎖状の種類
    end_time         TIMESTAMP                                            -- 接近鎖状が終了する時刻
);

CREATE INDEX lock_state_target_object_id_index ON lock_state (target_object_id);

-- 防護無線状態
CREATE TABLE protection_zone_state
(
    id              BIGSERIAL PRIMARY KEY,
    protection_zone BIGINT       NOT NULL,
    train_number    VARCHAR(100) NOT NULL,
    UNIQUE (protection_zone, train_number)
);