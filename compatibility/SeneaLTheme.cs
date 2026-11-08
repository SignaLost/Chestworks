using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Chestworks.Common;

namespace Chestworks.Compatibility;

/// <summary>Reads the muted color from SeneaL API's theme, so the "(Locked)" mark matches its look; stands down when <c>Themes.TryColor</c> is not found as expected.</summary>
internal static class SeneaLTheme
{
    private const string Muted = "Muted";

    private static bool _resolved;
    private static Type _colors;
    private static MethodInfo _tryColor;
    private static readonly object[] Args = new object[2];

    /// <summary>The theme's muted color as a "#RRGGBB" rich text value; the fallback without a theme.</summary>
    internal static string MutedHex(string fallback)
    {
        Resolve();

        if (_tryColor == null)
        {
            return fallback;
        }

        try
        {
            Args[0] = Enum.Parse(_colors, Muted);
            Args[1] = null;
            return (bool)_tryColor.Invoke(null, Args) ? "#" + ColorUtility.ToHtmlStringRGB((Color)Args[1]) : fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        Type themes = AccessTools.TypeByName("SeneaL.API.Themes");
        Type colors = AccessTools.TypeByName("SeneaL.API.ThemeColor");

        if (themes == null || colors == null)
        {
            return;
        }

        MethodInfo tryColor = AccessTools.Method(themes, "TryColor", new[] { colors, typeof(Color).MakeByRefType() });

        if (tryColor == null || tryColor.ReturnType != typeof(bool) || !colors.IsEnum || !Enum.IsDefined(colors, Muted))
        {
            Log.LogWarning("SeneaL API's theme has another shape than expected; the lock mark keeps its own gray.");
            return;
        }

        _colors = colors;
        _tryColor = tryColor;
    }
}
