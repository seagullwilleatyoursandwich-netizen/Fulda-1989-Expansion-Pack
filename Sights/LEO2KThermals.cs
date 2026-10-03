using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Fulda1989;
using GHPC.Camera;
using GHPC.Equipment.Optics;
using GHPC.Vehicle;
using MelonLoader;
using Reticle;
using UnityEngine;

namespace FuldaExpansion1989.Sights
{
    internal static class LEO2KThermals
    {
        private const float WIDE_FOV = 16.0f;
        private const float NARROW_FOV = 4.0f;

        internal enum ThermalVariant
        {
            Leopard2K1,
            Leopard2K2,
            Leopard2K3
        }

        private sealed class ThermalPreset
        {
            public bool OverrideResolution;

            public int FLIRWidth;
            public int FLIRHeight;

            public float BaseBlur;
            public float VibrationBlurScale;
            public float VibrationShakeMultiplier;

            public bool VibrationPreBlur;
        }

        private static readonly ThermalPreset LEO2K1_PRESET =
            new ThermalPreset
            {
                OverrideResolution = true,
                FLIRWidth = 640,
                FLIRHeight = 360,

                BaseBlur = 0.005f,
                VibrationBlurScale = 0.10f,
                VibrationShakeMultiplier = 0.02f,
                VibrationPreBlur = false
            };

        private static readonly ThermalPreset LEO2K2_PRESET =
            new ThermalPreset
            {
                OverrideResolution = true,
                FLIRWidth = 832,
                FLIRHeight = 468,

                BaseBlur = 0f,
                VibrationBlurScale = 0.05f,
                VibrationShakeMultiplier = 0.005f,
                VibrationPreBlur = false
            };

        private static readonly ThermalPreset LEO2K3_PRESET =
            new ThermalPreset
            {
                OverrideResolution = true,
                FLIRWidth = 1024,
                FLIRHeight = 576,

                BaseBlur = 0f,
                VibrationBlurScale = 0f,
                VibrationShakeMultiplier = 0f,
                VibrationPreBlur = false
            };

        private static Material flir_blit_material;
        private static GameObject flir_post_green;

        private static ThermalDonor thermal_donor;

        private static readonly FieldInfo f_reticleMesh_reticle = typeof(ReticleMesh).GetField("reticle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo f_reticleMesh_smr = typeof(ReticleMesh).GetField("SMR", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private sealed class ReticleClone
        {
            public ReticleSO Tree;
            public object Cached;
        }

        private sealed class ThermalDonor
        {
            public ReticleMesh NfovMesh;
            public ReticleMesh WfovMesh;

            public float DefaultFov;
            public float[] OtherFovs;
        }

        // ============================================================
        // ENTRY POINT
        // ============================================================

        internal static void Add(UsableOptic optic, ThermalVariant variant)
        {

            if (optic == null)
            {
                MelonLogger.Warning("[LEO2K] Thermal setup failed: optic is null.");
                return;
            }

            try
            {
                ApplyThermal(optic, GetPreset(variant));
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[LEO2K] Thermal setup failed on {optic.name}: " + $"{ex.GetType().Name}: {ex.Message}");
            }
        }
        private static ThermalPreset GetPreset(ThermalVariant variant)
        {
            switch (variant)
            {
                case ThermalVariant.Leopard2K1:
                    return LEO2K1_PRESET;

                case ThermalVariant.Leopard2K2:
                    return LEO2K2_PRESET;

                case ThermalVariant.Leopard2K3:
                    return LEO2K3_PRESET;

                default:
                    return LEO2K1_PRESET;
            }
        }

        // ============================================================
        // MAIN SETUP
        // ============================================================

        private static void ApplyThermal(UsableOptic optic, ThermalPreset preset)
        {
            LoadThermalResources();

            CameraSlot slot = optic.slot;

            if (f_optic_hasOverrideRangeLimits != null)
            {
                try
                {
                    f_optic_hasOverrideRangeLimits.SetValue(optic, false);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[LEO2K] Failed to clear optic range-limit override: {ex.Message}");
                }
            }

            if (slot == null)
            {
                MelonLogger.Warning($"[LEO2K] {optic.name} has no CameraSlot.");
                return;
            }

            // Vanilla night optic post-processing is not wanted.
            optic.post = null;

            // --------------------------------------------------------
            // THERMAL CAMERA
            // --------------------------------------------------------

            slot.VisionType = NightVisionType.Thermal;

            slot.BaseBlur = preset.BaseBlur;
            slot.VibrationBlurScale = preset.VibrationBlurScale;
            slot.VibrationShakeMultiplier = preset.VibrationShakeMultiplier;
            slot.VibrationPreBlur = preset.VibrationPreBlur;

            if (preset.OverrideResolution)
            {
                slot.OverrideFLIRResolution = true;
                slot.FLIRWidth = preset.FLIRWidth;
                slot.FLIRHeight = preset.FLIRHeight;
            }

            slot.CanToggleFlirPolarity = true;
            slot.FLIRFilterMode = FilterMode.Point;

            if (flir_blit_material != null)
                slot.FLIRBlitMaterialOverride = flir_blit_material;

            // --------------------------------------------------------
            // THERMAL RETICLE
            // --------------------------------------------------------

            ConfigureThermalReticle(optic);

            // --------------------------------------------------------
            // GREEN FLIR POST PROCESSING
            // --------------------------------------------------------

            if (flir_post_green != null &&
                optic.transform.Find("__LEO2K_FLIR_POST__") == null)
            {
                GameObject post = UnityEngine.Object.Instantiate(flir_post_green, optic.transform);

                post.name = "__LEO2K_FLIR_POST__";
                post.SetActive(true);
            }

            MelonLogger.Msg($"[LEO2K] Thermal applied to {optic.name}.");
        }

        // ============================================================
        // LOAD RESOURCE CONTROLLER ASSETS
        // ============================================================

        private static void LoadThermalResources()
        {
            FuldaResourceController.LoadDynamicAssets();

            flir_blit_material =  FuldaResourceController.GetThermalFlirBlitMaterial();

            flir_post_green = FuldaResourceController.GetThermalFlirPostPrefab();
        }

        // ============================================================
        // LOAD MARDER FLIR DONOR
        // ============================================================

        private static ThermalDonor LoadThermalDonor()
        {
            if (thermal_donor != null && thermal_donor.NfovMesh != null && thermal_donor.WfovMesh != null && thermal_donor.NfovMesh.reticleSO != null && thermal_donor.WfovMesh.reticleSO != null)
            {
                return thermal_donor;
            }

            Vehicle marder = FuldaAssetUtil.PrewarmVanillaVehicle("MARDER1A2", new[]
                    {
                        "Marder1A1_rig/hull/turret/FLIR",
                        "FLIR",
                        "Marder1A1_rig/hull/turret/PERI Z11",
                        "PERI Z11"
                    });

            if (marder == null)
            {
                MelonLogger.Warning("[LEO2K] Marder 1A2 thermal donor could not be loaded.");
                return null;
            }

            // --------------------------------------------------------
            // Find FLIR optic
            // --------------------------------------------------------

            UsableOptic flir = marder.transform.Find("Marder1A1_rig/hull/turret/FLIR")?.GetComponent<UsableOptic>();

            if (flir == null)
            {
                flir = marder.GetComponentsInChildren<UsableOptic>(true).FirstOrDefault(x => x != null && string.Equals(x.name,"FLIR",StringComparison.OrdinalIgnoreCase));
            }

            if (flir == null)
            {
                MelonLogger.Warning("[LEO2K] Marder FLIR UsableOptic could not be found.");return null;
            }

            // --------------------------------------------------------
            // Find donor reticles
            // --------------------------------------------------------

            ReticleMesh nfov = FindReticleMesh(flir,"Reticle Mesh","NFOV");

            ReticleMesh wfov = FindReticleMesh(flir,"Reticle Mesh WFOV","WFOV");

            if (nfov == null) nfov = flir.reticleMesh;

            if (nfov == null || wfov == null)
            {
                MelonLogger.Warning("[LEO2K] Marder FLIR reticles missing: " + $"NFOV={nfov != null}, WFOV={wfov != null}");
                return null;
            }

            thermal_donor = new ThermalDonor
            {
                NfovMesh = nfov,
                WfovMesh = wfov,
                DefaultFov = WIDE_FOV,
                OtherFovs = new[] { NARROW_FOV }
            };

            MelonLogger.Msg( "[LEO2K] Marder FLIR thermal donor loaded.");

            return thermal_donor;
        }

        // ============================================================
        // FIND RETICLE MESH
        // ============================================================

        private static ReticleMesh FindReticleMesh(
            UsableOptic optic,
            params string[] names)
        {
            if (optic == null)
                return null;

            // Exact child lookup first.
            if (names != null)
            {
                foreach (string name in names)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    Transform child = optic.transform.Find(name);

                    if (child == null)
                        continue;

                    ReticleMesh mesh = child.GetComponent<ReticleMesh>();

                    if (mesh != null)
                        return mesh;
                }
            }

            // Then search descendants
            ReticleMesh[] meshes = optic.GetComponentsInChildren<ReticleMesh>(true);

            return meshes.FirstOrDefault(mesh => mesh != null && names != null && names.Any(name =>!string.IsNullOrWhiteSpace(name) && mesh.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        // ============================================================
        // APPLY RETICLE
        // ============================================================

        private static void ConfigureThermalReticle(
            UsableOptic optic)
        {
            ThermalDonor donor = LoadThermalDonor();

            if (donor == null)
                return;

            ReticleMesh host = optic.reticleMesh;

            if (host == null)
            {
                host = optic.GetComponentsInChildren<ReticleMesh>(true).FirstOrDefault();
            }

            if (host == null)
            {
                MelonLogger.Warning($"[LEO2K] No host ReticleMesh found on {optic.name}.");
                return;
            }

            host.nightBrightness = 1f;

            // --------------------------------------------------------
            // Copy Marder NFOV reticle into Leopard optic
            // --------------------------------------------------------

            ReticleClone nfov = CloneReticle(donor.NfovMesh, "__LEO2K_THERMAL_NFOV__");

            if (nfov == null)
            {
                MelonLogger.Warning("[LEO2K] Failed to clone Marder NFOV reticle.");
                return;
            }

            AssignReticle(host, nfov);

            // --------------------------------------------------------
            // Copy Marder WFOV mesh
            // --------------------------------------------------------

            ReticleMesh wfov = CloneReticleMesh(donor.WfovMesh, optic.transform, "__LEO2K_THERMAL_WFOV__");

            if (wfov == null)
            {
                MelonLogger.Warning("[LEO2K] Failed to clone Marder WFOV reticle.");
                return;
            }

            // --------------------------------------------------------
            // Thermal zoom
            // --------------------------------------------------------

            ConfigureThermalFov(optic.slot,donor);

            EnsureVisible(host);
            EnsureVisible(wfov);

            MelonLogger.Msg($"[LEO2K] Thermal reticle installed on {optic.name}.");
        }

        // ============================================================
        // FOV
        // ============================================================

        private static void ConfigureThermalFov(
            CameraSlot slot,
            ThermalDonor donor)
        {
            if (slot == null || donor == null)
                return;

            slot.DefaultFov = donor.DefaultFov;

            slot.OtherFovs = donor.OtherFovs != null ? (float[])donor.OtherFovs.Clone() : new[] { NARROW_FOV };

            try
            {
                slot.ForceUpdateFov();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[LEO2K] Thermal FOV update failed: {ex.Message}");
            }
        }

        // ============================================================
        // RETICLE CLONE
        // ============================================================

        private static ReticleClone CloneReticle(ReticleMesh source, string name)
        {
            if (source == null || source.reticleSO == null)
            {
                return null;
            }

            ReticleSO tree = UnityEngine.Object.Instantiate(source.reticleSO);

            tree.name = name;

            object cached = null;

            try
            {
                if (f_reticleMesh_reticle != null)
                {
                    cached = f_reticleMesh_reticle.GetValue(source);
                }
            }
            catch
            {
            }

            object cachedClone = CloneCachedReticle(cached, tree);

            return new ReticleClone
            {
                Tree = tree,
                Cached = cachedClone
            };
        }

        private static object CloneCachedReticle(
            object source,
            ReticleSO tree)
        {
            if (source == null || tree == null)
            {
                return null;
            }

            Type type = source.GetType();

            object clone;

            try
            {
                clone = FormatterServices.GetUninitializedObject(type);
            }
            catch
            {
                return null;
            }

            CopyFields(clone, source);

            FieldInfo treeField = FindField(type, "tree");

            FieldInfo meshField = FindField(type, "mesh");

            try
            {
                treeField?.SetValue(clone, tree);
            }
            catch
            {
            }

            try
            {
                meshField?.SetValue(clone, null);
            }
            catch
            {
            }

            return clone;
        }

        private static FieldInfo FindField(Type type,string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(name,BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        private static readonly FieldInfo f_optic_hasOverrideRangeLimits = typeof(UsableOptic).GetField("<HasOverrideRangeLimits>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void CopyFields(object target, object source)
        {
            if (target == null || source == null)
            {
                return;
            }

            Type type = source.GetType();

            while (type != null)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        field.SetValue(target,field.GetValue(source));
                    }
                    catch
                    {
                    }
                }

                type = type.BaseType;
            }
        }

        // ============================================================
        // CLONE WHOLE RETICLE MESH
        // ============================================================

        private static ReticleMesh CloneReticleMesh(
            ReticleMesh source,
            Transform parent,
            string name)
        {
            if (source == null || parent == null)
            {
                return null;
            }

            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject,parent);

            if (clone == null)
                return null;

            clone.name = name;

            ReticleMesh mesh = clone.GetComponent<ReticleMesh>();

            if (mesh == null)
                return null;

            ReticleClone reticle = CloneReticle(source,name + "_Reticle");

            if (reticle != null)
            {
                AssignReticle(mesh,reticle);
            }

            return mesh;
        }

        // ============================================================
        // ASSIGN RETICLE TO MESH
        // ============================================================

        private static void AssignReticle(
            ReticleMesh mesh,
            ReticleClone clone)
        {
            if (mesh == null || clone == null || clone.Tree == null)
            {
                return;
            }

            mesh.reticleSO = clone.Tree;

            try
            {
                if (f_reticleMesh_reticle != null)
                {
                    f_reticleMesh_reticle.SetValue(mesh,clone.Cached);
                }
            }
            catch
            {
            }

            try
            {
                if (f_reticleMesh_smr != null)
                {
                    f_reticleMesh_smr.SetValue(mesh,null);
                }
            }
            catch
            {
            }

            try
            {
                mesh.Load();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[LEO2K] ReticleMesh.Load failed: {ex.Message}");
            }
        }

        // ============================================================
        // VISIBILITY
        // ============================================================

        private static void EnsureVisible(
            ReticleMesh mesh)
        {
            if (mesh == null)
                return;

            try
            {
                mesh.gameObject.SetActive(true);
                mesh.enabled = true;

                SkinnedMeshRenderer renderer = mesh.GetComponent<SkinnedMeshRenderer>();

                if (renderer != null)
                    renderer.enabled = true;

                foreach (PostMeshComp postMesh in mesh.GetComponentsInChildren<PostMeshComp>(true))
                {
                    if (postMesh != null)
                        postMesh.enabled = true;
                }
            }
            catch
            {
            }
        }
    }
}