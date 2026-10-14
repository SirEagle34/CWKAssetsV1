# Google account linking setup

The Google endpoints in `app.py` require an explicit database migration. The application does **not** create these Google tables at startup.

## 1. Configure Google OAuth

In Google Cloud Console, configure an OAuth 2.0 Web application client. Add this exact redirect URI to the client:

```
https://YOUR_PUBLIC_HOST/account/google/callback
```

Set these environment variables on the server (do not put the client secret in Unity or commit it to Git):

- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`
- `GOOGLE_REDIRECT_URI` — the exact callback URL registered above

Restart the Flask service after setting them.

## 2. Apply the migration once

Back up the active database first. Run `migrations/20261009_google_accounts.sql` against the **same SQLite database file** configured for Flask-SQLAlchemy. With the common Flask-SQLAlchemy instance-relative layout, that file is often `instance/cardwarskingdom.db`; confirm the real path on your host before running this command.

Example only, if the active database is `instance/cardwarskingdom.db`:

```sh
sqlite3 instance/cardwarskingdom.db < migrations/20261009_google_accounts.sql
```

Do not run this against a different or empty database. The migration adds only `google_account_links` and `google_account_tickets`; it does not alter the existing `player` table.

## 3. Endpoints used by Unity

- `POST /account/google/begin`
- `GET /account/google/start?ticket=...`
- `GET /account/google/callback` (Google redirect)
- `GET /account/google/poll?ticket=...`
- `POST /account/google/create`
- `POST /account/google/status`
- `POST /account/google/unlink`

Link, relink, status, and unlink requests use the existing `Player-Id` header. The ticket is random, stored as a SHA-256 hash, and expires after five minutes.

## Notes

- Account recovery is based on Google's verified stable subject (`sub`), not an email address supplied by the client.
- New Google sign-ins without an existing link can create a new game account after the server verifies the Google identity.
- The current status endpoint returns `reward: false`; no linking reward is granted until a reward policy and idempotent reward bookkeeping are explicitly implemented.
- Test with a non-production account and a database backup before enabling this for players.
