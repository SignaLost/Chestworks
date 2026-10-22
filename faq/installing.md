# How do I install it?

## Installing

1. Install the BepInEx pack for Valheim and **Jotunn**.
2. Install Chestworks through a mod manager, or unzip the package into a folder of its
   own in `BepInEx/plugins`. Mods that need it, such as Valchitecture, list it as a
   dependency, so a mod manager installs it for you.

On its own Chestworks changes nothing in the game; the mods that use it decide what it
is used for.

## Configuration

`BepInEx/config/NullSignal.Chestworks.cfg` has one setting:

| Setting | Default | Values | Effect |
|---|---|---|---|
| `Verbosity` | `Low` | `Low`, `Medium`, `High` | How much Chestworks writes to the log; not synced with the server |

Everything else is up to the mods that use it.

## Multiplayer

Chestworks can also be installed on a dedicated server. A server running it, dedicated
or hosted, requires it of every client at the same major and minor version, and changes
to shared containers are then made by the game controlling them. On a vanilla server,
clients may still run it; a container is then taken over before it is changed. See the
FAQ entry on which containers the network uses.
