# How do I use Chestworks from my mod?

Everything goes through `ChestworksAPI`, a public static class in `Chestworks.dll`. It
works for the local player, must be called from the main thread, and returns nothing (or
does nothing) before a world loads. `ChestworksAPI.Version` rises when members are
added; existing members keep their meaning.

## Purposes

A purpose names what containers are drawn on for. `Purposes.Building` (`"building"`) and
`Purposes.Crafting` (`"crafting"`) are built in; any other string works, such as
`"fuel"` for a mod that feeds smelters.

One mod owns each purpose and sets its reach. Any mod can read its stock or take from
it. Valchitecture owns `"building"`.

## Depending on Chestworks

Reference `Chestworks.dll` and declare the dependency, so Chestworks loads first:

```csharp
[BepInDependency(ChestworksAPI.Guid)]
public class MyPlugin : BaseUnityPlugin { }
```

For an optional dependency, use `BepInDependency.DependencyFlags.SoftDependency` with
the GUID `"NullSignal.Chestworks"` and reach the API by reflection (below) only when
that GUID is in `Chainloader.PluginInfos`.

On Thunderstore, list `NullSignal-Chestworks-1.0.0` in your manifest's dependencies.

## Owning a purpose

Register a function that describes the reach for the local player. Chestworks asks for
it at most once a frame while the purpose is in use, so keep it cheap; return null while
the purpose should reach nothing, such as before an unlock or with a setting off. The
containers themselves are gathered again four times a second, or sooner when the
character or the point moves four meters.

```csharp
private static readonly ReachRules Rules = new();

private void Awake()
{
    ChestworksAPI.RegisterReach(Purposes.Crafting, "My.Crafting.Mod", player =>
    {
        if (!MyConfig.Enabled.Value)
        {
            return null;
        }

        CraftingStation station = player.GetCurrentCraftingStation();

        Rules.Radius = 10f;     // around the character's eye; 0 for none
        Rules.Stations = true;  // stations covering the character or the point
        Rules.Linked = false;   // plus stations linked to those by overlapping ranges
        Rules.Point = station ? station.transform.position : null;  // null: the character
        return Rules;
    });
}
```

`ReachRules` can be reused as above; Chestworks reads it at once and does not keep it.
Station ranges are each station's current build range, so mods that change station
ranges are respected. A second mod registering the same purpose is refused with a
warning; `UnregisterReach` gives a purpose up.

## Counting for an interface

Add `Stock` to the inventory count you show. It counts the purpose's containers only:

```csharp
int have = player.GetInventory().CountItems("$item_wood")
    + ChestworksAPI.Stock(Purposes.Building, "$item_wood");
```

Item names are the game's shared names. Counts are cached for the frame, so calling
`Stock` for every requirement row each frame is fine. To redraw only when something
changed, keep the last `ChestworksAPI.Revision` and compare; it also rises when the
player moves away from containers or a purpose turns off.

`IsActive(purpose)` says whether the purpose reaches anything right now, such as for
marking a count as including chests.

If your mod is an interface that Chestworks keeps a compatibility patch for, call this
once at startup to switch that patch off:

```csharp
ChestworksAPI.ShowsStock("my.ui.mod.guid");
```

## Taking for a cost

The owner of a purpose usually takes its own costs. To take the shortfall after the
inventory, the way Valchitecture does for building:

```csharp
int needed = requirement.GetAmount(quality);
int shortfall = needed - player.GetInventory().CountItems(name, quality);

if (shortfall > 0)
{
    ChestworksAPI.Take(Purposes.Building, name, shortfall, quality);
}
```

`Take` uses the nearest containers first and returns what it took from your game's copy
of each container. It never touches the inventory. On a server running Chestworks the
game controlling a container applies the same removal, and takes what is there if
another player emptied the chest in between.

## Storing

`Store` puts items into the purpose's containers that already hold that item, nearest
the given point first, and returns how many went in. Anything left is yours to place:

```csharp
int stored = ChestworksAPI.Store(Purposes.Building, piece.transform.position, woodPrefab, 10, exclude: piece);
```

`exclude` keeps items out of a container that is about to disappear, such as a chest
being removed. `AddItems(inventory, prefab, amount)` adds to any inventory in proper
stacks within its free space; it does not check carry weight.

## Mods that take from chests themselves

If your mod pulls from chests on its own for a purpose, tell Chestworks while it does,
so nothing is counted or taken twice:

```csharp
ChestworksAPI.DefersTo("my.mod.guid", Purposes.Crafting, () => MyConfig.FromChests.Value);
```

Pass null as the purpose to defer every purpose. While a purpose is deferred, it counts
and takes nothing and `IsActive` is false; `Store` still works, so returned materials
reach the chests whichever mod takes from them.

## All members

Optional parameters are shown with their defaults.

| Member | Gives |
|---|---|
| `Version`, `Guid` | The API version (`1`) and `"NullSignal.Chestworks"` |
| `RegisterReach(purpose, ownerGuid, rules)` | Owns a purpose; `rules` is a `Func<Player, ReachRules>` |
| `UnregisterReach(purpose, ownerGuid)` | Gives the purpose up |
| `DefersTo(modGuid, purpose, active)` | Leaves the purpose (every purpose for null) to a mod while `active()` is true |
| `ShowsStock(modGuid)` | Switches off the compatibility patch for an interface mod |
| `IsActive(purpose)` | The purpose reaches containers right now and is not deferred |
| `Revision` | Rises whenever stock may have changed |
| `Stock(purpose, item, quality = -1)` | The containers' stock of an item, without the inventory |
| `GetContainers(purpose, into)` | Adds the purpose's containers, nearest first, to a `List<Container>` |
| `HasCheated(purpose, resources, quality = -1, matchWorldLevel = true)` | Cheated items among those containers, as the game checks the inventory |
| `Take(purpose, item, amount, quality = -1, worldLevelBased = true)` | Takes, nearest first; returns what was taken |
| `Store(purpose, point, prefab, amount, cheated = false, exclude = null)` | Stores where the item already is; returns what was stored |
| `AddItems(inventory, prefab, amount, cheated = false)` | Adds in proper stacks within free space; returns what was added |
| `IsOpenElsewhere(container)` | Another player's game has the container open |
| `IsUsable(container)` | The container passes the rules for the local player, reach aside |
| `IsStorageInUse(piece)` | A container in the piece is open elsewhere, or its cart is pulled by someone else |

## Without a reference

Every member but `RegisterReach` uses only game and system types, so reflection works:

```csharp
if (Chainloader.PluginInfos.TryGetValue("NullSignal.Chestworks", out PluginInfo info))
{
    Type api = info.Instance.GetType().Assembly.GetType("Chestworks.ChestworksAPI");
    MethodInfo stock = api?.GetMethod("Stock");
    int have = stock != null ? (int)stock.Invoke(null, new object[] { "building", "$item_wood", -1 }) : 0;
}
```
