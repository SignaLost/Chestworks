# How do I use Chestworks from my mod?

Everything goes through `Chestworks.API`, a public static class in Chestworks.dll. It
works for the local player, must be called from the main thread, and returns nothing (or
does nothing) before a world loads. `API.Version` rises when members are added; existing
members keep their meaning.

## Purposes

A purpose names what containers are drawn on for. `Purposes.Building` (`"building"`) and
`Purposes.Crafting` (`"crafting"`) are built in; any other string works, such as
`"fuel"` for a mod that feeds smelters.

One mod owns each purpose and sets its reach. Any mod can read its stock or take from it.
Valchitecture owns `"building"`.

## Depending on Chestworks

Reference Chestworks.dll and declare the dependency, so Chestworks loads first:

```csharp
[BepInDependency("NullSignal.Chestworks")]
public class MyPlugin : BaseUnityPlugin { }
```

For an optional dependency, use `BepInDependency.DependencyFlags.SoftDependency` and
reach the API by reflection (below) only when `NullSignal.Chestworks` is in
`Chainloader.PluginInfos`.

On Thunderstore, list `NullSignal-Chestworks-1.0.0` in your manifest's dependencies.

## Owning a purpose

Register a function that describes the reach for the local player. Chestworks asks for it
at most once a frame while the purpose is in use, so keep it cheap; return null while
the purpose should reach nothing, such as before an unlock or with a setting off. The
containers themselves are gathered again four times a second, or sooner when the point
moves a meter.

```csharp
private static readonly ReachRules Rules = new();

private void Awake()
{
    API.RegisterReach(Purposes.Crafting, "My.Crafting.Mod", player =>
    {
        if (!MyConfig.Enabled.Value)
        {
            return null;
        }

        CraftingStation station = player.GetCurrentCraftingStation();

        Rules.Radius = 10f;            // around the character's eye
        Rules.Stations = true;         // the range of stations covering the character or the point
        Rules.Linked = false;          // plus stations linked by overlapping ranges
        Rules.Point = station ? station.transform.position : null;
        return Rules;
    });
}
```

`ReachRules` can be reused as above; Chestworks reads it at once and does not keep it.
Station ranges are each station's current build range, so mods that change station
ranges are respected. A second mod registering the same purpose is refused with a
warning; `UnregisterReach` gives it up.

## Counting for an interface

Add `Stock` to the inventory count you show. It counts the purpose's containers only:

```csharp
int have = player.GetInventory().CountItems("$item_wood") + API.Stock(Purposes.Building, "$item_wood");
```

Item names are the game's shared names. Counts are cached for the frame, so calling
`Stock` for every requirement row each frame is fine. To recompute only when something
changed, keep the last `API.Revision` and redraw when it differs; it also rises when the
player moves away from containers or a purpose turns off.

`IsActive(purpose)` says whether the purpose reaches anything right now, such as for
marking a count as including chests.

If your mod is an interface that Chestworks keeps a compatibility patch for, call this
once at startup to switch that patch off:

```csharp
API.ShowsStock("my.ui.mod.guid");
```

## Taking for a cost

The owner of a purpose usually takes its own costs. To take the shortfall after the
inventory, the way Valchitecture does for building:

```csharp
int needed = requirement.GetAmount(quality);
int shortfall = needed - player.GetInventory().CountItems(name);

if (shortfall > 0)
{
    API.Take(Purposes.Building, name, shortfall);
}
```

`Take` uses the nearest containers first and returns what it took. It never touches the
inventory. A count can be a moment old in multiplayer, so `Take` may return less than
asked if another player emptied a chest just before.

## Storing

`Store` puts items into the purpose's containers that already hold that item, nearest the
given point first, and returns how many went in. Anything left is yours to place:

```csharp
int stored = API.Store(Purposes.Building, piece.transform.position, woodPrefab, 10, exclude: piece);
```

`exclude` keeps items out of a container that is about to disappear, such as a chest
being removed. `AddItems(inventory, prefab, amount)` adds to any inventory in proper
stacks within its free space; it does not check carry weight.

## Mods that take from chests themselves

If your mod pulls from chests on its own for a purpose, tell Chestworks while it does, so
nothing is counted or taken twice:

```csharp
API.DefersTo("my.mod.guid", Purposes.Crafting, () => MyConfig.FromChests.Value);
```

Pass null as the purpose to defer every purpose. Storing is not held back.

## Protection checks

| Member | Gives |
|---|---|
| `IsOpenElsewhere(container)` | Another player's game has the container open |
| `IsUsable(container)` | The container passes Chestworks' rules for the local player, reach aside |
| `IsStorageInUse(piece)` | A container in the piece is open elsewhere, or its cart is pulled by someone else |

## All members

| Member | Gives |
|---|---|
| `Version`, `Guid` | The API version, and `NullSignal.Chestworks` |
| `RegisterReach(purpose, owner, rules)`, `UnregisterReach(purpose, owner)` | Owning a purpose |
| `DefersTo(guid, purpose, active)` | Leaving a purpose to a mod that pulls from chests itself |
| `ShowsStock(guid)` | Switching off a compatibility patch for an interface mod |
| `IsActive(purpose)` | The purpose reaches containers right now |
| `Revision` | Rises whenever stock may have changed |
| `Stock(purpose, item, quality)` | Containers' stock of an item, without the inventory |
| `GetContainers(purpose, list)` | The purpose's containers, nearest first |
| `HasCheated(purpose, resources)` | Cheated items among those containers, as the game checks the inventory |
| `Take(purpose, item, amount, quality)` | Takes, nearest first; returns what was taken |
| `Store(purpose, point, prefab, amount, cheated, exclude)` | Stores where the item already is; returns what was stored |
| `AddItems(inventory, prefab, amount, cheated)` | Adds in proper stacks within free space |
| `IsOpenElsewhere`, `IsUsable`, `IsStorageInUse` | Protection checks |

## Without a reference

Every member but `RegisterReach` uses only game and system types, so reflection works:

```csharp
if (Chainloader.PluginInfos.TryGetValue("NullSignal.Chestworks", out PluginInfo info))
{
    Type api = info.Instance.GetType().Assembly.GetType("Chestworks.API");
    MethodInfo stock = api?.GetMethod("Stock");
    int have = stock != null ? (int)stock.Invoke(null, new object[] { "building", "$item_wood", -1 }) : 0;
}
```
