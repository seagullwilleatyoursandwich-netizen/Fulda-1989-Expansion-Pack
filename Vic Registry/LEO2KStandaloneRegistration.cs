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
    public static class LEO2KStandaloneRegistration
    {
        private const string SourceUniqueName = "LEO1A4";
        private const string LEO2KUniqueName = "LEO2K";

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
                    MelonLogger.Error("[LEO2K CUSTOMIZER] Native LEO1A4 has no prefab AssetReference.");
                    yield break;
                }

                if (lookup.AllUnits.Any(metadata => metadata != null && string.Equals(metadata.Name, LEO2KUniqueName, StringComparison.OrdinalIgnoreCase)))
                {
                    _registered = true;
                    //MelonLogger.Msg("[LEO2K CUSTOMIZER] Leopard 2K metadata is already registered.");
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
                    Name = LEO2KUniqueName,
                    FriendlyName = "Leopard 2K"
                });

                lookup.AllUnits = allUnits.ToArray();

                _registered = true;

                //MelonLogger.Msg("[LEO2K CUSTOMIZER] Registered Leopard 2K directly in UnitPrefabLookup using native LEO1A4 prefab.");
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
    public static class LEO2KCustomizerSpawnPatch
    {
        public static void Postfix(string uniqueName, IUnit __result)
        {
            if (!string.Equals(uniqueName, "LEO2K", StringComparison.OrdinalIgnoreCase))
                return;

            Vehicle vehicle = __result as Vehicle;

            if (vehicle == null)
            {
                MelonLogger.Warning("[LEO2K CUSTOMIZER] Spawn returned an IUnit without a Vehicle component.");
                return;
            }

            vehicle._uniqueName = "LEO2K";
            vehicle._friendlyName = "Leopard 2K";

            //MelonLogger.Msg("[LEO2K CUSTOMIZER] Spawned Leopard 2K: identity changed to LEO2K.");
        }
    }
}