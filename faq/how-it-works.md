# Which containers does the network use?

## Which containers count

Any container a player built: chests, carts, ships and containers added by other mods.
Containers that came with the world, such as dungeon chests, do not count, and neither do
the ones a player carries. A container is skipped while:

- someone else has it open, unless MultiUserChest is installed and the server runs
  Chestworks. A container left marked open by a player who dropped out counts as closed
  once your game controls it, as the game itself treats it;
- it is a cart being pulled, a ship with someone at the helm or its sail set, or a cart or
  ship moving faster than 0.5 m/s;
- it is private and not yours, or inside a ward you have no access to, as when opening it.

## How far it reaches

Each purpose, such as building, has a reach set by the mod that owns it. A reach is made
of up to three parts, and covers everything any of them covers:

- a **distance** around your character;
- the build range of every **station** covering you or the place the work happens, such
  as the piece being placed;
- every station **linked** to those, where two stations link when their ranges overlap.
  Any kind of station links: a workbench, forge and stonecutter whose ranges overlap form
  one network, and a chain of them reaches as far as it goes.

Ranges are each station's current build range, so mods and settings that change station
ranges carry through. The containers nearest the work are used first.

## How current the counts are

The set of containers is gathered again four times a second, or sooner when the work moves
a meter. Counts are read fresh each frame, and pick up another player's changes as soon as
they arrive rather than waiting for the game's own once-a-second check.

## Multiplayer

A container's contents are saved by whoever's game currently controls it, and only that
game may change them. When the server runs Chestworks, every player has it, so your game
asks the controlling game to take or add the items, the way MultiUserChest handles its
shared chests. Your own copy changes at once, so the next count is right, and the
controlling game's next save replaces it. If a chest was emptied in the moment between,
that game takes what is there; if one filled up before stored items arrived, the rest
drops next to it.

A request that reaches a game which no longer controls the container is passed on to the
one that does. One that comes back to the game that sent it is dropped, as that game has
already applied it to its own copy.

On a server without Chestworks, other players may not have it, so there is no one to ask.
Your game takes control of the container instead and tells the previous owner at once, as
the game's own "take all" does once it has been allowed. An open container is never taken
over this way, even with MultiUserChest, as the player using it would be left looking at
old contents.
