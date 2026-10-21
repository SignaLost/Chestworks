# Chestworks

A shared storage network for Valheim mods. Chestworks decides which containers a mod can
draw on, takes from them and stores into them safely in multiplayer, and gives every mod
and interface one place to ask how much is at hand.

On its own it adds nothing to the game. Mods that use it, such as **Valchitecture**
building from your chests, get the same rules and the same safety, and an interface mod
that supports Chestworks shows the right counts for all of them.

## What it handles

**Reach.** Each use of the network, such as building or crafting, is a purpose with its
own reach, set by the mod that owns it: a distance around you, the range of the stations
you or the work stand in, and optionally every station linked to those by overlapping
ranges. Containers nearest the work are used first.

**Which containers count.** Containers players built: chests, carts, ships and those added
by other mods. Never one someone else has open, a cart being pulled, a ship under sail or
at the helm, a private chest that is not yours, or one inside a ward you cannot open.

**Multiplayer.** A container is only ever changed by the game that controls it, so nothing
is duplicated or lost when several players use the same storage. MultiUserChest's shared
chests work too.

**Other mods.** AzuCraftyBoxes and SeneaL UI's own chest crafting are left in charge when
they are on, so materials are never counted or taken twice.

## For mod authors

`Chestworks.API` registers a purpose's reach, reads stock, takes and stores, and checks
whether storage is in use. The FAQ has a guide with examples.

## More

The FAQ covers how the network decides, multiplayer, other mods, the API, installing and
building from source.

## License

GNU General Public License v3.0; see [LICENSE.md](LICENSE.md).
