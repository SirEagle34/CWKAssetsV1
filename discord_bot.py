"""
CWK Discord bot for PythonAnywhere.
Runs as a separate process from Flask app.py and reads SQLite in read-only mode.
Set DISCORD_BOT_TOKEN and CWK_DATABASE_PATH as environment variables.
"""
import asyncio
import logging
import os
import sqlite3
import time
from datetime import datetime, timezone
from urllib.error import URLError
from urllib.request import Request, urlopen

import discord
from discord import app_commands
from discord.ext import commands, tasks

logging.basicConfig(
    level=os.getenv("LOG_LEVEL", "INFO").upper(),
    format="%(asctime)s %(levelname)s %(name)s: %(message)s",
)
log = logging.getLogger("cwk-discord")

TOKEN = os.getenv("DISCORD_BOT_TOKEN", "").strip()
DATABASE_PATH = os.getenv("CWK_DATABASE_PATH", "").strip()
HEALTH_URL = os.getenv("CWK_HEALTH_URL", "").strip()
GUILD_ID = os.getenv("DISCORD_GUILD_ID", "").strip()
ANNOUNCE_CHANNEL_ID = os.getenv("DISCORD_ANNOUNCE_CHANNEL_ID", "").strip()
STATUS_CHANNEL_ID = os.getenv("DISCORD_STATUS_CHANNEL_ID", "").strip()
STATUS_INTERVAL = max(60, int(os.getenv("DISCORD_STATUS_INTERVAL", "300")))

intents = discord.Intents.default()
bot = commands.Bot(command_prefix="!", intents=intents)


def read_database_stats():
    """Read only basic counts; discover the schema rather than assuming columns."""
    if not DATABASE_PATH:
        return {"ok": False, "error": "CWK_DATABASE_PATH is not configured"}
    if not os.path.isfile(DATABASE_PATH):
        return {"ok": False, "error": "Configured SQLite database was not found"}

    connection = None
    try:
        uri = "file:" + os.path.abspath(DATABASE_PATH).replace("\\", "/") + "?mode=ro"
        connection = sqlite3.connect(uri, uri=True, timeout=3)
        connection.row_factory = sqlite3.Row
        connection.execute("PRAGMA query_only = ON")
        tables = {
            row["name"] for row in connection.execute(
                "SELECT name FROM sqlite_master WHERE type='table'"
            )
        }
        result = {"ok": True, "tables": len(tables), "players": None}
        if "player" in tables:
            # Table names are discovered from sqlite_master; quote the fixed identifier.
            result["players"] = connection.execute('SELECT COUNT(*) FROM "player"').fetchone()[0]
        return result
    except (sqlite3.Error, OSError) as exc:
        log.warning("Database stats unavailable: %s", exc)
        return {"ok": False, "error": "SQLite query failed"}
    finally:
        if connection is not None:
            connection.close()


def check_health_url():
    if not HEALTH_URL:
        return None
    try:
        req = Request(HEALTH_URL, headers={"User-Agent": "CWK-Discord-Bot/1.0"})
        with urlopen(req, timeout=5) as response:
            return 200 <= response.status < 400
    except (URLError, TimeoutError, OSError, ValueError):
        return False


def is_admin(interaction: discord.Interaction) -> bool:
    perms = interaction.user.guild_permissions if isinstance(
        interaction.user, discord.Member
    ) else None
    return bool(perms and (perms.administrator or perms.manage_guild))


@bot.event
async def setup_hook():
    if GUILD_ID.isdigit():
        guild = discord.Object(id=int(GUILD_ID))
        bot.tree.copy_global_to(guild=guild)
        synced = await bot.tree.sync(guild=guild)
        log.info("Synced %s slash commands to guild %s", len(synced), GUILD_ID)
    else:
        synced = await bot.tree.sync()
        log.info("Synced %s global slash commands", len(synced))


@bot.event
async def on_ready():
    log.info("Logged in as %s (%s)", bot.user, bot.user.id if bot.user else "?")
    if not refresh_presence.is_running():
        refresh_presence.start()


@bot.tree.command(name="status", description="Show CWK service and database status")
async def status(interaction: discord.Interaction):
    await interaction.response.defer(ephemeral=True)
    stats = await asyncio.to_thread(read_database_stats)
    health = await asyncio.to_thread(check_health_url)
    embed = discord.Embed(
        title="Card Wars Kingdom — Status",
        color=discord.Color.green() if stats["ok"] and health is not False else discord.Color.orange(),
        timestamp=datetime.now(timezone.utc),
    )
    embed.add_field(name="SQLite", value="Connected" if stats["ok"] else "Unavailable", inline=True)
    if stats.get("players") is not None:
        embed.add_field(name="Player records", value=str(stats["players"]), inline=True)
    if health is not None:
        embed.add_field(name="Web/API", value="Online" if health else "Unreachable", inline=True)
    elif not stats["ok"]:
        embed.add_field(name="Details", value=stats["error"], inline=False)
    embed.set_footer(text="CWK server status")
    await interaction.followup.send(embed=embed)


@bot.tree.command(name="players", description="Show the total player records in the CWK database")
async def players(interaction: discord.Interaction):
    await interaction.response.defer(ephemeral=True)
    stats = await asyncio.to_thread(read_database_stats)
    if not stats["ok"]:
        await interaction.followup.send(
            "Player statistics are unavailable. Check CWK_DATABASE_PATH and database permissions.",
            ephemeral=True,
        )
        return
    if stats.get("players") is None:
        await interaction.followup.send(
            "The configured database does not contain a 'player' table.", ephemeral=True
        )
        return
    await interaction.followup.send(
        f"**Player records:** {stats['players']:,}\n"
        "_This is the number of database records, not live online players._",
        ephemeral=True,
    )


@bot.tree.command(name="announce", description="Post a community announcement")
@app_commands.describe(message="Announcement text to publish")
async def announce(interaction: discord.Interaction, message: str):
    if not is_admin(interaction):
        await interaction.response.send_message(
            "You need Manage Server or Administrator permission to use this command.",
            ephemeral=True,
        )
        return
    if not ANNOUNCE_CHANNEL_ID.isdigit():
        await interaction.response.send_message(
            "Announcement channel is not configured. Set DISCORD_ANNOUNCE_CHANNEL_ID.",
            ephemeral=True,
        )
        return
    channel = bot.get_channel(int(ANNOUNCE_CHANNEL_ID))
    if channel is None:
        try:
            channel = await bot.fetch_channel(int(ANNOUNCE_CHANNEL_ID))
        except (discord.NotFound, discord.Forbidden, discord.HTTPException):
            channel = None
    if channel is None or not hasattr(channel, "send"):
        await interaction.response.send_message(
            "Announcement channel could not be accessed. Check its ID and bot permissions.",
            ephemeral=True,
        )
        return
    safe_message = message.strip()
    if not safe_message or len(safe_message) > 1800:
        await interaction.response.send_message(
            "Message must contain 1–1800 characters.", ephemeral=True
        )
        return
    embed = discord.Embed(
        title="📢 CWK Community Announcement",
        description=safe_message,
        color=discord.Color.blurple(),
        timestamp=datetime.now(timezone.utc),
    )
    await channel.send(embed=embed, allowed_mentions=discord.AllowedMentions.none())
    await interaction.response.send_message("Announcement posted.", ephemeral=True)


@bot.tree.command(name="poll", description="Create a simple community poll")
@app_commands.describe(question="Poll question", option_a="First option", option_b="Second option")
async def poll(interaction: discord.Interaction, question: str, option_a: str, option_b: str):
    if not isinstance(interaction.user, discord.Member) or not (
        interaction.user.guild_permissions.send_messages
    ):
        await interaction.response.send_message("You cannot create a poll here.", ephemeral=True)
        return
    values = [question.strip(), option_a.strip(), option_b.strip()]
    if any(not value for value in values) or any(len(value) > 900 for value in values):
        await interaction.response.send_message(
            "Question and options must each contain 1–900 characters.", ephemeral=True
        )
        return
    embed = discord.Embed(title=values[0], color=discord.Color.blurple())
    embed.add_field(name="🇦 Option A", value=values[1], inline=False)
    embed.add_field(name="🇧 Option B", value=values[2], inline=False)
    await interaction.response.send_message(embed=embed)
    message = await interaction.original_response()
    await message.add_reaction("🇦")
    await message.add_reaction("🇧")


@tasks.loop(seconds=STATUS_INTERVAL)
async def refresh_presence():
    stats = await asyncio.to_thread(read_database_stats)
    if not stats["ok"]:
        activity = discord.Game(name="Server status unavailable")
    elif stats.get("players") is not None:
        activity = discord.Game(name=f"{stats['players']:,} player records")
    else:
        activity = discord.Game(name="Card Wars Kingdom")
    await bot.change_presence(status=discord.Status.online, activity=activity)

    if STATUS_CHANNEL_ID.isdigit():
        channel = bot.get_channel(int(STATUS_CHANNEL_ID))
        if channel is not None and hasattr(channel, "send"):
            health = await asyncio.to_thread(check_health_url)
            if health is False:
                # Keep alerts conservative; this reports only a configured health URL failure.
                log.warning("Configured CWK health URL is unreachable")


@refresh_presence.before_loop
async def before_refresh_presence():
    await bot.wait_until_ready()


def main():
    if not TOKEN:
        raise SystemExit("Missing DISCORD_BOT_TOKEN environment variable.")
    if not DATABASE_PATH:
        log.warning("CWK_DATABASE_PATH is not set; database commands will report unavailable.")
    bot.run(TOKEN, log_handler=None)


if __name__ == "__main__":
    main()
