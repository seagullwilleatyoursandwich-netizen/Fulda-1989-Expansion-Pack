using GHPC;
using GHPC.AI;
using GHPC.Mission;
using GHPC.Mission.Data;
using GHPC.Vehicle;
using HarmonyLib;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fulda1989
{
    public static class LEO2A4StandaloneRegistration
    {
        private const string SourceUniqueName = "LEO2A3";
        private const string T90UniqueName = "LEO2A4";

        private static bool _registered;

        public static void TryRegister()
        {
            if (_registered)
                return;

            MelonCoroutines.Start(RegisterRoutine());
        }

        public static void ResetSceneAttempt()
        {
        }

        public static void Dispose()
        {
            _registered = false;
        }

        private static IEnumerator RegisterRoutine()
        {
            while (!_registered)
            {
                UnitPrefabLookupScriptable lookup = Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>().FirstOrDefault();

                if (lookup == null || lookup.AllUnits == null)
                {
                    //MelonLogger.Msg("[LEO2K CUSTOMIZER] Native unit lookup is not ready yet.");
                    yield return null;
                    continue;
                }

                UnitPrefabLookupScriptable.UnitPrefabMetadata sourceMetadata = lookup.AllUnits.FirstOrDefault(metadata => metadata != null && string.Equals(metadata.Name, SourceUniqueName, StringComparison.OrdinalIgnoreCase));

                if (sourceMetadata == null)
                {
                    //MelonLogger.Msg("[LEO2K CUSTOMIZER] Could not find native LEO1A4 metadata yet.");
                    yield return null;
                    continue;
                }

                if (sourceMetadata.PrefabReference == null)
                {
                    MelonLogger.Error("[LEO2A4 CUSTOMIZER] Native LEO2A3 has no prefab AssetReference.");
                    yield break;
                }

                if (lookup.AllUnits.Any(metadata => metadata != null && string.Equals(metadata.Name, T90UniqueName, StringComparison.OrdinalIgnoreCase)))
                {
                    _registered = true;
                    //MelonLogger.Msg("[LEO2A4 CUSTOMIZER] Leopard 2A4 metadata is already registered.");
                    yield break;
                }

                List<UnitPrefabLookupScriptable.UnitPrefabMetadata> allUnits = lookup.AllUnits.ToList();

                allUnits.Add(new UnitPrefabLookupScriptable.UnitPrefabMetadata
                {
                    AllowInCustomizer = true,
                    Class = sourceMetadata.Class,
                    Army = sourceMetadata.Army,
                    DecalLayout = sourceMetadata.DecalLayout,
                    PrefabReference = sourceMetadata.PrefabReference,
                    BaseAmmoClipsReference = new UnityEngine.AddressableAssets.AssetReference(),
                    UseDecalLayout = sourceMetadata.UseDecalLayout,
                    AlternativeClasses = sourceMetadata.AlternativeClasses,
                    Name = T90UniqueName,
                    FriendlyName = "Leopard 2A4 (1988)"
                });

                lookup.AllUnits = allUnits.ToArray();

                _registered = true;

                //MelonLogger.Msg("[LEO2A4 CUSTOMIZER] Registered Leopard 2A4 directly in UnitPrefabLookup using native LEO2A3 prefab.");
            }
        }
    }

    [HarmonyPatch(typeof(UnitSpawner), "SpawnUnit", new Type[]
    {
        typeof(string),
        typeof(UnitMetaData),
        typeof(WaypointHolder),
        typeof(Transform)
    })]
    public static class LEO2A4CustomizerSpawnPatch
    {
        public static void Postfix(string uniqueName, IUnit __result)
        {
            if (!string.Equals(uniqueName, "LEO2A4", StringComparison.OrdinalIgnoreCase))
                return;

            Vehicle vehicle = __result as Vehicle;

            if (vehicle == null)
            {
                MelonLogger.Warning("[LEO2A4 CUSTOMIZER] Spawn returned an IUnit without a Vehicle component.");
                return;
            }

            vehicle._uniqueName = "LEO2A4";
            vehicle._friendlyName = "Leopard 2A4 (1988)";

            //MelonLogger.Msg("[LEO2A4 CUSTOMIZER] Spawned Leopard 2K: identity changed to LEO2K.");
        }
    }
}