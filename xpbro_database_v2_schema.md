# XPBRO Database V2 - Schema Documentation

This document provides an overview of the PostgreSQL/Supabase database schema for **XPBRO V2**, a platform designed for managing user accounts, connected gaming platforms, game libraries, statistics, and synchronization logs.

---

## 📊 Database Entity-Relationship Diagram (ASCII)

```text
 +-------------------+          +-------------------+          +-------------------+
 |     xpbro.users   |          |    xpbro.roles    |          | xpbro.platforms   |
 +-------------------+          +-------------------+          +-------------------+
 | PK id (UUID)      |<-------->| PK id (SERIAL)    |          | PK id (UUID)      |
 |    email (CITEXT) |          |    name           |          |    slug           |
 |    password_hash  |          +-------------------+          |    auth_type      |
 |    is_active      |                    ^                    +-------------------+
 +-------------------+                    |                              ^
           |                              v                              |
           |                    +-------------------+                    |
           +------------------->|xpbro.user_roles   |<---+               |
           |                    +-------------------+    |               |
           |                    | PK,FK user_id     |    |               |
           |                    | PK,FK role_id     |    |               |
           |                    +-------------------+    |               |
           |                                             |               |
           v                                             |               |
 +-------------------+                                   |               |
 |xpbro.refresh_toks |                                   |               |
 +-------------------+                                   |               |
 | PK id (UUID)      |                                   |               |
 | FK user_id        |                                   |               |
 +-------------------+                                   |               |
           |                                             |               |
           v                                             |       +-------------------+
 +-------------------+                                   |       | xpbro.user_plats  |
 |xpbro.user_sessions|                                   |       +-------------------+
 +-------------------+                                   |       | PK id (UUID)      |
 | PK id (UUID)      |                                   |       | FK user_id        |<--+
 | FK user_id        |                                   |       | FK platform_id    |---|
 | FK refresh_tok_id |---+                               |       +-------------------+   |
 +-------------------+   |                               |                 |             |
                         v                               |                 |             |
           +---------------------------------------------+                 v             |
           |                                                       +---------------+     |
           v                                                       | xpbro.games   |     |
 +----------------------------------+                              +---------------+     |
 |xpbro.platform_sync_logs          |                              | PK id (UUID)  |     |
 +----------------------------------+                              | FK platform_id|-----+
 | PK id (UUID)                     |                              |    name       |
 | FK user_platform_id ------------>|                              +---------------+
 +----------------------------------+                                      ^
                                                                           |
                                   +---------------------------------------+
                                   |
                                   v
                         +-------------------+           +-------------------+
                         | xpbro.user_games  |           | xpbro.game_stats  |
                         +-------------------+           +-------------------+
                         | PK id (UUID)      |           | PK id (UUID)      |
                         | FK user_id        |           | FK user_platform_id
                         | FK game_id -------+---------->| FK game_id        |
                         |    playtime_mins  |           |    playtime_mins  |
                         |    status         |           |    wins / losses  |
                         +-------------------+           +-------------------+
```

---

## 🏗️ Architecture Overview

The schema is encapsulated within the dedicated `xpbro` schema namespace. It utilizes modern PostgreSQL extensions (`pgcrypto` for secure identifiers and hashing, and `citext` for case-insensitive email handling) and ensures idempotency through `IF NOT EXISTS` clauses and `ON CONFLICT` constraints.

---

## 👥 Core & Identity Tables

### 1. `xpbro.users`
Stores core user account credentials, profile details, and active status.
* **`id`**: `UUID` (Primary Key, default `gen_random_uuid()`)
* **`email`**: `CITEXT` (Unique, case-insensitive email address)
* **`password_hash`**: `TEXT` (Hashed password for standard accounts)
* **`google_id`**: `VARCHAR(255)` (Optional OAuth identifier for Google sign-in)
* **`first_name`** / **`last_name`**: `VARCHAR(100)`
* **`email_confirmed`**: `BOOLEAN` (Defaults to `false`)
* **`is_active`**: `BOOLEAN` (Defaults to `true`)
* **`created_at`** / **`updated_at`**: `TIMESTAMPTZ` (Auto-updated via trigger `trg_users_updated_at`)

### 2. `xpbro.roles` & `xpbro.user_roles`
Manages Role-Based Access Control (RBAC). 
* **`roles`**: Contains system definitions such as `Admin`, `Manager`, and `User`.
* **`user_roles`**: Junction table mapping users to their respective roles with a composite primary key `(user_id, role_id)`.

---

## 🔐 Authentication & Security

### 3. `xpbro.refresh_tokens`
Handles secure session management via refresh tokens.
* **`user_id`**: References `xpbro.users(id)` (Cascade delete)
* **`token`**: `TEXT` (Unique refresh token string)
* **`expires_at`**: `TIMESTAMPTZ`
* **`revoked_at`** & **`replaced_by_token`**: Tracks token lifecycle and rotation.

### 4. `xpbro.email_verification_tokens` & `xpbro.password_reset_tokens`
Temporary token tables used for account verification and password recovery processes. Both utilize UUID tokens with expiration tracking and `used_at` timestamps.

### 5. `xpbro.user_sessions`
Monitors active user login sessions, capturing device metadata.
* **`ip_address`**: `VARCHAR(100)`
* **`user_agent`**: `TEXT`
* **`last_seen_at`**: `TIMESTAMPTZ`

### 6. `xpbro.audit_logs`
Tracks critical user and system actions for security and compliance.
* **`action`** & **`entity`**: `VARCHAR(100)` descriptors.
* **`details`**: `JSONB` payload for flexible logging data.

---

## 🎮 Gaming & Platforms Tables

### 7. `xpbro.platforms`
Stores supported external gaming platforms (e.g., Steam, Epic Games, Board Game Arena, BoardGameGeek).
* **`slug`**: `VARCHAR(50)` (Unique identifier slug)
* **`auth_type`**: Restricted via check constraint to: `public`, `openid`, `oauth2`, `api_key`, or `manual`.

### 8. `xpbro.user_platforms`
Links user accounts to external gaming accounts, securely storing connection credentials and tokens.
* **`user_id`** / **`platform_id`**: Foreign keys establishing the connection.
* **`external_user_id`** / **`external_username`**: Identity fields on the target platform.
* **`access_token`** / **`refresh_token`**: Stored securely for API communication.

### 9. `xpbro.games`
Central catalog of games mapped to their respective platforms.
* **`external_game_id`** + **`platform_id`**: Enforces a unique composite constraint.
* **`metadata`**: `JSONB` data structure (defaults to `{}`::jsonb) for platform-specific game attributes.

### 10. `xpbro.user_games`
Represents a user's personal game collection and tracking data.
* **`playtime_minutes`**: `INT` tracking total time spent.
* **`status`**: `VARCHAR(30)` (e.g., `unplayed`).
* **`favorite`**: `BOOLEAN` flag.
* **Constraint**: `user_game_unique (user_id, game_id)` ensures a game is only added once per user library.

### 11. `xpbro.game_stats`
Stores detailed user statistics and performance metrics per game.
* **`user_platform_id`** & **`game_id`**: Composite unique key mapping stats to a specific user platform profile and game.
* **`playtime_minutes`**: `BIGINT` tracking cumulative playtime.
* **`matches_played`**, **`wins`**, **`losses`**: Non-negative integer counters enforced via check constraints.
* **`extra_data`**: `JSONB` field indexed with **GIN** for high-performance querying of arbitrary metrics.

### 12. `xpbro.platform_sync_logs`
Monitors and logs synchronization history with external platform APIs.
* **`sync_type`**: Restricted to `full`, `games`, `stats`, `achievements`, or `profile`.
* **`status`**: Restricted to `running`, `success`, `partial`, or `failed`.
* **`duration_ms`**: Tracks execution duration.
* **`metadata`**: `JSONB` for additional debugging payloads.

---

## 🚀 Initial Seed Data

The schema automatically seeds default roles and platform integrations upon deployment:
* **Roles**: `Admin`, `Manager`, `User`
* **Platforms**: `Steam` (`openid`), `Epic Games` (`public`), `Board Game Arena` (`manual`), `BoardGameGeek` (`public`)
