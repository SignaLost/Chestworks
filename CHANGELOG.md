# Changelog

## 1.0.0

First release, split out of Valchitecture so other mods can share the same storage.

### Added

- **Purposes** with their own reach, set by the mod that owns each: a radius around the
  character, the range of stations covering the character or a point, and stations linked
  by overlapping ranges.
- **Container rules**: player-built containers only; skips containers open elsewhere,
  pulled carts, ships under sail or at the helm, moving carts and ships, private chests
  of others and warded containers.
- **Safe changes in multiplayer**: on a server running Chestworks, the game controlling a
  container applies every change; on a vanilla server the container is taken over first,
  as the game's take-all does.
- **Compatibility**: MultiUserChest's shared chests stay usable while open; AzuCraftyBoxes
  and SeneaL UI's own chest settings are left in charge; SeneaL UI's panels count the
  network until it reads the API itself.
- `ChestworksAPI` for other mods: reach registration, stock, revision, taking, storing,
  adding items, and storage-in-use checks.
