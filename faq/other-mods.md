# How does it work with other mods?

## Mods built on it

**Valchitecture** owns the building purpose: building takes from your containers, and
removed pieces' materials go back into them. Its unlocks decide the reach.

## AzuCraftyBoxes

AzuCraftyBoxes takes from nearby chests itself, for building, crafting and fuel. While it
is installed, Chestworks leaves every purpose to it, so nothing is counted or taken twice.
Storing still works.

## MultiUserChest

MultiUserChest lets several players use a chest at once, with every change made by the
game controlling it. Chestworks changes containers the same way when the server runs it,
so with MultiUserChest a chest someone has open stays usable. Without it, or on a server
without Chestworks, an open chest is skipped. A chest counts as open while the player who
opened it first has it open; players viewing it alongside them are not marked.

## SeneaL UI

SeneaL UI draws its own build and crafting panels. Until it reads `ChestworksAPI`
itself, Chestworks adds the matching purpose's stock to those panels' counts and their
"from chests" marker. With SeneaL UI's own `[Chests] BuildFromChests` or `FromChests` on,
it takes from chests itself and Chestworks leaves that purpose to it.

## Backpack mods

Containers a player carries, such as those of backpack mods, belong to that player's
inventory and are never part of the network.
