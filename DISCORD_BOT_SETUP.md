# CWK Discord Bot — PythonAnywhere setup

This bot is a separate process from Flask `app.py`. It reads the configured SQLite database in read-only mode and does not modify player data.

## Commands

- `/status` — SQLite availability, player-record count when the `player` table exists, and optional HTTP health check.
- `/players` — total rows in the `player` table. This is **not** a live online-player count.
- `/announce message:` — sends a community announcement to the configured channel. Requires Manage Server or Administrator permission.
- `/poll question: option_a: option_b:` — creates a two-option reaction poll.

The bot presence shows a database player-record count when available. It does not claim that these records are currently online.

## 1. Create the Discord application

1. Open https://discord.com/developers/applications and create an application.
2. Under **Bot**, add a bot and reset/copy its token. Keep the token secret.
3. Under **OAuth2 → URL Generator**, select the `bot` and `applications.commands` scopes.
4. Grant only needed bot permissions: View Channels, Send Messages, Embed Links, Add Reactions, and Read Message History as needed.
5. Open the generated URL and invite the bot to your community server.
6. Copy the server ID and announcement channel ID with Discord Developer Mode enabled.

## 2. Add files to PythonAnywhere

Upload `discord_bot.py` and `discord_bot_requirements.txt` to the same project directory as `app.py`, or clone/pull the `test` branch.

Create a virtual environment if the web app does not already have one, then install the bot dependency in that environment:

```bash
pip install -r discord_bot_requirements.txt
```

If the web app uses a dedicated virtual environment, use that environment's full Python/pip paths.

## 3. Set environment variables

Set these in the process environment used to start the bot:

- `DISCORD_BOT_TOKEN` — bot token from the Developer Portal.
- `CWK_DATABASE_PATH` — **absolute path** to the actual SQLite database used by this deployment.
- `CWK_HEALTH_URL` — optional public health endpoint.
- `DISCORD_GUILD_ID` — optional test server ID; guild commands normally appear faster during development.
- `DISCORD_ANNOUNCE_CHANNEL_ID` — optional announcement channel ID.
- `DISCORD_STATUS_INTERVAL` — optional refresh interval, default 300 seconds.

Do not commit real tokens, passwords, or production database paths. Do not assume the database path in this example matches your PythonAnywhere deployment.

## 4. Run it as a persistent process

A Flask web app's WSGI process does not automatically run a Discord Gateway bot. Start this separately:

```bash
python discord_bot.py
```

On PythonAnywhere, use an **Always-on task** if your account supports it. A normal scheduled task is not suitable for a continuously connected Gateway bot. Check your account's outbound network access and PythonAnywhere's current Discord/WebSocket policy before deploying; if persistent outbound connections are unavailable on your plan, host the bot on a platform that supports always-on processes while keeping the Flask app and SQLite database on PythonAnywhere. Do not start the bot at Flask module import time, because WSGI reloads can create duplicate bot processes.

## 5. Slash-command sync

For initial testing, set `DISCORD_GUILD_ID` to the test server ID. Restart the bot and check its logs for “Synced … slash commands”. Leave the value blank only when you want global command registration, which can take longer to appear.

## 6. Validation checklist

- Confirm the bot is online in Discord.
- Run `/status` and verify the SQLite connection.
- Run `/players` and confirm the count is plausible.
- Test `/announce` as a server manager and as a regular member; only the manager should succeed.
- Test `/poll` in a channel where the bot can send messages and add reactions.
- Confirm the bot token is not present in Git history, logs, or screenshots.

## Important limitations

- The bot only counts rows in the `player` table; it does not infer online users from database records.
- `CWK_HEALTH_URL` must be a public, safe endpoint. Do not point it at admin pages or endpoints that disclose player data.
- SQLite is opened in read-only mode. The bot cannot edit, ban, grant currency, or change game state.
- If your deployment uses multiple databases (for example, primary and LocalInstance), set `CWK_DATABASE_PATH` to the database you want the bot to report. This version reports one database only.
