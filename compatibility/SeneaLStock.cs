using System;
using System.Reflection;
using Chestworks.Core;
using Chestworks.Common;
using HarmonyLib;

namespace Chestworks.Compatibility;
/// <summary>
///     Bridges Chestworks to SeneaL UI's build and crafting panels. It finds SeneaL UI's <c>Stock</c> class by
///     reflection, defers building and crafting to SeneaL UI while its own chest settings are on, and otherwise
///     names the purpose whose stock the panel patches add to the counts. It stands down once SeneaL UI calls
///     <see cref="ChestworksAPI.ShowsStock" /> or when the <c>Stock</c> class does not have the expected shape.
/// </summary>
internal static class SeneaLStock
{
    private const string Guid = "seneaL.valheim.ui";
    private const string StockType = "SeneaLUI.Core.Stock";

    private static bool _resolved;
    private static bool _registered;
    private static Type _type;
    private static Func<bool> _buildOn;
    private static Func<bool> _craftOn;
    private static Func<bool> _otherChests;
    private static FieldInfo _build;

    internal static Type Type
    {
        get
        {
            Resolve();
            return _type;
        }
    }

    /// <summary>Leaves building and crafting to SeneaL UI while its own chest settings for them are on; runs once.</summary>
    internal static void Register()
    {
        if (_registered || Type == null)
        {
            return;
        }

        _registered = true;
        StorageNetwork.Defer(Guid, Purposes.Building, () => _buildOn());
        StorageNetwork.Defer(Guid, Purposes.Crafting, () => _craftOn());
    }

    /// <summary>The purpose SeneaL UI is counting for now, when Chestworks should add to it; otherwise null.</summary>
    internal static string Counting()
    {
        Resolve();

        if (_build == null || StorageNetwork.ShowsStock.Contains(Guid) || (_otherChests != null && _otherChests()))
        {
            return null;
        }

        string purpose = (bool)_build.GetValue(null) ? Purposes.Building : Purposes.Crafting;
        return StorageNetwork.IsActive(purpose) ? purpose : null;
    }

    private static void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        _type = AccessTools.TypeByName(StockType);

        if (_type == null)
        {
            return;
        }

        MethodInfo buildOn = AccessTools.PropertyGetter(_type, "BuildOn");
        MethodInfo craftOn = AccessTools.PropertyGetter(_type, "CraftOn");
        _build = AccessTools.Field(_type, "Build");

        if (buildOn == null || craftOn == null || _build == null || _build.FieldType != typeof(bool))
        {
            Log.LogWarning("SeneaL UI's stock counts have another shape than expected; its panels count without Chestworks.");
            _type = null;
            _build = null;
            return;
        }

        _buildOn = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), buildOn);
        _craftOn = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), craftOn);

        // PovWins is missing on versions without Path of Valheiman support.
        MethodInfo povWins = AccessTools.PropertyGetter(_type, "PovWins");
        _otherChests = povWins != null && povWins.ReturnType == typeof(bool)
            ? (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), povWins)
            : null;
    }
}
