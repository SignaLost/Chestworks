# Changelog

## 1.0.0

First release, split out of Valchitecture so other mods can share the same storage.

- **Purposes** with their own reach, set by the mod that owns each: a radius around the
  character, the range of stations covering the character or the work, and stations
  linked to those by overlapping ranges.
- **Container rules**: player-built containers only; open containers, pulled or moving
  carts, ships under way, private chests of others and warded containers are skipped.
- **Safe changes in multiplayer**: on a server running Chestworks, the game controlling a
  container applies every change; on a vanilla server the container is taken over first,
  as the game's take-all does.
- **Compatibility**: MultiUserChest's open chests stay usable on a server running
  Chestworks; AzuCraftyBoxes and SeneaL UI's own chest settings are left in charge;
  SeneaL UI's panels count the network until SeneaL UI reads the API itself.
- **`ChestworksAPI`** for other mods: reach registration, deferral, stock and revision,
  the containers in reach, cheated-item checks, taking, storing, adding items, and
  storage-in-use checks.
