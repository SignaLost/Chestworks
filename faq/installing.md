# How do I install it?

## Installing

1. Install the BepInEx pack for Valheim and **Jotunn**.
2. Install Chestworks with a mod manager, or unzip it into a folder of its own inside
   the `BepInEx/plugins` folder.

Mods that need Chestworks, such as Valchitecture, list it as a dependency, so a mod
manager installs it for you. On its own, Chestworks changes nothing in the game.

## Settings

The settings file is `BepInEx/config/NullSignal.Chestworks.cfg`. It has one setting:

| Setting | Default | What it does |
|---|---|---|
| `Verbosity` | `Low` | How much it writes to the log: `Low`, `Medium` or `High` |

Each player sets this one for themselves. Everything else is up to the mods that use it.

## Multiplayer

You can also install Chestworks on the server, dedicated or hosted. Then every player
needs it too, at the same version. Only the last number may differ, so 1.0.0 and
1.0.2 work together.

Players can still use Chestworks on a server that doesn't have it. The FAQ page on which
containers Chestworks uses explains the difference.
