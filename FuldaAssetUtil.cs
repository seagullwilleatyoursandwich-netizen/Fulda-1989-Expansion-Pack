using System;
using System.Collections.Generic;
using System.Linq;
using GHPC.Equipment.Optics;
using GHPC.Mission;
using GHPC.Vehicle;
using Reticle;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Fulda1989
{
    internal static class FuldaAssetUtil
    {
        private static UnitPrefabLookupScriptable.UnitPrefabMetadata[] lookupAllUnits;
        private static readonly List<AssetReference> loadedAssetReferences = new List<AssetReference>();
        private static readonly HashSet<string> prewarmedVehicleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal static Vehicle PrewarmVanillaVehicle(string name, string[] probePaths = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            Vehicle vehicle = LoadVanillaVehicle(name);
            if (vehicle == null)
            {
                return null;
            }

            string cacheKey = name;
            if (prewarmedVehicleKeys.Contains(cacheKey))
                return vehicle;

            try
            {
                Transform[] transforms = vehicle.GetComponentsInChildren<Transform>(true);
                UsableOptic[] optics = vehicle.GetComponentsInChildren<UsableOptic>(true);
                ReticleMesh[] meshes = vehicle.GetComponentsInChildren<ReticleMesh>(true);

                int resolvedProbes = 0;
                if (probePaths != null)
                {
                    for (int i = 0; i < probePaths.Length; i++)
                    {
                        string probePath = probePaths[i];
                        if (string.IsNullOrWhiteSpace(probePath))
                            continue;

                        Transform probe = vehicle.transform.Find(probePath);
                        if (probe == null)
                        {
                            probe = vehicle.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t != null && string.Equals(t.name, probePath, StringComparison.OrdinalIgnoreCase));
                        }

                        if (probe != null)
                        {
                            resolvedProbes++;
                            probe.GetComponents<Component>();
                            probe.GetComponentsInChildren<Component>(true);
                        }
                    }
                }

                prewarmedVehicleKeys.Add(cacheKey);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[Assets] Vanilla donor prewarm failed: {name} | {ex.Message}");
            }

            return vehicle;
        }

        internal static Vehicle LoadVanillaVehicle(string name)
        {
            if (lookupAllUnits == null)
            {
                var lookup = Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>().FirstOrDefault();
                lookupAllUnits = lookup != null ? lookup.AllUnits : null;
            }

            if (lookupAllUnits == null)
                return null;

            var metadata = lookupAllUnits.FirstOrDefault(o => o != null && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
            if (metadata == null || metadata.PrefabReference == null)
                return null;

            AssetReference prefabRef = metadata.PrefabReference;
            if (prefabRef.Asset == null)
            {
                if (!loadedAssetReferences.Contains(prefabRef))
                    loadedAssetReferences.Add(prefabRef);

                var loaded = prefabRef.LoadAssetAsync<GameObject>().WaitForCompletion();
                return loaded != null ? loaded.GetComponent<Vehicle>() : null;
            }

            return (prefabRef.Asset as GameObject)?.GetComponent<Vehicle>();
        }

        internal static void ReleaseVanillaAssets()
        {
            foreach (var prefabRef in loadedAssetReferences)
            {
                try { prefabRef.ReleaseAsset(); }
                catch { }
            }

            loadedAssetReferences.Clear();
            prewarmedVehicleKeys.Clear();
        }

        internal static GameObject CloneInactive(GameObject source, string cloneName = null)
        {
            if (source == null) return null;

            bool wasActive = source.activeSelf;
            source.SetActive(false);
            GameObject clone = UnityEngine.Object.Instantiate(source);
            source.SetActive(wasActive);

            if (!string.IsNullOrEmpty(cloneName))
                clone.name = cloneName;

            return clone;
        }
    }
}
