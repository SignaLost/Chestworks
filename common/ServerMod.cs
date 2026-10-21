using Jotunn.Utils;
using UnityEngine;

namespace Chestworks.Common;

/// <summary>Whether the current server runs Chestworks; always true when hosting.</summary>
internal static class ServerMod
{
    private static ZNet _checkedWith;
    private static bool _present;
    private static int _checkedFrame = -1;

    /// <summary>Cached per connection; a false result is rechecked once per frame.</summary>
    internal static bool Present
    {
        get
        {
            if (_checkedWith != ZNet.instance)
            {
                _checkedWith = ZNet.instance;
                _present = false;
                _checkedFrame = -1;
            }

            if (!_present && ZNet.instance && _checkedFrame != Time.frameCount)
            {
                _present = ModCompatibility.IsModuleOnServer(ChestworksPlugin.PluginGuid);
                _checkedFrame = Time.frameCount;
            }

            return _present;
        }
    }
}
