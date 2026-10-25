# Chestworks

Chestworks lets Valheim mods use the items in your chests. It finds the chests near you,
takes what a mod needs, puts items back, and keeps it all safe when several players share
the same storage.

On its own it adds nothing to the game. You install it because a mod you use needs it,
such as **Valchitecture**, which lets you build straight from your chests. Every mod that
uses Chestworks follows the same rules, and a menu mod that supports it shows the right
item counts for all of them.

## What it does

**Finds the chests in reach.** Each mod decides how far its feature reaches, such as
building or crafting: a distance around you, the range of nearby crafting stations,
or a whole chain of stations whose ranges overlap. The chests closest to the work
are used first.

**Uses only storage players built.** Chests, carts, ships and storage from other mods all
count. It skips anything someone has open, a cart being pulled, a moving ship, someone
else's private chest, and chests inside a ward you can't open.

**Stays safe in multiplayer.** Only one player's game changes a chest at a time, so
nothing gets duplicated or lost when several players use the same storage. With
MultiUserChest and Chestworks on the server, a chest someone else has open stays usable.

**Works alongside similar mods.** If AzuCraftyBoxes is installed, or SeneaL UI's own
chest settings are on, Chestworks lets them do the job, so materials are never counted
or taken twice.

## For mod authors

`ChestworksAPI` lets your mod set its reach, read how many items are at hand, take
and store items, and check whether storage is in use. The API page of the FAQ has a
guide with examples.

## More

The FAQ explains which containers Chestworks uses, how it works with other mods and how
to install it. For mod authors, it also covers the API and building from source.

## Credits

- **MSchmoecker**: *MultiUserChest*, the inspiration for changing a container through the
  game that controls it.
- **Azumatt**: *AzuCraftyBoxes*, the inspiration for building and crafting from nearby
  containers.

## License

GNU General Public License v3.0; see [LICENSE.md](LICENSE.md).
