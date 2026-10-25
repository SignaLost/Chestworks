# Which containers does Chestworks use?

## Which containers count

Any storage a player built: chests, carts, ships and storage added by other mods.
Containers that came with the world, such as dungeon chests, don't count, and neither
does anything a player carries.

A container is skipped while:

- someone has it open, you or anyone else. With MultiUserChest, and Chestworks on the
  server, only a chest you have open yourself is skipped;
- it's a cart being pulled or moving, or a ship that's sailing or has someone at the helm;
- it's someone else's private chest, or inside a ward you can't open.

## How far it reaches

Each mod decides how far its feature reaches, such as building or crafting. The reach can
include any of these:

- a **distance** around you;
- the range of every **crafting station** that covers you or the spot you're working on,
  such as where you're placing a piece;
- every station **linked** to those. Two stations link when their ranges overlap, whatever
  kind they are: a workbench, forge and stonecutter side by side form one group, and a
  chain of them reaches as far as it goes.

Mods and settings that change station ranges carry over. The containers closest to the
work are used first.

## Are the counts up to date?

Yes. Item counts keep up as you move around and as other players take from or add to
the same chests.

## Multiplayer

Only one player's game changes a chest at a time, so nothing gets duplicated or lost when
several players use the same storage. This works best with Chestworks installed on the
server, since every player then has it too.

On a server without Chestworks, it still works: it handles each chest the same way
the game's own "Take all" does. A chest someone has open is always skipped there,
even with MultiUserChest.
