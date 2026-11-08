# How do I install it?

## Installing

1. Install the BepInEx pack for Valheim and **Jotunn**.
2. Install Chestworks with a mod manager, or unzip it into a folder of its own inside
   the `BepInEx/plugins` folder.

Mods that need Chestworks, such as Valchitecture, list it as a dependency, so a mod
manager installs it for you. On its own, Chestworks changes nothing in the game.

## Settings

The settings file is `BepInEx/config/NullSignal.Chestworks.cfg`.

| Setting | Default | What it does |
|---|---|---|
| `Verbosity` | `Low` | How much it writes to the log: `Low`, `Medium` or `High` |
| `Keep In Chests` | `0` | How many of each item mods always leave in your chests |
| `Lock Button Style` | `Button` | `Button`, a square copy of Take All, or `Tab`, like the chest's weight tab. With Auga, always one of its own buttons, reading Lock or Unlock |
| `Button Anchor` | `TakeAll` | Button style: just past `TakeAll` or `PlaceStacks`, or inside the panel's `TopLeft`, `TopRight`, `BottomLeft` or `BottomRight` corner |
| `Button Offset` | `0, 0` | Button style: moves it from its anchor, in pixels, right and up |
| `Tab Anchor` | `RightTop` | Tab style: the panel edge it hangs from and where along it, such as `RightTop`, `LeftBottom` or `TopRight` |
| `Tab Offset` | `0, 0` | Tab style: moves it from its anchor, in pixels, right and up |

Each player sets these for themselves. Everything else is up to the mods that use it.

## Multiplayer

You can also install Chestworks on the server, dedicated or hosted. Then every player
needs it too, at the same version. Only the last number may differ, so 1.0.0 and
1.0.2 work together.

Players can still use Chestworks on a server that doesn't have it. The FAQ page on which
containers Chestworks uses explains the difference.
