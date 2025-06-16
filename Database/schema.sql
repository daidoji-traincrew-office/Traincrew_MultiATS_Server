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

CREATE TYPE object_type AS ENUM ('route', 'switching_machine', 'track_circuit', 'lever', 'direction_route', 'direction_self_control_lever');

-- object(進路、転てつ機、軌道回路、てこ)を表す
-- 以上4つのIDの一元管理を行う
-- Todo: 命名を連動オブジェクトなどに変更する
CREATE TABLE interlocking_object
(
    id          BIGSERIAL PRIMARY KEY,
    type        object_type  NOT NULL,               -- 進路、転てつ機、軌道回路、てこ
    name        VARCHAR(100) NOT NULL UNIQUE,        -- 名前
    station_id  VARCHAR(10) REFERENCES station (id), -- 所属する停車場
    description TEXT                                 -- 説明
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

-- 運転告知機
CREATE TABLE operation_notification_display
(
    name       VARCHAR(100) PRIMARY KEY,                     -- 告知機の名前
    station_id VARCHAR(10) REFERENCES station (id) NOT NULL, -- 所属する停車場
    is_up      BOOLEAN                             NOT NULL, -- 上り
    is_down    BOOLEAN                             NOT NULL  -- 下り
);

-- 軌道回路
CREATE TABLE track_circuit
(
    id                                  BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    protection_zone                     INT NOT NULL,                                                 -- 防護無線区間
    operation_notification_display_name VARCHAR(100) REFERENCES operation_notification_display (name) -- 運転告知機の名前
);
CREATE INDEX track_circuit_operation_notification_display_name_index ON track_circuit (operation_notification_display_name);

-- 進路
-- 場内信号機、出発信号機、誘導信号機、入換信号機、入換標識
CREATE TYPE route_type AS ENUM ('arriving', 'departure', 'guide', 'switch_signal', 'switch_route');

CREATE TABLE route
(
    id                                   BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    tc_name                              VARCHAR(100) NOT NULL,               -- Traincrewでの名前
    route_type                           route_type   NOT NULL,
    root_id                              BIGINT REFERENCES route (id),        -- 親進路
    indicator                            VARCHAR(10),                         -- 進路表示機(Todo: ここに持たせるべきなのか?)
    approach_lock_time                   INT,                                 -- 接近鎖状の時間
    approach_lock_final_track_circuit_id BIGINT REFERENCES track_circuit (id) -- 接近鎖錠欄の最後の軌道回路ID
);

-- 進路の親子関係
CREATE TABLE route_include
(
    source_lever_id BIGINT REFERENCES route (ID) NOT NULL,
    target_lever_id BIGINT REFERENCES route (ID) NOT NULL,
    UNIQUE (source_lever_id, target_lever_id)
);
CREATE INDEX route_include_source_lever_id_index ON route_include (source_lever_id);

-- TTC列番窓
-- 種類
CREATE TYPE ttc_window_type AS ENUM ('home_track', 'up', 'down', 'switching');

CREATE TABLE ttc_window
(
    name       VARCHAR(100) PRIMARY KEY,                     -- 名前
    station_id VARCHAR(10) REFERENCES station (id) NOT NULL, -- 所属する停車場
    type       ttc_window_type                     NOT NULL  -- 列番窓の種類
);

CREATE TABLE ttc_window_display_station
(
    id              BIGSERIAL PRIMARY KEY,
    ttc_window_name VARCHAR(100) REFERENCES ttc_window (name) NOT NULL, -- 列番窓の名前
    station_id      VARCHAR(10) REFERENCES station (id)       NOT NULL, -- 表示する駅のID
    UNIQUE (ttc_window_name, station_id)
);

CREATE TABLE ttc_window_track_circuit
(
    id               BIGSERIAL PRIMARY KEY,
    ttc_window_name  VARCHAR(100) REFERENCES ttc_window (name) NOT NULL, -- 列番窓の名前
    track_circuit_id BIGINT REFERENCES track_circuit (ID)      NOT NULL, -- 対応する軌道回路のID
    UNIQUE (ttc_window_name, track_circuit_id)
);

-- リンク設定
CREATE TYPE ttc_window_link_type AS ENUM ('up', 'down', 'switching');
CREATE TABLE ttc_window_link
(
    id                      BIGSERIAL PRIMARY KEY,
    source_ttc_window_name  VARCHAR(100) REFERENCES ttc_window (name) NOT NULL, -- リンク元の列番窓の名前
    target_ttc_window_name  VARCHAR(100) REFERENCES ttc_window (name) NOT NULL, -- リンク先の列番窓の名前
    type                    ttc_window_link_type                      NOT NULL, -- リンクの種類
    is_empty_sending        BOOLEAN                                   NOT NULL, -- 空送りかどうか
    track_circuit_condition BIGINT REFERENCES track_circuit (id)                -- 移行条件の軌道回路ID
);

-- 移行条件進路リスト
CREATE table ttc_window_link_route_condition
(
    id                 BIGSERIAL PRIMARY KEY,
    ttc_window_link_id BIGINT REFERENCES ttc_window_link (id) NOT NULL, -- リンクのID
    route_id           BIGINT REFERENCES route (id)           NOT NULL, -- 進路のID
    UNIQUE (ttc_window_link_id, route_id)
);


-- 転てつ機
CREATE TABLE switching_machine
(
    id      BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    tc_name VARCHAR(100) NOT NULL -- Traincrewでの名前
);

-- 鎖状、信号制御、てっさ鎖状、進路鎖状、接近鎖状、保留鎖状
CREATE TYPE lock_type AS ENUM ('lock', 'signal_control', 'detector', 'route', 'approach', 'stick');

-- (転てつ器、進路)てこ
CREATE TYPE lever_type AS ENUM ('route', 'switching_machine', 'direction');
CREATE TABLE lever
(
    id                   BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    lever_type           lever_type NOT NULL,                     -- てこの種類 
    switching_machine_id BIGINT REFERENCES switching_machine (ID) --転てつ機のID
);

CREATE TYPE lr as ENUM ('left', 'right');

-- 開放てこ(方向てこ用)
CREATE TYPE nr AS ENUM ('reversed', 'normal');
CREATE TABLE direction_self_control_lever
(
    id BIGINT PRIMARY KEY REFERENCES interlocking_object (id) -- ID
);

-- 方向進路
CREATE TABLE direction_route
(
    id                              BIGINT PRIMARY KEY REFERENCES interlocking_object (id),
    lever_id                        BIGINT REFERENCES lever (ID) NOT NULL,               -- てこのID;
    direction_self_control_lever_id BIGINT REFERENCES direction_self_control_lever (id), -- 開放てこのID
    l_lock_lever_id                 BIGINT REFERENCES direction_route (id),              -- Lてこに対する隣駅鎖錠てこ
    l_lock_lever_direction          lr,                                                  -- Lてこに対する隣駅鎖錠てこの方向
    l_single_locked_lever_id        BIGINT REFERENCES direction_route (id),              -- Lてこに対する隣駅被片鎖状てこ
    l_single_locked_lever_direction lr,                                                  -- Lてこに対する隣駅被片鎖状てこの方向
    r_lock_lever_id                 BIGINT REFERENCES direction_route (id),              -- Rてこに対する隣駅鎖錠てこ
    r_lock_lever_direction          lr,                                                  -- Rてこに対する隣駅鎖錠てこの方向
    r_single_locked_lever_id        BIGINT REFERENCES direction_route (id),              -- Rてこに対する隣駅被片鎖状てこ
    r_single_locked_lever_direction lr                                                   -- Rてこに対する隣駅被片鎖状てこの方向
);

-- 進路に対するてこと着点ボタンのリスト
CREATE TABLE route_lever_destination_button
(
    id                      BIGSERIAL PRIMARY KEY,
    route_id                BIGINT REFERENCES route (ID) NOT NULL,             -- 進路のID
    lever_id                BIGINT REFERENCES lever (ID) NOT NULL,             -- てこのID
    destination_button_name VARCHAR(100) REFERENCES destination_button (name), -- 着点ボタンの名前
    direction               lr                           NOT NULL,             -- 左右方向
    UNIQUE (route_id),
    UNIQUE NULLS NOT DISTINCT (lever_id, destination_button_name, direction)
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
    name                     VARCHAR(100) PRIMARY KEY                   NOT NULL,
    station_id               VARCHAR(10) REFERENCES station (id),                 -- 所属する停車場(線間閉塞の場合は設定されない)
    type                     VARCHAR(100) REFERENCES signal_type (name) NOT NULL, -- 信号機の種類(4灯式とか)
    track_circuit_id         BIGINT REFERENCES track_circuit (ID),                -- 閉そく信号機の軌道回路
    direction_route_left_id  BIGINT REFERENCES direction_route (id),              -- 左方向進路
    direction_route_right_id BIGINT REFERENCES direction_route (id),              -- 右方向進路
    direction                lr                                                   -- LR向き
);
CREATE INDEX signal_station_id_index ON signal (station_id);

--- 信号機と進路の関係(停車場内の信号機に設定する)
CREATE TABLE signal_route
(
    id          BIGSERIAL PRIMARY KEY,
    signal_name VARCHAR(100) REFERENCES signal (name) NOT NULL,
    route_id    BIGINT REFERENCES route (id)          NOT NULL,
    UNIQUE (signal_name, route_id)
);
CREATE INDEX signal_route_signal_name_index ON signal_route (signal_name);

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
    object_id        BIGINT REFERENCES interlocking_object (id), -- 進路、転てつ機、方向てこのID
    type             lock_type NOT NULL,                         -- 鎖状の種類
    route_lock_group INT                                         -- 進路鎖状のグループ(カッコで囲まれてるやつを同じ数字にする)
);
CREATE INDEX lock_object_id_type_index ON lock (object_id, type);

CREATE TYPE nrc AS ENUM ('reversed', 'center', 'normal');
CREATE TYPE raise_drop AS ENUM ('raise', 'drop');
CREATE TYPE lock_condition_type AS ENUM ('and', 'or', 'not', 'object');
CREATE TYPE lcr as ENUM ('left', 'center', 'right');

-- 鎖状条件詳細(and, or, object)
CREATE TABLE lock_condition
(
    id        BIGSERIAL PRIMARY KEY,
    lock_id   BIGINT REFERENCES lock (ID) NOT NULL,  -- 鎖状のID(グラフの根、鎖状条件の一番上の階層)
    parent_id BIGINT REFERENCES lock_condition (ID), -- 親のID(いれば)
    type      lock_condition_type         NOT NULL   -- 鎖状条件の種類(and, or, not, object)
);
CREATE INDEX lock_condition_lock_id_index ON lock_condition (lock_id);
-- 鎖状条件のobjectの詳細
CREATE TABLE lock_condition_object
(
    id             BIGINT PRIMARY KEY REFERENCES lock_condition (ID),   -- 鎖状条件のID
    object_id      BIGINT REFERENCES interlocking_object (id) NOT NULL, -- 進路、転てつ機、軌道回路、てこのID
    timer_seconds  INT,                                                 -- タイマーの秒数
    is_reverse     nr                                         NOT NULL, -- 定反
    is_single_lock BOOLEAN                                    NOT NULL, -- 片鎖状がどうか    
    is_lr          lr                                                   -- 方向てこの方向
);
-- 統括制御テーブル
CREATE TABLE throw_out_control
(
    id                 BIGSERIAL PRIMARY KEY,
    source_id          BIGINT REFERENCES interlocking_object (id) NOT NULL, -- 統括元オブジェクトID
    source_lr          lr,                                                  -- 統括元が方向てこの場合、方向てこの向き
    target_id          BIGINT REFERENCES interlocking_object (id) NOT NULL, -- 統括先オブジェクトID
    target_lr          lr,                                                  -- 統括先が方向てこの場合、方向てこの向き
    condition_lever_id BIGINT REFERENCES direction_self_control_lever (id), -- てこ条件となる開放てこID
    condition_nr       nr                                                   -- てこ条件の開放てこの向き
);
CREATE INDEX throw_out_control_source_id_index ON throw_out_control (source_id);
CREATE INDEX throw_out_control_target_id_index ON throw_out_control (target_id);

-- 転てつ機に対して要求元進路と要求向きのリスト
CREATE TABLE switching_machine_route
(
    id                   BIGSERIAL PRIMARY KEY,
    switching_machine_id BIGINT REFERENCES switching_machine (id) NOT NULL, -- 転てつ機のID
    route_id             BIGINT REFERENCES route (id)             NOT NULL, -- 進路のID
    is_reverse           nr                                       NOT NULL, -- 定反
    on_route_lock        BOOLEAN DEFAULT false                    NOT NULL,
    UNIQUE (switching_machine_id, route_id)
);
CREATE INDEX switching_machine_route_switching_machine_id_index ON switching_machine_route (switching_machine_id);

-- 進路鎖錠で鎖状するべき軌道回路のリスト
CREATE TABLE route_lock_track_circuit
(
    id               BIGSERIAL PRIMARY KEY,
    route_id         BIGINT REFERENCES route (id)         NOT NULL, -- 進路のID
    track_circuit_id BIGINT REFERENCES track_circuit (ID) NOT NULL, -- 軌道回路のID
    UNIQUE (route_id, track_circuit_id)
);

-- 列車種別
CREATE TABLE train_type
(
    id   BIGINT PRIMARY KEY,          -- 列車のID
    name VARCHAR(100) NOT NULL UNIQUE -- 種別名
);

-- 列車(ダイヤグラム内の1列車)情報
CREATE TABLE train_diagram
(
    train_number    VARCHAR(100) PRIMARY KEY,                        -- 列車番号
    train_type_id   BIGINT      NOT NULL REFERENCES train_type (id), -- 列車種別ID
    from_station_id VARCHAR(10) NOT NULL REFERENCES station (id),    -- 出発駅ID
    to_station_id   VARCHAR(10) NOT NULL REFERENCES station (id),    -- 到着駅ID
    dia_id          INT         NOT NULL                             -- ダイヤID
);

-- ここから状態系
-- 駅時素状態
CREATE TABLE station_timer_state
(
    id                  BIGSERIAL PRIMARY KEY,
    station_id          VARCHAR(10) NOT NULL REFERENCES station (id), -- 駅のID
    seconds             INT         NOT NULL,                         -- 駅時素の秒数
    is_teu_relay_raised raise_drop  NOT NULL DEFAULT 'drop',
    is_ten_relay_raised raise_drop  NOT NULL DEFAULT 'drop',
    is_ter_relay_raised raise_drop  NOT NULL DEFAULT 'raise',
    teu_relay_raised_at TIMESTAMP NULL     DEFAULT NULL,
    UNIQUE (station_id, seconds)
);

-- てこ状態
CREATE TABLE lever_state
(
    id          BIGINT PRIMARY KEY REFERENCES lever (ID), -- てこのID
    is_reversed lcr NOT NULL                              -- てこの位置
);

-- 開放てこ状態
CREATE TABLE direction_self_control_lever_state
(
    id              BIGINT PRIMARY KEY REFERENCES direction_self_control_lever (ID), -- てこのID
    is_inserted_key BOOL NOT NULL DEFAULT 'false',                                   -- 鍵が挿入されているか
    is_reversed     nr   NOT NULL DEFAULT 'normal'                                   -- てこの位置
);

-- 方向てこ状態
CREATE TABLE direction_route_state
(
    id                   BIGINT PRIMARY KEY REFERENCES direction_route (ID), -- てこのID
    is_lr                lr         NOT NULL DEFAULT 'left',                 -- 方向てこの方向
    is_fl_relay_raised   raise_drop NOT NULL DEFAULT 'drop',                 -- 運転方向鎖錠リレー
    is_lfys_relay_raised raise_drop NOT NULL DEFAULT 'drop',                 -- L方向総括リレー
    is_rfys_relay_raised raise_drop NOT NULL DEFAULT 'drop',                 -- R方向総括リレー
    is_ly_relay_raised   raise_drop NOT NULL DEFAULT 'drop',                 -- L方向てこ反応リレー
    is_ry_relay_raised   raise_drop NOT NULL DEFAULT 'drop',                 -- R方向てこ反応リレー
    is_l_relay_raised    raise_drop NOT NULL DEFAULT 'drop',                 -- L方向てこリレー
    is_r_relay_raised    raise_drop NOT NULL DEFAULT 'drop'                  -- R方向てこリレー
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
    id                               BIGINT PRIMARY KEY REFERENCES track_circuit (ID), -- 軌道回路のID
    train_number                     VARCHAR(100),                                     -- 列車番号
    is_short_circuit                 BOOLEAN    NOT NULL,                              -- 短絡状態
    is_locked                        BOOLEAN    NOT NULL,                              -- 鎖状しているかどうか
    unlocked_at                      TIMESTAMP                    DEFAULT NULL,        -- 鎖状解除時刻
    locked_by                        BIGINT REFERENCES route (id) DEFAULT NULL,        -- 鎖状している進路のID
    is_correction_raise_relay_raised raise_drop NOT NULL          DEFAULT 'drop',      -- 不正扛上補正リレー
    raised_at                        TIMESTAMP                    DEFAULT NULL,        -- 軌道回路を扛上させるタイミング
    is_correction_drop_relay_raised  raise_drop NOT NULL          DEFAULT 'drop',      -- 不正落下補正リレー
    dropped_at                       TIMESTAMP                    DEFAULT NULL         -- 軌道回路を落下させるタイミング
);
CREATE INDEX track_circuit_state_train_number_index ON track_circuit_state USING hash (train_number);


-- 転てつ機状態
CREATE TABLE switching_machine_state
(
    id              BIGINT PRIMARY KEY REFERENCES switching_machine (ID), -- 転てつ機のID
    is_switching    BOOLEAN   NOT NULL,                                   -- 転換中
    is_reverse      nr        NOT NULL,                                   -- 定反
    switch_end_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP          -- 転換終了時刻
);

-- 進路状態
CREATE TABLE route_state
(
    id                           BIGINT PRIMARY KEY REFERENCES route (ID), -- 進路のID
    is_lever_relay_raised        raise_drop NOT NULL,                      -- てこリレーが上がっているか
    is_route_relay_raised        raise_drop NOT NULL,                      -- 進路照査リレーが上がっているか
    is_signal_control_raised     raise_drop NOT NULL,                      -- 信号制御リレーが上がっているか
    is_approach_lock_mr_raised   raise_drop NOT NULL,                      -- 接近鎖状が上がっているか
    is_approach_lock_ms_raised   raise_drop NOT NULL,                      -- 接近鎖状が上がっているか
    is_route_lock_raised         raise_drop NOT NULL,                      -- 進路鎖状が上がっているか
    is_throw_out_xr_relay_raised raise_drop NOT NULL,                      -- 統括制御リレーが上がっているか
    is_throw_out_ys_relay_raised raise_drop NOT NULL                       -- 統括制御リレーが上がっているか
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

-- 運転告知状態
CREATE TYPE operation_notification_type AS ENUM (
    'none',
    'yokushi',
    'tsuuchi',
    'tsuuchi_kaijo',
    'kaijo',
    'shuppatsu',
    'shuppatsu_jikoku',
    'torikeshi',
    'other',
    'class',
    'tenmatsusho'
);
CREATE TABLE operation_notification_state
(
    display_name VARCHAR(100) REFERENCES operation_notification_display (name) PRIMARY KEY, -- 告知機の名前
    type         operation_notification_type NOT NULL,                                      -- 告知種類 
    content      TEXT                        NOT NULL,                                      -- 表示データ
    operated_at  TIMESTAMP                   NOT NULL                                       -- 操作時刻
);

-- TTC状態
CREATE TABLE ttc_window_state
(
    name         VARCHAR(100) REFERENCES ttc_window (name) NOT NULL, -- 列番窓の名前
    train_number VARCHAR(100)                              NOT NULL  -- 列車番号
);

-- 列車状態
CREATE TABLE train_state
(
    id              BIGSERIAL PRIMARY KEY,                         -- 列車状態のID
    train_number    VARCHAR(100) NOT NULL UNIQUE,                  -- 列車番号
    dia_number      INT          NOT NULL UNIQUE,                  -- 運行番号
    from_station_id VARCHAR(10)  NOT NULL REFERENCES station (id), -- 出発駅ID
    to_station_id   VARCHAR(10)  NOT NULL REFERENCES station (id), -- 到着駅ID
    delay           INT          NOT NULL DEFAULT 0,               -- 遅延時間(秒)
    driver_id       BIGINT UNIQUE                                  -- 運転士ID(列車の運転士)
);

-- 列車車両情報
CREATE TABLE train_car_state
(
    train_state_id    BIGINT REFERENCES train_state (id) NOT NULL,               -- 列車状態のID
    index             INT                                NOT NULL,               -- インデックス
    car_model         VARCHAR(100)                       NOT NULL,               -- 車両形式
    has_pantograph    BOOLEAN                            NOT NULL DEFAULT false, -- パンタグラフの有無
    has_driver_cab    BOOLEAN                            NOT NULL DEFAULT false, -- 運転台の有無
    has_conductor_cab BOOLEAN                            NOT NULL DEFAULT false, -- 車掌室の有無
    has_motor         BOOLEAN                            NOT NULL DEFAULT false, -- 電動機ありなし
    door_close        BOOLEAN                            NOT NULL DEFAULT true,  -- 扉閉め状態
    bc_press          DOUBLE PRECISION                   NOT NULL DEFAULT 0,     -- ブレーキ圧力
    ampare            DOUBLE PRECISION                   NOT NULL DEFAULT 0,     -- 電流値
    PRIMARY KEY (train_state_id, index)
);