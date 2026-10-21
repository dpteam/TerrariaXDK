using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Terraria;

/// <summary>
/// Text this port introduced itself: chat replies from cheats and host commands, moderation
/// notices. <see cref="Lang"/> covers the original game's strings; nothing in it would ever
/// hold "!kick &lt;player&gt; [reason]", so these lived as English literals in code and a player
/// running the game in German saw a German world with an English console.
///
/// Keys are stable identifiers, not the text: <c>chat.kick.usage</c>. Renaming the English
/// wording never breaks a translation file. Placeholders are <c>{0}</c>-style and are filled
/// by <see cref="Format"/>, which does not use <c>string.Format</c> - see the note there.
///
/// Overrides come from the same per-language file as the game's own text
/// (<c>Storage/Lang/&lt;code&gt;.txt</c>, section <c>[Texts]</c>), so one file holds everything a
/// translator needs to touch.
/// </summary>
internal static class Texts
{
	/// <summary>Everything the game can say, sorted - the order a template lists them in.</summary>
	private static readonly List<string> Order = new List<string>();

	/// <summary>Built-in English text. A key that is missing here is a bug, not a fallback.</summary>
	private static readonly Dictionary<string, string> Defaults = Build();

	/// <summary>Text read from the language file. Only these keys differ from <see cref="Defaults"/>.</summary>
	private static readonly Dictionary<string, string> Overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static Dictionary<string, string> Build()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Add(dictionary, "chat.cheats.unavailable.connected",
			"Cheats only work in single player (you are connected as {0}).");
		Add(dictionary, "chat.cheats.unavailable.disabled",
			"Cheats are disabled in Settings.ini ([Cheats] Enabled=False).");
		Add(dictionary, "chat.cheats.failed", "Command failed: {0}");
		Add(dictionary, "chat.cheats.unknown", "Unknown command '!{0}'. Type !help.");
		Add(dictionary, "chat.cheats.list", "Cheat commands (!cmd):");
		Add(dictionary, "chat.cheats.state", "Cheats: {0} {1}");
		Add(dictionary, "chat.cheats.on", "on");
		Add(dictionary, "chat.cheats.off", "off");
		Add(dictionary, "chat.cheats.god", "god mode");
		Add(dictionary, "chat.cheats.noclip", "no-clip");
		Add(dictionary, "chat.cheats.mana", "infinite mana");
		Add(dictionary, "chat.cheats.buffs", "infinite buffs");
		Add(dictionary, "chat.cheats.healed", "healed");
		Add(dictionary, "chat.cheats.bloodmoon", "blood moon");
		Add(dictionary, "chat.cheats.meteor", "meteor incoming");
		Add(dictionary, "chat.cheats.killall", "removed {0} enemies");
		Add(dictionary, "chat.cheats.time", "time set to {0}");
		Add(dictionary, "chat.cheats.gave", "gave {0} x{1}");
		Add(dictionary, "chat.cheats.teleported", "teleported to {0},{1}");
		Add(dictionary, "chat.cheats.spawned", "spawned npc {0}");
		Add(dictionary, "chat.cheats.full", "Inventory is full");
		Add(dictionary, "chat.cheats.no.item", "Unknown item: {0}");
		Add(dictionary, "chat.cheats.usage.time", "Usage: !time <dawn|noon|dusk|midnight>");
		Add(dictionary, "chat.cheats.usage.give", "Usage: !give <item> [count]");
		Add(dictionary, "chat.cheats.usage.tp", "Usage: !tp <x> <y>  (tile coordinates)");
		Add(dictionary, "chat.cheats.usage.spawn", "Usage: !spawn <npcId>");
		Add(dictionary, "chat.cheats.help.god", "toggle god mode");
		Add(dictionary, "chat.cheats.help.noclip", "toggle no-clip");
		Add(dictionary, "chat.cheats.help.mana", "infinite mana");
		Add(dictionary, "chat.cheats.help.buffs", "infinite buffs");
		Add(dictionary, "chat.cheats.help.heal", "restore health and mana");
		Add(dictionary, "chat.cheats.help.time", "set the time of day");
		Add(dictionary, "chat.cheats.help.bloodmoon", "toggle the blood moon");
		Add(dictionary, "chat.cheats.help.give", "add an item to the inventory");
		Add(dictionary, "chat.cheats.help.tp", "teleport to tile coordinates");
		Add(dictionary, "chat.cheats.help.spawn", "spawn an NPC at your position");
		Add(dictionary, "chat.cheats.help.meteor", "drop a meteor");
		Add(dictionary, "chat.cheats.help.killall", "remove all enemies");
		Add(dictionary, "chat.cheats.help.help", "list commands");
#if DEBUG
		// Only referenced by the Debug-only commands, so only compiled there.
		Add(dictionary, "chat.debugkit.help", "give a test character with the tools to get anywhere");
		Add(dictionary, "chat.debugkit.given", "test kit");
		Add(dictionary, "chat.debugkit.allitems", "fill the inventory with one page of every item id");
		Add(dictionary, "chat.debugkit.usage", "Usage: !allitems [page 0-{0}]");
		Add(dictionary, "chat.debugkit.items", "gave {0} item(s) from page {1} of 0-{2}");
#endif
		Add(dictionary, "chat.host.not.hosting", "Commands only work while hosting a multiplayer world.");
		Add(dictionary, "chat.host.forbidden", "{0} may not use {1}.");
		Add(dictionary, "chat.host.you", "You");
		Add(dictionary, "chat.anticheat.refused", "Move refused: {0}");
		Add(dictionary, "chat.host.anticheat.none", "Anti-cheat: {0}");
		Add(dictionary, "chat.host.anticheat.set", "Anti-cheat is now {0}");
		Add(dictionary, "chat.host.anticheat.usage", "Usage: !anticheat [off|observe|enforce]");
		Add(dictionary, "chat.host.anticheat.saved", "Saved to {0}.");
		Add(dictionary, "chat.host.anticheat.hint",
			"!anticheat off|observe|enforce changes it. Observe only logs, Enforce also refuses the move.");
		Add(dictionary, "chat.host.usage.say", "Usage: !say <text>");
		Add(dictionary, "chat.host.server.prefix", "[Server] {0}");
		Add(dictionary, "chat.host.server.rules", "[Server] Rules: {0}");
		Add(dictionary, "chat.host.usage.serverconfig", "Usage: !serverconfig [show|create]");
		Add(dictionary, "chat.host.serverconfig.summary",
			"Port {0}, max {1}, password {2}, publish {3}, spawn protection {4}, building {5}");
		Add(dictionary, "chat.host.serverconfig.hint",
			"!serverconfig create writes these values to {0}.");
		Add(dictionary, "chat.host.serverconfig.failed", "Could not write {0}: {1}");
		Add(dictionary, "chat.host.serverconfig.wrote",
			"Wrote {0}; it now overrides Settings.ini for hosting.");
		Add(dictionary, "chat.host.on", "on");
		Add(dictionary, "chat.host.off", "off");
		Add(dictionary, "chat.host.disabled", "disabled");
		Add(dictionary, "chat.host.allowed", "allowed");
		Add(dictionary, "chat.host.item.already", "Item {0} is already banned.");
		Add(dictionary, "chat.host.item.banned", "Banned item {0}.");
		Add(dictionary, "chat.host.item.not", "Item {0} is not banned.");
		Add(dictionary, "chat.host.item.unbanned", "Unbanned item {0}.");
		Add(dictionary, "chat.host.items.none", "No items are banned.");
		Add(dictionary, "chat.host.items.list", "{0} banned: {1}");
		Add(dictionary, "chat.host.stats.players", "Players {0}/{1}");
		Add(dictionary, "chat.host.stats.traffic", "Traffic: {0}");
		Add(dictionary, "chat.host.stats.uptime", "Uptime {0}");
		Add(dictionary, "chat.host.world.name", "World: {0}, {1}x{2} tiles");
		Add(dictionary, "chat.host.world.unnamed", "(unnamed)");
		Add(dictionary, "chat.host.world.spawn",
			"Spawn: ({0}, {1}), surface {2}, rock layer {3}");
		Add(dictionary, "chat.host.world.bosses", "Bosses downed: {0}");
		Add(dictionary, "chat.host.boss.eye", "eye");
		Add(dictionary, "chat.host.boss.eater", "eater");
		Add(dictionary, "chat.host.boss.skeletron", "skeletron");
		Add(dictionary, "chat.host.boss.none", "(none reported so far)");
		Add(dictionary, "chat.host.item.need.id", "Give an item id (number).");
		Add(dictionary, "chat.host.item.id.range", "Item id must be 1-32767.");
		Add(dictionary, "chat.host.item.id.nan",
			"'{0}' is not an item id. This port has no name-to-id table, so use the number.");
		Add(dictionary, "chat.mod.item.blocked", "That item is not allowed on this server.");
		Add(dictionary, "chat.host.region.group.host", "host only");
		Add(dictionary, "chat.host.region.group.named", "group {0}");
		Add(dictionary, "chat.host.region.group.build", "{0} may build");
		Add(dictionary, "chat.host.region.bounds", "{0} ({1},{2})-({3},{4}), {5}");
		Add(dictionary, "chat.host.region.describe", "{0}: {1}x{2} at ({3},{4}), {5}");
		Add(dictionary, "chat.host.serverconfig.using.profile", "Using {0} (overrides Settings.ini)");
		Add(dictionary, "chat.host.serverconfig.using.settings", "Using Settings.ini (no {0})");
		Add(dictionary, "chat.host.region.need.manage", "Changing regions needs the 'manage' permission.");
		Add(dictionary, "chat.host.region.usage",
			"Usage: !region list | add <name> [radius] | remove <name> | group <name> [group]");
		Add(dictionary, "chat.host.region.none",
			"No regions. !region add <name> [radius] protects the area around you.");
		Add(dictionary, "chat.host.region.file", "regions.txt: {0}");
		Add(dictionary, "chat.host.region.usage.add", "Usage: !region add <name> [radius]");
		Add(dictionary, "chat.host.region.radius", "Radius must be 1-500 tiles.");
		Add(dictionary, "chat.host.region.no.world", "Stand in the world first: the region is created around you.");
		Add(dictionary, "chat.host.region.created",
			"Created region '{0}' around you ({1}x{2}), host only. Use !region group {0} <group> to let a group build.");
		Add(dictionary, "chat.host.region.usage.remove", "Usage: !region remove <name>");
		Add(dictionary, "chat.host.region.missing", "No region called '{0}'.");
		Add(dictionary, "chat.host.region.removed", "Removed region '{0}'.");
		Add(dictionary, "chat.host.region.usage.group", "Usage: !region group <name> [group]");
		Add(dictionary, "chat.host.region.host.only", "Region '{0}' is now host only.");
		Add(dictionary, "chat.host.region.group.set", "Region '{0}' is now buildable by group '{1}'.");
		Add(dictionary, "chat.host.usage.op", "Usage: !op <player> [group]");
		Add(dictionary, "chat.host.op.done", "{0} is now in group '{1}' ({2}).");
		Add(dictionary, "chat.host.usage.deop", "Usage: !deop <player>");
		Add(dictionary, "chat.host.deop.none", "'{0}' had no group.");
		Add(dictionary, "chat.host.deop.done", "{0} lost their group.");
		Add(dictionary, "chat.host.groups.none", "No groups defined.");
		Add(dictionary, "chat.host.groups.file", "permissions.ini: {0}");
		Add(dictionary, "chat.host.groups.nobody", "Nobody has a group yet (the host is always the owner).");
		Add(dictionary, "chat.host.usage.mute", "Usage: !mute <player>");
		Add(dictionary, "chat.host.mute.already", "'{0}' is already muted.");
		Add(dictionary, "chat.host.mute.done", "Muted {0}.");
		Add(dictionary, "chat.host.usage.unmute", "Usage: !unmute <player>");
		Add(dictionary, "chat.host.unmute.not", "'{0}' is not muted.");
		Add(dictionary, "chat.host.unmute.done", "Unmuted {0}.");
		Add(dictionary, "chat.host.usage.whisper", "Usage: !whisper <player> <text>");
		Add(dictionary, "chat.host.whisper.missing", "No single player matching '{0}'.");
		Add(dictionary, "chat.host.whisper.sent", "-> {0}: {1}");
		Add(dictionary, "chat.host.motd.none", "No MOTD set.");
		Add(dictionary, "chat.host.motd.show", "MOTD: {0}");
		Add(dictionary, "chat.host.rules.none", "No rules set.");
		Add(dictionary, "chat.host.rules.show", "Rules: {0}");
		Add(dictionary, "chat.host.nobody", "Nobody is connected.");
		Add(dictionary, "chat.host.who", "{0} connected: {1}");
		Add(dictionary, "chat.host.bans.empty", "The ban list is empty.");
		Add(dictionary, "chat.host.bans.list", "{0} banned: {1}");
		Add(dictionary, "chat.host.usage.kick", "Usage: !kick <player> [reason]");
		Add(dictionary, "chat.host.kick.default.reason", "Kicked by the host.");
		Add(dictionary, "chat.host.kick.missing", "No player matching '{0}'.");
		Add(dictionary, "chat.host.kick.done", "Kicked {0}.");
		Add(dictionary, "chat.host.usage.ban", "Usage: !ban <player|ip>");
		Add(dictionary, "chat.host.ban.default.reason", "Banned by the host.");
		Add(dictionary, "chat.host.ban.nothing", "Nothing to do: '{0}' is not connected and not on the ban list.");
		Add(dictionary, "chat.host.ban.done", "Banned {0}.");
		Add(dictionary, "chat.host.ban.done.kicked", "Banned {0} and kicked {1}.");
		Add(dictionary, "chat.host.ban.already", "'{0}' was already banned; kicked {1}.");
		Add(dictionary, "chat.host.usage.unban", "Usage: !unban <player|ip>");
		Add(dictionary, "chat.host.unban.not", "'{0}' is not on the ban list.");
		Add(dictionary, "chat.host.unban.done", "Unbanned {0}.");
		Add(dictionary, "ui.common.on", "On");
		Add(dictionary, "ui.common.off", "Off");
		// The cheat list uses upper case, which some languages write as a separate word form.
		Add(dictionary, "ui.common.on.upper", "ON");
		Add(dictionary, "ui.common.off.upper", "OFF");
		Add(dictionary, "ui.common.none", "(none)");
		Add(dictionary, "ui.common.yes", "Yes");
		Add(dictionary, "ui.common.no", "No");
		Add(dictionary, "ui.common.change.back", "LMB / Left-Right - change    Esc - back");

		// --- Cheat screen ---------------------------------------------------
		Add(dictionary, "ui.cheats.title", "Cheats");
		Add(dictionary, "ui.cheats.page", "< page {0}/{1} >");
		Add(dictionary, "ui.cheats.god", "God mode: {0}");
		Add(dictionary, "ui.cheats.noclip", "No clip: {0}");
		Add(dictionary, "ui.cheats.mana", "Infinite mana: {0}");
		Add(dictionary, "ui.cheats.buffs", "Infinite buffs: {0}");
		Add(dictionary, "ui.cheats.fall", "No fall damage: {0}");
		Add(dictionary, "ui.cheats.heal", "Heal and refill mana");
		Add(dictionary, "ui.cheats.caps", "Max life and mana caps");
		Add(dictionary, "ui.cheats.dawn", "Time: dawn");
		Add(dictionary, "ui.cheats.noon", "Time: noon");
		Add(dictionary, "ui.cheats.dusk", "Time: dusk");
		Add(dictionary, "ui.cheats.midnight", "Time: midnight");
		Add(dictionary, "ui.cheats.bloodmoon", "Blood moon: {0}");
		Add(dictionary, "ui.cheats.eye", "Spawn Eye of Cthulhu");
		Add(dictionary, "ui.cheats.skeletron", "Spawn Skeletron");
		Add(dictionary, "ui.cheats.killall", "Kill all enemies");
		Add(dictionary, "ui.cheats.meteor", "Drop a meteor");
		Add(dictionary, "ui.cheats.recipes", "Unlock all recipes: {0}");
		Add(dictionary, "ui.cheats.save", "Save world and player");
		Add(dictionary, "ui.cheats.single.only", "Cheats are only available in single player");
		Add(dictionary, "ui.cheats.removed", "Removed {0} enemies");
		Add(dictionary, "ui.cheats.status", "Cheats: {0}");

		// --- Mods screen ----------------------------------------------------
		Add(dictionary, "ui.mods.title", "Mods");
		Add(dictionary, "ui.mods.entry.on", "[on]  ");
		Add(dictionary, "ui.mods.entry.off", "[off] ");
		Add(dictionary, "ui.mods.signature", "signature {0}   {1} loaded");
		Add(dictionary, "ui.mods.none", "No mods found in {0}");
		Add(dictionary, "ui.mods.disabled.settings", "mods disabled in settings");
		Add(dictionary, "ui.mods.rescanned", "Rescanned {0}");
		Add(dictionary, "ui.mods.enabled", "{0} enabled - restart required");
		Add(dictionary, "ui.mods.disabled", "{0} disabled - restart required");
		Add(dictionary, "ui.mods.failed", "Could not change {0}: {1}");
		Add(dictionary, "ui.mods.order.failed", "Could not save the load order");
		Add(dictionary, "ui.mods.order.moved", "{0} moved {1} - restart required");
		Add(dictionary, "ui.mods.moved.up", "up");
		Add(dictionary, "ui.mods.moved.down", "down");
		Add(dictionary, "ui.mods.change.back", "LMB / Space - enable   Left-Right - load order   R - rescan   Esc - back");
		Add(dictionary, "ui.mods.hint.restart", "Changes apply after restarting the game");
		Add(dictionary, "ui.mods.errors", "{0} load error(s) - see terraria-debug.log");

		// --- Network screen -------------------------------------------------
		Add(dictionary, "ui.net.title", "Network");
		Add(dictionary, "ui.net.name.title", "Player name (what others see)");
		Add(dictionary, "ui.net.name.usage", "Use 1-{0} characters");
		Add(dictionary, "ui.net.name", "Name: {0}");
		Add(dictionary, "ui.net.name.row", "Player name: {0}");
		Add(dictionary, "ui.net.port.title", "Port (1-65535)");
		Add(dictionary, "ui.net.port.invalid", "Port must be 1-65535");
		Add(dictionary, "ui.net.port.set", "Port: {0} - applies when the next world is hosted");
		Add(dictionary, "ui.net.port", "Port: {0}");
		Add(dictionary, "ui.net.password.title", "Password (empty = anyone)");
		Add(dictionary, "ui.net.password.set", "Password: set - applies when the next world is hosted");
		Add(dictionary, "ui.net.master.title", "Master server (host:port)");
		Add(dictionary, "ui.net.master.invalid", "Could not parse '{0}' - use host:port");
		Add(dictionary, "ui.net.master.set", "Master server: {0}");
		Add(dictionary, "ui.net.publish.on", "Publishing this world while hosting");
		Add(dictionary, "ui.net.publish.off", "Not publishing");
		Add(dictionary, "ui.net.publish", "Publish to master: {0}");
		Add(dictionary, "ui.net.spawn.off", "Spawn protection: off");
		Add(dictionary, "ui.net.spawn", "Spawn protection: {0} tiles");
		Add(dictionary, "ui.net.build.disabled", "Building disabled for everyone but you");
		Add(dictionary, "ui.net.build.allowed", "Building allowed");
		Add(dictionary, "ui.net.build", "Disable building: {0}");
		Add(dictionary, "ui.net.refresh.off", "Auto refresh: off");
		Add(dictionary, "ui.net.refresh", "Auto refresh: {0} s");
		Add(dictionary, "ui.net.off", "off");
		Add(dictionary, "ui.net.hint.scope", "Port and password apply when the next world is hosted");

		// --- Key bindings screen --------------------------------------------
		Add(dictionary, "ui.keys.title", "Keyboard bindings");
		Add(dictionary, "ui.keys.press.for", "Press a key for {0} (Esc cancels)");
		Add(dictionary, "ui.keys.reset", "Bindings reset to defaults");
		Add(dictionary, "ui.keys.cancelled", "Cancelled");
		Add(dictionary, "ui.keys.assigned", "{0} = {1}");
		Add(dictionary, "ui.keys.taken", "{0} is already used by another action");
		Add(dictionary, "ui.keys.waiting", "press a key…");
		Add(dictionary, "ui.keys.change.back", "LMB / Space - rebind   R - reset all   Esc - back");
		Add(dictionary, "ui.keys.more.up", "^ more");
		Add(dictionary, "ui.keys.more.down", "v more");
		Add(dictionary, "ui.key.moveleft", "Move left");
		Add(dictionary, "ui.key.moveright", "Move right");
		Add(dictionary, "ui.key.moveup", "Move up");
		Add(dictionary, "ui.key.movedown", "Move down");
		Add(dictionary, "ui.key.jump", "Jump");
		Add(dictionary, "ui.key.inventory", "Inventory");
		Add(dictionary, "ui.key.throw", "Throw item");
		Add(dictionary, "ui.key.smartcursor", "Smart cursor");
		Add(dictionary, "ui.key.pause", "Pause / back");
		Add(dictionary, "ui.key.map", "Map");
		Add(dictionary, "ui.key.heal", "Quick heal");
		Add(dictionary, "ui.key.mana", "Quick mana");
		Add(dictionary, "ui.key.buff", "Quick buff");
		Add(dictionary, "ui.key.grapple", "Grapple");
		Add(dictionary, "ui.key.chat", "Chat");

		// --- Backups screen -------------------------------------------------
		Add(dictionary, "ui.backups.title", "Backups: {0}");
		Add(dictionary, "ui.backups.none", "No backups.");
		Add(dictionary, "ui.backups.change.back", "LMB / Space - restore    R - rescan    Esc - back");
		Add(dictionary, "ui.backups.no.name", "This world has no name yet, so it has no backups.");
		Add(dictionary, "ui.backups.empty", "No backups yet - they are written when the world is saved.");
		Add(dictionary, "ui.backups.cancelled", "Restore cancelled.");
		Add(dictionary, "ui.backups.count", "{0} backup(s)");
		Add(dictionary, "ui.backups.confirm", "Overwrite '{0}' with this backup? Space again to confirm.");
		Add(dictionary, "ui.backups.failed", "Could not restore: {0}");
		Add(dictionary, "ui.backups.restored", "Restored {0} - reloading the world list");
		Add(dictionary, "ui.settings.cheats", "Cheats: {0}");
		Add(dictionary, "ui.settings.keys", "Keyboard bindings");
		Add(dictionary, "ui.settings.mods", "Mods: {0} loaded");
		Add(dictionary, "ui.settings.display", "Display: {0}x{1}");
		Add(dictionary, "ui.settings.network", "Network: port {0}");
		Add(dictionary, "ui.settings.hud", "HUD: zoom {0}");
		Add(dictionary, "ui.menu.cheats", "Cheats");
		Add(dictionary, "ui.world.backups.pick", "Select a world on the left to see its backups");
		Add(dictionary, "ui.world.slot.empty", "That world slot is empty");
		Add(dictionary, "ui.world.folder.failed", "Could not read the worlds folder");
		Add(dictionary, "ui.server.removed", "Removed from saved servers: {0}");
		Add(dictionary, "ui.server.saved", "Saved server: {0}");
		Add(dictionary, "ui.server.address", "Server address (host or host:port)");
		Add(dictionary, "ui.server.timeout", "timeout");
		Add(dictionary, "ui.name.empty", "The name cannot be empty.");
		Add(dictionary, "ui.name.too.long", "Use at most {0} characters.");
		Add(dictionary, "ui.connect.title", "Connect");
		Add(dictionary, "ui.modmismatch.title", "Mod mismatch");
		Add(dictionary, "ui.modmismatch.text", "Player '{0}' uses a different mod set and cannot join.");
		Add(dictionary, "ui.reject.title", "Connection refused: {0}");
		Add(dictionary, "ui.reject.protocol", "server protocol {0}, yours {1}");
		Add(dictionary, "ui.reject.mods", "this server runs a different mod set");
		Add(dictionary, "ui.reject.banned", "you are banned from this server");
		Add(dictionary, "ui.reject.password", "wrong password");
		Add(dictionary, "ui.reject.full", "the server is full");
		Add(dictionary, "ui.reject.kicked", "disconnected by the host");
		Add(dictionary, "ui.reject.kicked.reason", "disconnected by the host: {0}");
		Add(dictionary, "ui.reject.unknown", "{0}");
		Add(dictionary, "ui.reject.unknown.empty", "the server gave no reason");

		// --- Display screen -------------------------------------------------
		Add(dictionary, "ui.display.title", "Display");
		Add(dictionary, "ui.display.profile", "Profile: {0}");
		Add(dictionary, "ui.display.window", "Window size: {0}x{1}");
		Add(dictionary, "ui.display.fullscreen", "Fullscreen: {0}");
		Add(dictionary, "ui.display.render", "Render scale: {0}");
		Add(dictionary, "ui.display.fps", "FPS counter: {0}");
		Add(dictionary, "ui.display.vsync", "VSync: {0}");
		Add(dictionary, "ui.display.fpslimit", "Frame cap: {0}");
		Add(dictionary, "ui.display.language", "Language: {0}");
		Add(dictionary, "ui.display.hint.restart", "Render scale is applied on the next start");
		Add(dictionary, "ui.display.status.window", "Window: {0}x{1}");
		Add(dictionary, "ui.display.status.fullscreen", "Fullscreen");
		Add(dictionary, "ui.display.status.windowed", "Windowed");
		Add(dictionary, "ui.display.status.restart", "Render scale: {0} - restart required");
		Add(dictionary, "ui.display.status.vsync.capped", "VSync: Off - overridden by the frame cap");
		Add(dictionary, "ui.display.status.cap.off", "Frame cap: off (vsync only)");
		Add(dictionary, "ui.display.status.cap", "Frame cap: {0} fps");
		Add(dictionary, "ui.display.status.profile", "Profile: {0} - render scale applies on the next start");
		Add(dictionary, "ui.display.failed", "Could not change the window: {0}");
		Add(dictionary, "ui.display.cap.overridden", " (off via cap)");

		// --- HUD screen -----------------------------------------------------
		Add(dictionary, "ui.hud.title", "HUD");
		Add(dictionary, "ui.hud.zoom", "Zoom: {0}");
		Add(dictionary, "ui.hud.zoom.max", "Zoom: {0} (maximum)");
		Add(dictionary, "ui.hud.zoom.min", "Zoom: {0} (default)");
		Add(dictionary, "ui.hud.zoom.reset", "Reset zoom");
		Add(dictionary, "ui.hud.zoom.reset.done", "Zoom reset to {0}");
		Add(dictionary, "ui.hud.clock", "Clock: {0}");
		Add(dictionary, "ui.hud.depth", "Depth: {0}");
		Add(dictionary, "ui.hud.compass", "Compass: {0}");
		Add(dictionary, "ui.hud.hardmode", "Hardmode alert: {0}");
		// One phrase per widget state, shared by the row and the status line: two spellings
		// ("with a watch" vs "only with a watch") read like two different settings.
		Add(dictionary, "ui.hud.always", "always on");
		Add(dictionary, "ui.hud.with.watch", "with a watch");
		Add(dictionary, "ui.hud.with.depth", "with a depth meter");
		Add(dictionary, "ui.hud.with.compass", "with a compass");
		Add(dictionary, "ui.hud.change.back", "LMB / Space - change    Esc - back");
		Add(dictionary, "ui.hud.hint.zoom", "Zoom also works in game: + / - (or triggers while paused)");
		Add(dictionary, "chat.host.help",
			"!kick <player> [reason], !ban/!unban <player|ip>, !bans, !who, !mute/!unmute <player>, "
			+ "!whisper <player> <text>, !motd/!rules [text], !say <text>, !op/!deop <player> [group], "
			+ "!groups, !region list|add|remove|group, !additem/!delitem <id>, !items, !stats, !world, !help");
		Add(dictionary, "chat.mod.motd", "MOTD: {0}");
		Add(dictionary, "chat.mod.rules", "Rules: {0}");
		Add(dictionary, "chat.mod.muted", "You are muted by the host.");
		Add(dictionary, "chat.mod.whisper.from", "{0} whispers: {1}");
		Add(dictionary, "chat.mod.whisper.to", "-> {0}: {1}");
		foreach (string key in dictionary.Keys)
		{
			Order.Add(key);
		}
		// Sorted, not registration order: a template that reorders itself between runs is
		// useless for diffing a translation against the previous one.
		Order.Sort(StringComparer.Ordinal);
		return dictionary;
	}

	private static void Add(Dictionary<string, string> target, string key, string text)
	{
		target[key] = text;
	}

	/// <summary>All known keys, sorted, so a template has a stable order.</summary>
	public static IList<string> Keys => Order;

	/// <summary>True when the key exists in the built-in table. Used by the loader and tests.</summary>
	public static bool Has(string key)
	{
		return key != null && Defaults.ContainsKey(key);
	}

	/// <summary>
	/// The text for a key, in the current language when a translation exists and in English
	/// otherwise. An unknown key is returned in angle brackets instead of throwing: a typo in a
	/// key should show up as <c>&lt;chat.host.typo&gt;</c> on screen, where it is obvious, rather
	/// than as a crash in the middle of a chat message.
	/// </summary>
	public static string Get(string key)
	{
		string text;
		if (Overrides.TryGetValue(key, out text))
		{
			return text;
		}
		if (Defaults.TryGetValue(key, out text))
		{
			return text;
		}
		return "<" + key + ">";
	}

	/// <summary>
	/// The text with <c>{0}</c>-style placeholders filled. Deliberately not
	/// <c>string.Format</c>: that throws on a stray brace, and the text comes from a file a
	/// player edits. A broken translation must produce a slightly wrong chat line, not an
	/// exception in the middle of the game loop.
	/// </summary>
	public static string Format(string key, params object[] args)
	{
		return Fill(Get(key), args);
	}

	/// <summary>
	/// Placeholder substitution. Only <c>{0}</c>..<c>{9}</c> are recognised; anything else is
	/// left exactly as written, and a placeholder with no matching argument stays visible
	/// instead of turning into an empty string - a message reading "Kicked ." hides the bug.
	/// </summary>
	internal static string Fill(string text, object[] args)
	{
		if (string.IsNullOrEmpty(text) || args == null || args.Length == 0 || text.IndexOf('{') < 0)
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length + 16);
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c != '{' || i + 2 >= text.Length || text[i + 2] != '}')
			{
				stringBuilder.Append(c);
				continue;
			}
			char c2 = text[i + 1];
			if (c2 < '0' || c2 > '9')
			{
				stringBuilder.Append(c);
				continue;
			}
			int num = c2 - '0';
			if (num >= args.Length)
			{
				stringBuilder.Append(c).Append(c2).Append('}');
				i += 2;
				continue;
			}
			stringBuilder.Append(Convert.ToString(args[num], CultureInfo.InvariantCulture));
			i += 2;
		}
		return stringBuilder.ToString();
	}

	/// <summary>The built-in English text for a key, regardless of any translation.</summary>
	public static string Default(string key)
	{
		string text;
		return (key != null && Defaults.TryGetValue(key, out text)) ? text : null;
	}

	/// <summary>
	/// The placeholder numbers a string uses: "Kicked {0}." gives {0}.
	///
	/// Used to compare a translation with the English it replaces. A translator who drops
	/// <c>{0}</c> produces "Kicked ." - a sentence that reads like a bug in the command, at the
	/// one moment (someone being kicked) when the host has no patience for mysteries.
	/// </summary>
	internal static List<int> Placeholders(string text)
	{
		List<int> list = new List<int>();
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		for (int i = 0; i + 2 < text.Length; i++)
		{
			if (text[i] != '{' || text[i + 2] != '}')
			{
				continue;
			}
			char c = text[i + 1];
			if (c >= '0' && c <= '9' && !list.Contains(c - '0'))
			{
				list.Add(c - '0');
			}
		}
		list.Sort();
		return list;
	}

	/// <summary>
	/// Installs a translation. Rejects unknown keys so a typo does not sit there silently.
	/// </summary>
	internal static bool SetOverride(string key, string text)
	{
		if (!Has(key))
		{
			return false;
		}
		if (string.IsNullOrEmpty(text))
		{
			Overrides.Remove(key);
			return true;
		}
		Overrides[key] = text;
		return true;
	}

	/// <summary>Drops every translation, returning to English. Called when the language changes.</summary>
	internal static void ClearOverrides()
	{
		Overrides.Clear();
	}

	/// <summary>How many keys the current language actually translated.</summary>
	public static int OverrideCount => Overrides.Count;

	/// <summary>
	/// True when every built-in string is non-empty and its placeholders are consistent with
	/// the ones a translation may use. Guards against a half-registered key, which would show
	/// up as an empty chat line rather than as a build error.
	/// </summary>
	internal static bool Verify(out string problem)
	{
		problem = null;
		if (Order.Count == 0)
		{
			problem = "the text table is empty";
			return false;
		}
		for (int i = 0; i < Order.Count; i++)
		{
			string key = Order[i];
			string text;
			if (!Defaults.TryGetValue(key, out text))
			{
				problem = key + " is listed but has no text";
				return false;
			}
			if (string.IsNullOrEmpty(text))
			{
				problem = key + " has empty text";
				return false;
			}
			if (text.Trim().Length == 0)
			{
				problem = key + " is only whitespace";
				return false;
			}
			// An unclosed '{' would make the placeholder reader pass it through, so a message
			// like "Usage: !kick {player}" would print literally. Cheap to catch here.
			int num = 0;
			for (int j = 0; j < text.Length; j++)
			{
				if (text[j] == '{')
				{
					num++;
				}
				else if (text[j] == '}')
				{
					num--;
				}
				if (num < 0)
				{
					problem = key + " has an unmatched '}'";
					return false;
				}
			}
			if (num != 0)
			{
				problem = key + " has an unmatched '{'";
				return false;
			}
		}
		return true;
	}
}
