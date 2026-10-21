# How do I install it?

## Installing

1. Install the BepInEx pack for Valheim and **Jotunn**.
2. Install it through a mod manager, or unzip the package into a folder of its own in
   `BepInEx/plugins`. Mods that need it, such as Valchitecture, install it for you.

On its own Chestworks changes nothing in the game; the mods that use it decide what it is
used for.

## Configuration

`BepInEx/config/NullSignal.Chestworks.cfg` has one setting, `Verbosity`, for how much it
writes to the log. Everything else is up to the mods that use it.

## Multiplayer

A server running Chestworks, dedicated or hosted, requires it of every client, at the
same minor version, and changes to shared containers are then made by the game
controlling them. On a vanilla
server it runs client-side and takes containers over before changing them; see the FAQ
entry on which containers the network uses.
