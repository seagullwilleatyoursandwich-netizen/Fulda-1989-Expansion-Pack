using GHPC;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using ModUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader.Utils;
using System.IO;
using System.Linq;

namespace Fulda1989
{
    public class LEO2A4 : Module
    {
        public static MelonPreferences_Entry<bool> leo2a4_patch;
        public static bool PatchEnabled => leo2a4_patch != null && leo2a4_patch.Value;
        public static Material ctechAarMaterial;
        private static AssetBundle leo2a4Bundle;
        public static void Config(MelonPreferences_Category category)
        {
            leo2a4_patch = category.CreateEntry("Leopard 2A4 Patch", true);
            leo2a4_patch.Comment = "Enables the Leopard 2A4 1988 (Standalone - Found inside the vehicle customiser)";
        }
        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;
                //MelonLogger.Msg("Detected vehicle: " + vic.FriendlyName);

                GameObject vic_go = vic.gameObject;
                if (!vic.UniqueName.Contains("LEO2A4")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                vic_go.AddComponent<AlreadyConverted>();
                ApplyCtechArmour(vic);

                LoadoutManager loadout_manager = vic.GetComponent<LoadoutManager>();
                WeaponsManager weapons_manager = vic.GetComponent<WeaponsManager>();

                if (loadout_manager != null &&
                    loadout_manager.LoadedAmmoList != null &&
                    loadout_manager.LoadedAmmoList.AmmoClips != null &&
                    loadout_manager.LoadedAmmoList.AmmoClips.Length > 0 &&
                    weapons_manager != null &&
                    weapons_manager.Weapons != null &&
                    weapons_manager.Weapons.Length > 0 &&
                    weapons_manager.Weapons[0].Weapon != null)
                {
                    WeaponSystem main_gun = weapons_manager.Weapons[0].Weapon;

                    loadout_manager.LoadedAmmoList.AmmoClips[0] = Ammo_120mm_Bundeswehr.ap["DM33"];

                    for (int i = 0; i < loadout_manager.RackLoadouts.Length; i++)
                        Util.EmptyRack(loadout_manager.RackLoadouts[i].Rack);

                    loadout_manager.SpawnCurrentLoadout();

                    if (main_gun.Feed != null)
                    {
                        main_gun.Feed.AmmoTypeInBreech = null;
                        main_gun.Feed.Start();
                    }

                    loadout_manager.RegisterAllBallistics();
                }
                else
                {
                    MelonLogger.Warning("[LEO2A4] Could not update the DM33 ammunition loadout.");
                }
                Transform skirtdown = vic_go.transform.Find("LEO2_mesh/rear skirt down");
                skirtdown.gameObject.SetActive(true);
                Transform altskirt = vic_go.transform.Find("LEO2_mesh/rear skirt unclipped");
                altskirt.gameObject.SetActive(false);
                Transform skirtup = vic_go.transform.Find("LEO2_mesh/rear skirt up");
                skirtup.gameObject.SetActive(false);

                yield return null;
            }
        }


        private static void ApplyCtechArmour(Vehicle vic)
        {
            Armour.Init();

            if (Armour.ctech == null)
            {
                MelonLogger.Error("[LEO2A4 ARMOUR] C-Tech composite is not initialized.");
                return;
            }

            Transform turretRoot = Find2A4ArmourRoot(vic, "LEO2A4 TURRET COLLIDERS");
            Transform gunRoot = Find2A4ArmourRoot(vic, "LEO2A4 GUN COLLIDERS");
            Transform hullRoot = Find2A4ArmourRoot(vic, "LEO2A4 HULL COLLIDERS");

            if (turretRoot == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] Could not find LEO2A4 TURRET COLLIDERS.");

            if (gunRoot == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] Could not find LEO2A4 GUN COLLIDERS.");

            if (hullRoot == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] Could not find LEO2A4 HULL COLLIDERS.");

            Material eberAarMaterial = ctechAarMaterial;

            if (eberAarMaterial == null)
                MelonLogger.Warning("[LEO2A4 ARMOUR] C-Tech AAR material unavailable; armour conversion will continue without changing AAR materials.");

            int updated = 0;

            // TURRET FRONT ARRAYS
            ApplyCtechAtPath(turretRoot,"LEO2_armour_turret/turret armour/cheek array left A", 220f, eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/cheek array right A", 220f, eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/cheek array right B", 220f, eberAarMaterial, ref updated);

            // TURRET SIDE ARRAYS
            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/array left", 100f, eberAarMaterial, ref updated);

            ApplyCtechAtPath(turretRoot, "LEO2_armour_turret/turret armour/array right", 100f, eberAarMaterial, ref updated);

            // GUN MANTLET
            // The path matcher intentionally returns ALL matching objects,
            // including duplicates with identical names and hierarchy paths.
            ApplyCtechAtPath(gunRoot,"mantlet armour/mantlet", 130f, eberAarMaterial, ref updated);

            // HULL FRONT ARRAYS
            ApplyCtechAtPath(hullRoot,"LEO2_armour_hull/upper glacis array", 200f, eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot,"LEO2_armour_hull/lower glacis array", 200f, eberAarMaterial, ref updated);

            // HULL FRONT LEFT/RIGHT COMPOSITE ARRAYS
            ApplyCtechAtPath(hullRoot,"LEO2_armour_hull/front left array", 80f, eberAarMaterial, ref updated);

            ApplyCtechAtPath(hullRoot, "LEO2_armour_hull/front right array", 80f, eberAarMaterial, ref updated);

            //MelonLogger.Msg($"[LEO2A4 ARMOUR] C-Tech applied to {updated} existing VariableArmor components.");
        }

        private static Transform Find2A4ArmourRoot(Vehicle vic, string rootName)
        {
            // Check the vehicle hierarchy first.
            foreach (Transform candidate in vic.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == rootName)
                    return candidate;
            }

            // Also search LateFollow hierarchies associated with this vehicle.
            foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
            {
                if (follow == null || !follow.gameObject.scene.IsValid())
                    continue;

                Component parentUnit = follow.ParentUnit as Component;
                Transform originalParent = follow.OriginalParent;

                bool belongsToVehicle = parentUnit == vic || (parentUnit != null && parentUnit.gameObject == vic.gameObject) || originalParent == vic.transform || (originalParent != null && originalParent.IsChildOf(vic.transform));

                if (!belongsToVehicle)
                    continue;

                foreach (Transform candidate in follow.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == rootName)
                        return candidate;
                }
            }

            return null;
        }

        private static List<Transform> FindAllExactArmourPaths(Transform root, string relativePath)
        {
            List<Transform> matches = new List<Transform>();

            if (root == null)
                return matches;

            string[] segments = relativePath.Split(new[] { '/' });

            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                Transform cursor = candidate;
                bool matchesPath = true;

                for (int i = segments.Length - 1; i >= 0; i--)
                {
                    if (cursor == null || cursor.name != segments[i])
                    {
                        matchesPath = false;
                        break;
                    }

                    cursor = cursor.parent;
                }

                if (matchesPath && cursor == root)
                    matches.Add(candidate);
            }

            return matches;
        }

        private static void ApplyCtechAtPath(
            Transform root,
            string relativePath,
            float averageRha,
            Material eberAarMaterial,
            ref int updated)
        {
            if (root == null)
                return;

            List<Transform> targets = FindAllExactArmourPaths(root, relativePath);

            if (targets.Count == 0)
            {
                MelonLogger.Warning($"[LEO2A4 ARMOUR] No object matched '{root.name}/{relativePath}'.");
                return;
            }

            foreach (Transform target in targets)
            {
                VariableArmor armor = target.GetComponent<VariableArmor>();

                if (armor == null)
                {
                    MelonLogger.Warning($"[LEO2A4 ARMOUR] '{target.name}' has no VariableArmor component.");
                    continue;
                }

                string previousType = armor._armorType != null ? armor._armorType.name : "NULL";

                // Replace the existing armour configuration; do not add another component.
                armor._armorType = Armour.ctech;
                armor.AverageRha = averageRha;
                armor._name = "C-Tech composite";

                AarVisual aar = target.GetComponent<AarVisual>();

                if (aar != null && eberAarMaterial != null)
                {
                    aar.AarMaterial = eberAarMaterial;
                    aar.UpdateVisibility();
                }
                else if (aar == null)
                {
                    MelonLogger.Warning($"[LEO2A4 ARMOUR] '{target.name}' has no AarVisual component.");
                }

                updated++;

                //MelonLogger.Msg($"[LEO2A4 ARMOUR] {target.name}: {previousType} -> {Armour.ctech.name}, AverageRha={averageRha}.");
            }
        }

        private static Material GetEberAarMaterial()
        {
            GameObject bundlePrefab = LEO2K_Assets.leo2k_full;

            if (bundlePrefab == null)
                return null;

            string[] eberAarPaths =
            {
        "hull/l2ka2 hull/AAR",
        "turret/l2ka2 turret/AAR",
        "mantlet/l2ka2 mantlet/AAR"
        };

            foreach (string path in eberAarPaths)
            {
                Transform aarTransform = bundlePrefab.transform.Find(path);

                if (aarTransform == null)
                    continue;

                AarVisual aar = aarTransform.GetComponent<AarVisual>();

                if (aar == null)
                    aar = aarTransform.GetComponentInChildren<AarVisual>(true);

                if (aar != null && aar.AarMaterial != null)
                    return aar.AarMaterial;

                // The bundled AAR meshes may contain their material even when the
                // prefab does not have an AarVisual component attached.
                Renderer renderer = aarTransform.GetComponentInChildren<Renderer>(true);

                if (renderer != null && renderer.sharedMaterial != null)
                    return renderer.sharedMaterial;
            }

            return null;
        }

        public override void LoadStaticAssets()
        {
            string path = Path.Combine(MelonEnvironment.ModsDirectory, "Fulda1989", "leo2a4");

            leo2a4Bundle = AssetBundle.LoadFromFile(path);

            if (leo2a4Bundle == null)
            {
                MelonLogger.Error("[LEO2A4 ASSETS] Failed to load leo2a4 AssetBundle.");
                return;
            }

            ctechAarMaterial = leo2a4Bundle.LoadAllAssets<Material>().FirstOrDefault(m => m != null && m.name == "aar2a4ctech");

            if (ctechAarMaterial == null)
            {
                MelonLogger.Error("[LEO2A4 ASSETS] Could not find material 'aar2a4ctech' in the AssetBundle.");
                return;
            }

            Shader aarShader = Shader.Find("Standard (FLIR)");

            if (aarShader == null)
            {
                MelonLogger.Error("[LEO2A4 ASSETS] Could not find Standard (FLIR) shader.");
                return;
            }

            ctechAarMaterial.shader = aarShader;
            ctechAarMaterial.renderQueue = 2000;
            ctechAarMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");

            //MelonLogger.Msg("[LEO2A4 ASSETS] Applied Standard (FLIR) shader and Eber render queue to C-Tech AAR material.");

            ctechAarMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;

            //MelonLogger.Msg("[LEO2A4 ASSETS] Loaded aar2a4ctech material successfully.");
        }

        public static void Init()
        {
            if (!leo2a4_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}