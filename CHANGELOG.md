# Changelog

## 1.1.0

- A padlock button in the chest panel. Mods leave a locked chest alone, and everyone on
  the server sees the lock. With SeneaL UI, its own exclusion button does the same; with
  Auga, it is one of Auga's buttons reading Lock or Unlock.
- `Keep In Chests` setting: how many of each item mods always leave in your chests.
- Mods can now choose which chests in reach they use.
- When two players take the same items from a chest at the same moment, the second one
  now pays for them from their inventory or another chest instead of getting them free.
- Counting items across many chests is cheaper.
- Changes made to the settings file while the game runs now apply right away.

## 1.0.0

First release. Chestworks was split out of Valchitecture so other mods can share
the same storage.

- Mods can use the chests within a set distance of you, within the range of nearby
  crafting stations, or along a chain of stations whose ranges overlap.
- Only storage players built is used. Open chests, carts being pulled, moving ships,
  other players' private chests and chests inside wards you can't open are skipped.
- Nothing gets duplicated or lost when several players use the same chests.
- Also works on servers that don't have Chestworks installed.
- With MultiUserChest, a chest someone else has open stays usable, as long as the server
  has Chestworks too.
- AzuCraftyBoxes and SeneaL UI's own chest settings are left in charge, so nothing is
  counted or taken twice.
- SeneaL UI's build and crafting panels count the items in your chests.
- `ChestworksAPI` lets other mods use the chests in reach.
