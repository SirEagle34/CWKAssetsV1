-- Google account linking migration.
-- Run this explicitly against the same SQLite database used by Flask.
-- This file is intentionally NOT executed automatically by app.py.

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS google_account_links (
    google_sub TEXT NOT NULL PRIMARY KEY,
    player_username VARCHAR(80) NOT NULL UNIQUE,
    email VARCHAR(320) NOT NULL DEFAULT '',
    linked_at INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_google_account_links_player
    ON google_account_links (player_username);

CREATE TABLE IF NOT EXISTS google_account_tickets (
    ticket_hash VARCHAR(64) NOT NULL PRIMARY KEY,
    mode VARCHAR(16) NOT NULL,
    player_username VARCHAR(80),
    oauth_state VARCHAR(128) NOT NULL UNIQUE,
    status VARCHAR(24) NOT NULL DEFAULT 'pending',
    result_username VARCHAR(80),
    email VARCHAR(320),
    google_sub TEXT,
    error VARCHAR(240),
    can_create INTEGER NOT NULL DEFAULT 0,
    created_at INTEGER NOT NULL,
    expires_at INTEGER NOT NULL,
    consumed INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_google_account_tickets_expiry
    ON google_account_tickets (expires_at);

CREATE INDEX IF NOT EXISTS ix_google_account_tickets_state
    ON google_account_tickets (oauth_state);
