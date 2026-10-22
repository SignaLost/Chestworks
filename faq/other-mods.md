# How does it work with other mods?

## Mods built on it

**Valchitecture** owns the building purpose: building takes from your containers, and
the materials of removed pieces go back into them. Its unlocks decide the reach.

## AzuCraftyBoxes

AzuCraftyBoxes takes from nearby chests itself. While it is installed, Chestworks leaves
every purpose to it, so nothing is counted or taken twice. Storing still works.

## MultiUserChest

MultiUserChest lets several players use a chest at once, with every change made by the
game controlling it. Chestworks changes containers the same way when the server runs it,
so with both installed a chest someone else has open stays usable. Without
MultiUserChest, or on a server without Chestworks, an open chest is skipped.

## SeneaL UI

SeneaL UI draws its own build and crafting panels. Until it reads `ChestworksAPI`
itself, Chestworks adds the matching purpose's stock to those panels' counts and their
"from chests" marker. While SeneaL UI's own build-from-chests or craft-from-chests
setting is on, it takes from chests itself and Chestworks leaves that purpose to it.

## Backpack mods

Containers a player carries, such as those of backpack mods, belong to that player's
inventory and are never part of the network.
