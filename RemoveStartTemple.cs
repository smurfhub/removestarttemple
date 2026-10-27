using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RemoveStartTemple
{
    [BepInPlugin("es.uyimbube.removestarttemple", "RemoveStartTemple", "0.7.0")]
    public sealed class RemoveStartTemplePlugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        private void Awake()
        {
            Logger.LogInfo("[RemoveStartTemple] Initializing patch.");
            _harmony = new Harmony("es.uyimbube.removestarttemple");
            _harmony.PatchAll(typeof(RemoveStartTemplePlugin).Assembly);
            Logger.LogInfo("[RemoveStartTemple] Patches installed.");
        }

        private void OnDestroy()
        {
            Logger.LogInfo("[RemoveStartTemple] Removing patches.");
            _harmony?.UnpatchSelf();
            Logger.LogInfo("[RemoveStartTemple] Patches removed.");
        }
    }

    [HarmonyPatch]
    internal static class RemoveStartTempleFromWorldGeneration
    {
        private const string StartLocationName = "StartTemple";
        private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("RemoveStartTemple");

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ZoneSystem), "SetupLocations");
        }

        private static void Postfix(ZoneSystem __instance)
        {
            Log.LogInfo("[WorldGen] SetupLocations completed; checking registered locations.");
            var removed = 0;

            for (var i = __instance.m_locations.Count - 1; i >= 0; i--)
            {
                var location = __instance.m_locations[i];
                if (location == null || GetPrefabName(location) != StartLocationName)
                    continue;

                __instance.m_locations.RemoveAt(i);
                RemoveFromHashIndex(__instance, location);
                removed++;
                Log.LogInfo($"[WorldGen] Removed {StartLocationName} from location list and hash index (hash {location.Hash}).");
            }

            if (removed == 0)
                Log.LogWarning($"[WorldGen] {StartLocationName} was not found in the registered location list.");
            else
                Log.LogInfo($"[WorldGen] Removal complete; removed {removed} {StartLocationName} location(s).");
        }

        private static void RemoveFromHashIndex(ZoneSystem zoneSystem, ZoneSystem.ZoneLocation location)
        {
            var field = AccessTools.Field(typeof(ZoneSystem), "m_locationsByHash");
            var index = field?.GetValue(zoneSystem) as IDictionary;
            if (index == null)
            {
                Log.LogWarning("[WorldGen] Could not access ZoneSystem.m_locationsByHash.");
                return;
            }

            var hash = location.Hash;
            if (index.Contains(hash) && ReferenceEquals(index[hash], location))
            {
                index.Remove(hash);
                Log.LogInfo($"[WorldGen] Removed hash index entry {hash}.");
            }
            else
            {
                Log.LogWarning($"[WorldGen] Hash index entry {hash} was missing or belonged to another location.");
            }
        }

        private static string GetPrefabName(ZoneSystem.ZoneLocation location)
        {
            var prefabField = AccessTools.Field(typeof(ZoneSystem.ZoneLocation), "m_prefab");
            var prefab = prefabField?.GetValue(location);
            if (prefab == null)
                return null;

            var isValid = AccessTools.Property(prefab.GetType(), "IsValid");
            if (isValid != null && !(bool)isValid.GetValue(prefab, null))
                return null;

            var name = AccessTools.Property(prefab.GetType(), "Name");
            return name?.GetValue(prefab, null) as string;
        }
    }

    [HarmonyPatch]
    internal static class RedirectStartTempleLookupToWorldOrigin
    {
        private const string StartLocationName = "StartTemple";
        private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("RemoveStartTemple");
        private static bool _loggedRedirect;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(ZoneSystem), "GetLocationIcon");
        }

        private static bool Prefix(string name, ref Vector3 pos, ref bool __result)
        {
            if (name != StartLocationName)
                return true;

            pos = Vector3.zero;
            __result = true;
            if (!_loggedRedirect)
            {
                Log.LogInfo("[Spawn] Redirected vanilla GetLocationIcon(\"StartTemple\") lookup to world origin (0, 0, 0).");
                _loggedRedirect = true;
            }

            return false;
        }
    }
}
