using System.Collections;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace RemoveStartTemple
{
    [BepInPlugin("es.uyimbube.removestarttemple", "RemoveStartTemple", "0.8.4")]
    public sealed class RemoveStartTemplePlugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        private void Awake()
        {
            _harmony = new Harmony("es.uyimbube.removestarttemple");
            _harmony.PatchAll(typeof(RemoveStartTemplePlugin).Assembly);
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }

    [HarmonyPatch(typeof(ZoneSystem), "SetupLocations")]
    internal static class RemoveStartTempleFromWorldGeneration
    {
        private const string StartLocationName = "StartTemple";

        private static void Postfix(ZoneSystem __instance)
        {
            for (var i = __instance.m_locations.Count - 1; i >= 0; i--)
            {
                var location = __instance.m_locations[i];
                if (location == null || GetPrefabName(location) != StartLocationName)
                    continue;

                __instance.m_locations.RemoveAt(i);
                RemoveFromHashIndex(__instance, location);
            }
        }

        private static void RemoveFromHashIndex(ZoneSystem zoneSystem, ZoneSystem.ZoneLocation location)
        {
            var index = AccessTools.Field(typeof(ZoneSystem), "m_locationsByHash")?.GetValue(zoneSystem) as IDictionary;
            var hash = location.Hash;

            if (index != null && index.Contains(hash) && ReferenceEquals(index[hash], location))
                index.Remove(hash);
        }

        private static string GetPrefabName(ZoneSystem.ZoneLocation location)
        {
            var prefab = AccessTools.Field(typeof(ZoneSystem.ZoneLocation), "m_prefab")?.GetValue(location);
            if (prefab == null)
                return null;

            var type = prefab.GetType();
            if (AccessTools.Property(type, "IsValid")?.GetValue(prefab) is bool isValid && !isValid)
                return null;

            return AccessTools.Property(type, "Name")?.GetValue(prefab) as string;
        }
    }

    [HarmonyPatch(typeof(ZoneSystem), "GetLocationIcon")]
    internal static class RedirectStartTempleLookupToWorldOrigin
    {
        private static bool Prefix(string name, ref Vector3 pos, ref bool __result)
        {
            if (name != "StartTemple")
                return true;

            pos = Vector3.zero;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Game), "FindSpawnPoint")]
    internal static class WaitForSpawnAreaToLoad
    {
        private static void Postfix(ref bool __result)
        {
            if (__result && (!ZoneSystem.instance.LocationsGenerated || !ZoneSystem.instance.IsActiveAreaLoaded()))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.Interact))]
    internal static class BossTrophyPower
    {
        private static readonly Dictionary<string, string> Powers = new Dictionary<string, string>
        {
            ["TrophyEikthyr"] = "GP_Eikthyr",
            ["TrophyTheElder"] = "GP_TheElder",
            ["TrophyBonemass"] = "GP_Bonemass",
            ["TrophyDragonQueen"] = "GP_Moder",
            ["TrophyGoblinKing"] = "GP_Yagluth",
            ["TrophySeekerQueen"] = "GP_Queen",
            ["TrophyFader"] = "GP_Ashlands"
        };

        private struct Effects
        {
            public EffectList World;
            public EffectList Player;
            public bool Changed;
        }

        internal static string GetPowerName(ItemStand stand)
        {
            if (ObjectDB.instance == null || stand == null)
                return null;

            var item = ObjectDB.instance.GetItemPrefab(stand.GetAttachedItem());
            return item != null && Powers.TryGetValue(item.name, out var powerName) ? powerName : null;
        }

        private static void Prefix(ItemStand __instance, out Effects __state)
        {
            __state = default;
            var powerName = GetPowerName(__instance);
            if (powerName != null)
            {
                var power = ObjectDB.instance.m_StatusEffects.Find(effect => effect.name == powerName);
                if (power == null)
                    return;

                __instance.m_guardianPower = power;
                var source = FindPowerStand(__instance, powerName);
                if (source != null)
                {
                    __state = new Effects { World = __instance.m_activatePowerEffects, Player = __instance.m_activatePowerEffectsPlayer, Changed = true };
                    __instance.m_activatePowerEffects = source.m_activatePowerEffects;
                    __instance.m_activatePowerEffectsPlayer = source.m_activatePowerEffectsPlayer;
                }
            }
            else if (__instance.m_guardianPower != null && Powers.ContainsValue(__instance.m_guardianPower.name))
            {
                __instance.m_guardianPower = null;
            }
        }

        private static ItemStand FindPowerStand(ItemStand target, string powerName)
        {
            var source = ZNetScene.instance.GetPrefab(powerName)?.GetComponent<ItemStand>();
            if (HasPowerEffects(source, target, powerName))
                return source;

            foreach (var candidate in Resources.FindObjectsOfTypeAll<ItemStand>())
                if (HasPowerEffects(candidate, target, powerName))
                    return candidate;

            return null;
        }

        private static bool HasPowerEffects(ItemStand stand, ItemStand target, string powerName)
        {
            return stand != null && stand != target && stand.m_guardianPower != null &&
                   stand.m_guardianPower.name == powerName &&
                   (stand.m_activatePowerEffects.HasEffects() || stand.m_activatePowerEffectsPlayer.HasEffects());
        }

        private static void Postfix(ItemStand __instance, Effects __state)
        {
            if (!__state.Changed)
                return;

            __instance.m_activatePowerEffects = __state.World;
            __instance.m_activatePowerEffectsPlayer = __state.Player;
        }
    }

    [HarmonyPatch(typeof(ItemStand), nameof(ItemStand.GetHoverText))]
    internal static class BossTrophyHoverText
    {
        private static void Postfix(ItemStand __instance, ref string __result)
        {
            if (!__instance.m_canBeRemoved || string.IsNullOrEmpty(__result) ||
                !PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false))
                return;

            var powerName = BossTrophyPower.GetPowerName(__instance);
            if (powerName == null)
                return;

            var power = ObjectDB.instance.m_StatusEffects.Find(effect => effect.name == powerName);
            if (power == null)
                return;

            var action = Player.m_localPlayer.GetGuardianPowerName() == power.name
                ? "$guardianstone_hook_alreadyactive"
                : "[<color=yellow><b>$KEY_Use</b></color>] $guardianstone_hook_activate";

            __result += "\n" + Localization.instance.Localize(
                $"<color=orange>{power.m_name}</color>\n{power.GetTooltipString()}\n\n{action}");
        }
    }
}
