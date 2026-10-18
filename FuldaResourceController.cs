using GHPC.Camera;
using GHPC.Equipment.Optics;
using GHPC.Vehicle;
using MelonLoader;
using Reticle;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VanillaAssetUtil.AssetUtil;

namespace Fulda1989
{
    internal static class FuldaResourceController
    {
        private const string M60A3TtsPrefabName = "M60A3TTS";
        private const string M60A3TtsFriendlyName = "M60A3 TTS";

        private const string FlirPostPath = "Turret Scripts/Sights/FLIR/FLIR Post Processing - Green";

        private const string FlirSlotPath = "Turret Scripts/Sights/FLIR";

        private static bool initialized;
        private static bool opticsDynamicAssetsLoaded;

        internal static GameObject ThermalFlirPostPrefab { get; private set; }
        internal static Material ThermalFlirBlitMaterial { get; private set; }

        internal static void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
        }

        internal static void LoadDynamicAssets()
        {
            Initialize();

            if (opticsDynamicAssetsLoaded &&
                ThermalFlirPostPrefab != null &&
                ThermalFlirBlitMaterial != null)
            {
                return;
            }

            try
            {
                LoadOpticsDynamicAssets();
                opticsDynamicAssetsLoaded = true;

                MelonLogger.Msg("[Assets] Fulda thermal resources loaded.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[Assets] Fulda thermal resource load failed | {ex}");

                opticsDynamicAssetsLoaded = false;
            }
        }

        internal static void UnloadDynamicAssets()
        {
            if (!opticsDynamicAssetsLoaded)
                return;

            ThermalFlirPostPrefab = null;
            ThermalFlirBlitMaterial = null;

            opticsDynamicAssetsLoaded = false;

            MelonLogger.Msg("[Assets] Fulda thermal resources unloaded.");
        }

        internal static GameObject GetThermalFlirPostPrefab()
        {
            EnsureOpticsDynamicAssetsValid();
            return ThermalFlirPostPrefab;
        }

        internal static Material GetThermalFlirBlitMaterial()
        {
            EnsureOpticsDynamicAssetsValid();
            return ThermalFlirBlitMaterial;
        }

        internal static void PrewarmThermalResources()
        {
            Initialize();

            PrewarmVanillaVehicle(M60A3TtsPrefabName, new[]
            {FlirSlotPath,FlirPostPath});
        }
        private static void LoadOpticsDynamicAssets()
        {
            Vehicle m60a3 = LoadM60A3TtsVehicle();

            if (m60a3 == null)
                throw new InvalidOperationException("M60A3 TTS resource load failed.");

            Transform flirPost = m60a3.transform.Find(FlirPostPath);

            if (flirPost != null)
                ThermalFlirPostPrefab = flirPost.gameObject;

            CameraSlot flirSlot = m60a3.transform.Find(FlirSlotPath)  ?.GetComponent<CameraSlot>();

            if (flirSlot != null) ThermalFlirBlitMaterial = flirSlot.FLIRBlitMaterialOverride;

            if (ThermalFlirPostPrefab == null)
            {
                MelonLogger.Warning("[Assets] M60A3 TTS FLIR post-processing prefab not found.");
            }

            if (ThermalFlirBlitMaterial == null)
            {
                MelonLogger.Warning("[Assets] M60A3 TTS FLIR blit material not found.");
            }

            ThermalFlirPostPrefab?.SetActive(false);

            if (ThermalFlirPostPrefab != null)
                ThermalFlirPostPrefab.hideFlags = HideFlags.DontUnloadUnusedAsset;

            if (ThermalFlirBlitMaterial != null)
                ThermalFlirBlitMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        private static readonly HashSet<string> prewarmedVanillaVehicleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static Vehicle PrewarmVanillaVehicle(string name, string[] probePaths = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            Vehicle vehicle = AssetUtilApi.LoadVanillaVehicle(name, temp: true);
            if (vehicle == null)
            {
                MelonLoader.MelonLogger.Warning($"[Assets] Failed to load vanilla donor: {name}");
                return null;
            }

            if (prewarmedVanillaVehicleKeys.Contains(name))
                return vehicle;

            try
            {
                Transform[] transforms = vehicle.GetComponentsInChildren<Transform>(true);
                vehicle.GetComponentsInChildren<UsableOptic>(true);
                vehicle.GetComponentsInChildren<ReticleMesh>(true);

                if (probePaths != null)
                {
                    foreach (string probePath in probePaths)
                    {
                        if (string.IsNullOrWhiteSpace(probePath))
                            continue;

                        Transform probe = vehicle.transform.Find(probePath);

                        if (probe == null)
                        {
                            foreach (Transform candidate in transforms)
                            {
                                if (candidate != null &&
                                    string.Equals(candidate.name, probePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    probe = candidate;
                                    break;
                                }
                            }
                        }

                        if (probe != null)
                        {
                            probe.GetComponents<Component>();
                            probe.GetComponentsInChildren<Component>(true);
                        }
                    }
                }

                prewarmedVanillaVehicleKeys.Add(name);
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[Assets] Vanilla donor prewarm failed: {name} | {ex.Message}");
            }

            return vehicle;
        }

        private static void EnsureOpticsDynamicAssetsValid()
        {
            if (!opticsDynamicAssetsLoaded || ThermalFlirPostPrefab == null || ThermalFlirBlitMaterial == null)
            {
                LoadDynamicAssets();
            }
        }

        private static Vehicle LoadM60A3TtsVehicle()
        {
            Vehicle donor = AssetUtilApi.LoadVanillaVehicle(M60A3TtsPrefabName, temp: true);

            if (donor != null)
                return donor;

            donor = AssetUtilApi.LoadVanillaVehicle(M60A3TtsFriendlyName, temp: true);

            if (donor != null)
                return donor;

            return Resources.FindObjectsOfTypeAll<Vehicle>().FirstOrDefault(v =>v != null &&(string.Equals(v.name,M60A3TtsPrefabName,StringComparison.OrdinalIgnoreCase) || string.Equals(v.name,M60A3TtsFriendlyName,StringComparison.OrdinalIgnoreCase)|| string.Equals(v.FriendlyName,M60A3TtsFriendlyName,StringComparison.OrdinalIgnoreCase)));
        }
    }
}
