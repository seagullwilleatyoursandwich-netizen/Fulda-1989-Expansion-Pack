using FuldaExpansion1989.Sights;
using GHPC;
using GHPC.AI;
using GHPC.AI.Targeting;
using GHPC.Camera;
using GHPC.Effects;
using GHPC.Effects.Voices;
using GHPC.Equipment;
using GHPC.Equipment.AzimuthLocking;
using GHPC.Equipment.Optics;
using GHPC.State;
using GHPC.Thermals;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weaponry;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using NWH.VehiclePhysics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VanillaAssetUtil.AssetUtil;
using Module = ModUtil.Module;

namespace Fulda1989
{

    public class LEO2KAmmoVisualScaler : MonoBehaviour
    {
        private static readonly Vector3 AmmoScale = new Vector3(1.075f, 1.075f, 1.075f);

        private void LateUpdate()
        {
            foreach (AmmoStoredVisual visual in GetComponentsInChildren<AmmoStoredVisual>(true))
            {
                if (visual != null)
                    visual.transform.localScale = AmmoScale;
            }
        }
    }
    public class AARAlreadyMoved : MonoBehaviour
    {
    }
    public class RH202ServoFix : MonoBehaviour
    {
        public AimablePlatform Traverse;
        public Transform TraverseTransform;

        private static readonly FieldInfo f_useWorldSpace = typeof(AimablePlatform).GetField("_useWorldSpace",BindingFlags.Instance |BindingFlags.NonPublic);

        private static readonly FieldInfo f_goalAimVector =typeof(AimablePlatform).GetField("_goalAimVector",BindingFlags.Instance |BindingFlags.NonPublic);

        private void OnEnable()
        {
            Camera.onPreCull -= HandlePreCull;
            Camera.onPreCull += HandlePreCull;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= HandlePreCull;
        }

        private void LateUpdate()
        {
            Servo();
        }

        private void HandlePreCull(Camera cam)
        {
            if (cam != CameraManager.MainCam)return;
            Servo();
        }

        private void Servo()
        {
            if (Traverse == null || TraverseTransform == null)
                return;

            if (f_useWorldSpace == null || f_goalAimVector == null)
                return;

            bool useWorldSpace = (bool)f_useWorldSpace.GetValue(Traverse);

            if (!useWorldSpace)
                return;

            Vector3 aim = (Vector3)f_goalAimVector.GetValue(Traverse);

            if (aim.sqrMagnitude < 0.0625f)
                return;

            Vector3 up = TraverseTransform.parent != null ? TraverseTransform.parent.up : Vector3.up;

            aim = Vector3.ProjectOnPlane(aim, up);

            if (aim.sqrMagnitude < 0.0625f) return;
            aim.Normalize();

            Traverse.SetAimVector(aim,true,true,false,false);
            Traverse.ForceAimVectorNow(aim);

            TraverseTransform.rotation = Traverse.Rotation;
        }
    }
    public class LEO2K : Module
    {
        static WeaponSystemCodexScriptable gun_l44;
        static MelonPreferences_Entry<bool> leo2k_patch;
        static MelonPreferences_Entry<bool> leo2k1_enabled;
        static MelonPreferences_Entry<bool> leo2k2_enabled;
        static MelonPreferences_Entry<bool> leo2k3_enabled;
        static MelonPreferences_Entry<int> leo2k1_chance;
        static MelonPreferences_Entry<int> leo2k2_chance;
        static MelonPreferences_Entry<int> leo2k3_chance;
        static MelonPreferences_Entry<bool> leo2k_olive_drab;
        public static bool PatchEnabled => leo2k_patch != null && leo2k_patch.Value;

        // THIS SETS THE LEOPARD 2K's NEW TNT EQUIVALENT IN KG PER CHARGE, IT IS NECESSARY DUE TO THE OLD LEOPARD'S AMMORACK DETONATION REQUIREMENTS!
        private const float LEO2K_AMMO_TNT_EQUIVALENT = 3.25f;
        private static readonly FieldInfo f_ammoRackFlammables = typeof(GHPC.Weapons.AmmoRack).GetField("Flammables",
        BindingFlags.Instance |
        BindingFlags.Public |
        BindingFlags.NonPublic);
        public static void Config(MelonPreferences_Category category)
        {
            leo2k_patch = category.CreateEntry("Leopard 2K Patch", true);
            leo2k_patch.Comment = "Enables the Leopard 2K (Standalone - Found inside the vehicle customiser)";

            leo2k_olive_drab = category.CreateEntry("Leopard 2K Upgrades Use Olive Drab", false);
            leo2k_olive_drab.Comment = "Uses olive drab (Made by Illuminati and fcpx!) on Leopard 2KA1/2KA2/2KA3 instead of the standard tricolor camouflage.";

            leo2k1_enabled = category.CreateEntry("Leopard 2KA1 Enabled", true);
            leo2k1_enabled.Comment = "Enables/Disables Leopard 2KA1's from spawning, this will also disable 2KA2's and 2KA3's from spawning.";

            leo2k2_enabled = category.CreateEntry("Leopard 2KA2 Enabled", true);
            leo2k2_enabled.Comment = "Enables/Disables Leopard 2KA2's from spawning, this will also disable 2KA3's from spawning.";

            leo2k3_enabled = category.CreateEntry("Leopard 2KA3 Enabled", true);
            leo2k3_enabled.Comment = "Enables/Disables Leopard 2KA3's from spawning";

            leo2k1_chance = category.CreateEntry("Conversion chance for Leopard 2KA1 (%)", 80);
            leo2k1_chance.Comment = "Random chance for a Leopard 2K to be converted into a Leopard 2KA1 (1983) - Comes with an improved FCS (Thermals) and better 120mm ammunition.";

            leo2k2_chance = category.CreateEntry("Conversion chance for Leopard 2KA2 (%)", 60);
            leo2k2_chance.Comment = "Random chance for a Leopard 2KA1 to be converted into a Leopard 2KA2 (1986) - Further FCS improvements, better AP and HEAT for the 120mm, new armour package.";

            leo2k3_chance = category.CreateEntry("Conversion chance for Leopard 2KA3 (%)", 40);
            leo2k3_chance.Comment = "Random chance for a Leopard 2KA2 to be converted into a Leopard 2KA3 (1989) - Massive armour overhaul, EMES-12K FCS + PERI-R12K3 CITV - comes with rotate azimuth, experimental DM43K APFSDS round and DM63 APDS for the roof 20mm.";
        }
        public static IEnumerator Convert(GameState _)
        {
            MelonLogger.Msg($"[LEO2K RNG] Loaded chances: 2KA1={leo2k1_chance.Value}% | 2KA2={leo2k2_chance.Value}% | 2KA3={leo2k3_chance.Value}% | Enabled: 2KA1={leo2k1_enabled.Value}, 2KA2={leo2k2_enabled.Value}, 2KA3={leo2k3_enabled.Value}");
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;
                GameObject vic_go = vic.gameObject;
                if (vic.UniqueName != "LEO2K") continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null) continue;

                vic._friendlyName = "Leopard 2K";

                // 2K → 2K1
                if (vic.FriendlyName == "Leopard 2K")
                {
                    if (leo2k1_enabled.Value &&
                        UnityEngine.Random.Range(0, 100) < leo2k1_chance.Value)
                    {
                        vic._friendlyName = "Leopard 2KA1 'Vogel'";
                        //MelonLogger.Msg("[LEO2K] Leopard 2K -> Leopard 2K1");
                    }
                }

                // 2K1 → 2K2
                if (vic.FriendlyName == "Leopard 2KA1 'Vogel'")
                {
                    if (leo2k2_enabled.Value &&
                        UnityEngine.Random.Range(0, 100) < leo2k2_chance.Value)
                    {
                        vic._friendlyName = "Leopard 2KA2 'Falke'";
                        //MelonLogger.Msg("[LEO2K] Leopard 2K1 -> Leopard 2K2");
                    }
                }

                // 2K2 → 2K3
                if (vic.FriendlyName == "Leopard 2KA2 'Falke'")
                {
                    if (leo2k3_enabled.Value &&
                        UnityEngine.Random.Range(0, 100) < leo2k3_chance.Value)
                    {
                        vic._friendlyName = "Leopard 2KA3 'Adler'";
                        //MelonLogger.Msg("[LEO2K] Leopard 2K2 -> Leopard 2K3");
                    }
                }

                //MelonLogger.Msg($"[LEO2K] Final variant: {vic.FriendlyName}");

                vic_go.AddComponent<AlreadyConverted>();

                //MelonLogger.Msg("[LEO2K] ENTERED CONVERSION");

                Transform hull = vic.transform.Find("LEO1A1A1_rig/HULL");
                if (hull != null)
                {
                    const float ARM_X_SCALE = 1.25f;

                    for (int i = 1; i <= 7; i++)
                    {
                        Transform leftArm = hull.Find($"arm_l_{i}");
                        Transform rightArm = hull.Find($"arm_r_{i}");

                        if (leftArm != null)
                        {
                            Vector3 scale = leftArm.localScale;
                            scale.x *= ARM_X_SCALE;
                            leftArm.localScale = scale;
                        }

                        if (rightArm != null)
                        {
                            Vector3 scale = rightArm.localScale;
                            scale.x *= ARM_X_SCALE;
                            rightArm.localScale = scale;
                        }
                    }
                    //MelonLogger.Msg($"[LEO2K] Resized track wheel arms X by {ARM_X_SCALE}x.");
                }
                else
                {
                    MelonLogger.Warning("[LEO2K] Could not find LEO1A1A1_rig/HULL for wheel-arm scaling.");
                }
                Transform turret = vic.transform.Find("LEO1A1A1_rig/HULL/TURRET");

                Transform mantlet = vic.transform.Find("LEO1A1A1_rig/HULL/TURRET/1A3 mantlet");

                Transform oldcollider = vic.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "hull collider");


                if (oldcollider != null)
                {
                    oldcollider.gameObject.SetActive(false);
                }
                else
                {
                    MelonLogger.Warning("[LEO2K] Could not find vanilla hull collider!");
                }

                //MelonLogger.Msg("Hull target: " + hull);
                //MelonLogger.Msg("Turret target: " + turret);
                //MelonLogger.Msg("Mantlet target: " + mantlet);

                GameObject leo2k_full = GameObject.Instantiate(LEO2K_Assets.leo2k_full);
                GameObject _l44_obj = GameObject.Instantiate(Bundeswehr_120mm_Assets.l44_obj);
                ApplyLeo2KCamo(leo2k_full, vic.FriendlyName);
                ApplyLeo2KCamo(_l44_obj, vic.FriendlyName);

                Transform newcollider = leo2k_full.transform.Find("leo2k hull collider");
                Transform visual_gun = _l44_obj.transform;
                Transform visual_hull = leo2k_full.transform.Find("hull");
                Transform visual_turret = leo2k_full.transform.Find("turret");
                Transform visual_mantlet = leo2k_full.transform.Find("mantlet");
                Transform L2KA3visualhull = visual_hull.Find("l2ka3 hull");
                Transform L2KA3visualturret = visual_turret.Find("l2ka3 turret composites");
                Transform L2KA3visualmantlet = visual_mantlet.Find("l2ka3 mantlet");
                Transform L2KA2visualhull = visual_hull.Find("l2ka2 hull");
                Transform L2KA2visualturret = visual_turret.Find("l2ka2 turret");
                Transform L2KA2visualmantlet = visual_mantlet.Find("l2ka2 mantlet");
                Transform L2KA1visualhull = visual_hull.Find("l2ka1 hull");

                // HULL COLLIDER
                if (newcollider != null)
                {
                    newcollider.SetParent(hull, false);

                    newcollider.localPosition = new Vector3(8.03f, -1f, 0.0925f);
                    newcollider.localEulerAngles = new Vector3(0f, 0f, 0f);
                    newcollider.localScale = new Vector3(0.033f, 0.034f, 0.031f);
                }
                // HULL
                if (visual_hull != null)
                {
                    visual_hull.SetParent(hull, false);

                    visual_hull.localPosition = new Vector3(0f, -0.82f, 0.080f);
                    visual_hull.localEulerAngles = new Vector3(0f, 0f, 0f);
                    visual_hull.localScale = new Vector3(96f, 88f, 100f);

                    Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                    visual_hull.SetParent(hull_late_follow);
                }
                //MelonLogger.Msg("visual hull complete");

                // TURRET
                if (visual_turret != null)
                {
                    visual_turret.SetParent(turret, false);

                    visual_turret.localPosition = new Vector3(0f, -1.63f, -0.55f);
                    visual_turret.localEulerAngles = new Vector3(0f, 0f, 0f);
                    visual_turret.localScale = new Vector3(100f, 100f, 100f);

                    Transform turret_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                    visual_turret.SetParent(turret_late_follow);
                }
                //MelonLogger.Msg("visual turret complete");

                // MANTLET
                if (visual_mantlet != null)
                {
                    visual_mantlet.SetParent(mantlet, false);

                    visual_mantlet.localPosition = new Vector3(0f, -1.95f, -1.65f);
                    visual_mantlet.localEulerAngles = new Vector3(0f, 0f, 0f);
                    visual_mantlet.localScale = new Vector3(100f, 100f, 100f);

                    Transform mantlet_late_follow = mantlet.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                    visual_mantlet.SetParent(mantlet_late_follow);
                }
                //MelonLogger.Msg("visual mantlet complete");
                // GUN
                if (visual_gun != null)
                {
                    visual_gun.SetParent(mantlet, false);

                    visual_gun.transform.localPosition = new Vector3(0f, -0.02f, 2.565f);
                    visual_gun.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
                    visual_gun.transform.localScale = new Vector3(82.5f, 82.5f, 75f);

                    Transform mantlet_late_follow = mantlet.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                    visual_gun.SetParent(mantlet_late_follow);
                }
                // TRACK CHANGES
                // Replace the Leopard 2K track albedo while preserving the original TrackShader.
                // M60A3TTS T142_co is applied to the existing D640A material on both tracks.

                Vehicle m60Donor = AssetUtilApi.LoadVanillaVehicle("M60A3TTS", temp: true);

                if (m60Donor != null)
                {
                    Texture2D m60TrackAlbedo = null;

                    foreach (Renderer renderer in m60Donor.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer == null)
                            continue;

                        foreach (Material material in renderer.sharedMaterials)
                        {
                            if (material == null || !material.HasProperty("_MainTex"))
                                continue;

                            Texture texture = material.GetTexture("_MainTex");

                            if (texture == null)
                                continue;

                            string textureName = texture.name.Replace(" (Instance)", "");

                            if (!textureName.Equals("T142_co", StringComparison.OrdinalIgnoreCase))
                                continue;

                            m60TrackAlbedo = texture as Texture2D;
                            break;
                        }

                        if (m60TrackAlbedo != null)
                            break;
                    }

                    if (m60TrackAlbedo != null)
                    {
                        int applied = 0;

                        foreach (Renderer renderer in vic.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer == null)
                                continue;

                            if (!renderer.name.Equals("track_L", StringComparison.OrdinalIgnoreCase) &&
                                !renderer.name.Equals("track_R", StringComparison.OrdinalIgnoreCase))
                                continue;

                            Material[] materials = renderer.materials;

                            for (int i = 0; i < materials.Length; i++)
                            {
                                Material material = materials[i];

                                if (material == null)
                                    continue;

                                string materialName = material.name.Replace(" (Instance)", "");

                                if (!materialName.Equals("D640A", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                MaterialPropertyBlock block = new MaterialPropertyBlock();
                                renderer.GetPropertyBlock(block);

                                if (material.HasProperty("_AlbedoMult"))
                                    material.SetColor("_AlbedoMult", new Color(0.55f, 0.55f, 0.55f, 0f));

                                renderer.SetPropertyBlock(block);

                                applied++;

                                //MelonLogger.Msg($"[LEO2K MATERIAL] Applied M60 T142_co albedo to TrackShader on {renderer.name}.");
                            }

                            renderer.materials = materials;
                        }

                        //MelonLogger.Msg($"[LEO2K MATERIAL] T142_co applied to {applied} D640A track material slots.");
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K MATERIAL] Could not find T142_co on M60A3TTS donor.");
                    }
                }
                else
                {
                    MelonLogger.Warning("[LEO2K MATERIAL] Could not load M60A3TTS donor.");
                }

                //MelonLogger.Msg($"L44 loaded: {visual_gun.name}");
                //MelonLogger.Msg($"L44 position: {visual_gun.transform.position}");
                //MelonLogger.Msg($"L44 children: {visual_gun.transform.childCount}");
                //MelonLogger.Msg("[LEO2K] Finished visual conversion");
                // SHIFT LEOPARD AAR MODULES
                const float AAR_VERTICAL_OFFSET = 0f;
                const float AAR_FORWARD_OFFSET = 0.05f;
                const float AAR_SCALE_OFFSET = -0.05f;

                foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
                {
                    if (follow == null || !follow.gameObject.scene.IsValid() || follow.gameObject.name != "HULL ARMOR" || follow.GetComponent<AARAlreadyMoved>() != null)
                        continue;

                    Component parentUnit = follow.ParentUnit as Component;
                    Transform originalParent = follow.OriginalParent;

                    Component leo1a4 = parentUnit != null && parentUnit.name.Contains("LEO2K") ? parentUnit : originalParent != null && originalParent.name.Contains("LEO2K") ? originalParent : null;

                    if (leo1a4 == null || leo1a4.GetComponent<AlreadyConverted>() == null)
                        continue;

                    Transform hullArmour = follow.transform;
                    Transform[] children = hullArmour.GetComponentsInChildren<Transform>(true);

                    foreach (string aarName in new[] { "LEO1_AAR_hull" })
                    {
                        Transform aar = children.FirstOrDefault(t => t.name == aarName);

                        if (aar != null)
                        {
                            aar.localPosition += new Vector3(0f, AAR_VERTICAL_OFFSET, 0f);
                            //MelonLogger.Msg($"[LEO2K] Shifted {aarName} down by {Mathf.Abs(AAR_VERTICAL_OFFSET)}m.");
                        }
                        else
                        {
                            MelonLogger.Warning($"[LEO2K] Could not find {aarName} under HULL ARMOR.");
                        }
                    }

                    foreach (string aarName in new[] { "HULL AAR" })
                    {
                        Transform aar = children.FirstOrDefault(t => t.name == aarName);

                        if (aar != null)
                        {
                            aar.localPosition += new Vector3(0f, AAR_SCALE_OFFSET, 0f);
                            //MelonLogger.Msg($"[LEO2K] Shifted {aarName} down by {Mathf.Abs(AAR_SCALE_OFFSET)}m.");
                        }
                        else
                        {
                            MelonLogger.Warning($"[LEO2K] Could not find {aarName} under HULL ARMOR.");
                        }
                    }

                    Transform aarHull = children.FirstOrDefault(t => t.name == "LEO1_AAR_hull");
                    Transform nbc = aarHull?.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "NBC system");

                    if (nbc != null)
                    {
                        nbc.localPosition += new Vector3(0f, -0.05f, AAR_FORWARD_OFFSET);
                        //MelonLogger.Msg($"[LEO2K] Shifted LEO1_AAR_hull/NBC system down by {Mathf.Abs(AAR_VERTICAL_OFFSET)}m and forward {AAR_FORWARD_OFFSET}m.");
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Could not find LEO1_AAR_hull/NBC system under HULL ARMOR.");
                    }

                    if (aarHull != null)
                    {
                        aarHull.localScale += new Vector3(0f, AAR_SCALE_OFFSET, 0f);
                        aarHull.localPosition += new Vector3(0f, 0f, AAR_FORWARD_OFFSET);
                        //MelonLogger.Msg($"[LEO2K] Scaled LEO1_AAR_hull Z by {AAR_SCALE_OFFSET} and moved forward {AAR_FORWARD_OFFSET}m.");
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Could not find LEO1_AAR_hull under HULL ARMOR.");
                    }

                    follow.gameObject.AddComponent<AARAlreadyMoved>();
                    //MelonLogger.Msg($"[LEO2K] Marked {follow.gameObject.name} as AAR already moved.");
                }

                Transform rh202Gun = visual_turret.Find("20MM RIG/RH202_TRAVERSE_PIVOT/20mm_MOUNT/RH202_ELEVATION_PIVOT/20mm_GUN");

                if (rh202Gun != null)
                {
                    RH202.Leopard2KVariant rh202Variant;

                    switch (vic.FriendlyName)
                    {
                        case "Leopard 2KA1 'Vogel'": rh202Variant = RH202.Leopard2KVariant.KA1;
                            break;

                        case "Leopard 2KA2 'Falke'": rh202Variant = RH202.Leopard2KVariant.KA2;
                            break;

                        case "Leopard 2KA3 'Adler'": rh202Variant = RH202.Leopard2KVariant.KA3;
                            break;

                        default: rh202Variant = RH202.Leopard2KVariant.K; break;
                    }

                    RH202.Setup(vic, rh202Gun, rh202Variant);
                }
                else
                {
                    MelonLogger.Error("[LEO2K] Could not find RH202 20mm_GUN for Build 7.");
                }
                GameObject.Destroy(leo2k_full);

                // Determines per-variant ammo — hardcoded, no config, editable if needed - check the ammo bundeswehr 120mm entries for name values.
                string ap_round, heat_round;
                int ap_count, heat_count;

                switch (vic.FriendlyName)
                {
                    case "Leopard 2KA1 'Vogel'":
                        ap_round = "DM23"; heat_round = "DM12A1"; ap_count = 20; heat_count = 21; break;
                    case "Leopard 2KA2 'Falke'":
                        ap_round = "DM33"; heat_round = "DM12A1"; ap_count = 23; heat_count = 23; break;
                    case "Leopard 2KA3 'Adler'":
                        ap_round = "DM43K"; heat_round = "DM12A1K"; ap_count = 28; heat_count = 24; break;
                    default: // base "Leopard 2K"
                        ap_round = "DM13"; heat_round = "DM12"; ap_count = 18; heat_count = 23; break;
                }

                AmmoClipCodexScriptable sabotClipCodex = Ammo_120mm_Bundeswehr.ap[ap_round];

                AmmoClipCodexScriptable heatClipCodex = Ammo_120mm_Bundeswehr.heat[heat_round];

                LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();

                //MelonLogger.Msg($"[LEO2K] Rack count: {loadoutManager.RackLoadouts.Length}");

                for (int i = 0; i < loadoutManager.RackLoadouts.Length; i++)
                {
                    var rack = loadoutManager.RackLoadouts[i].Rack;

                    //MelonLogger.Msg($"[LEO2K] Rack {i}: " + $"Capacity={rack.ClipCapacity}, " + $"Stored={rack.StoredClips.Count}");
                }

                //for (int i = 0; i < loadoutManager.RackLoadouts.Length; i++)
                //{
                //    loadoutManager.RackLoadouts[i].FixedChoices = null;
                //    Util.EmptyRack(loadoutManager.RackLoadouts[i].Rack);
                //}

                //MelonLogger.Msg("[LEO2K] Finished ammo setup");

                // CONVERT 120MM MAIN GUN
                WeaponsManager weapons_manager = vic.GetComponent<WeaponsManager>();
                //MelonLogger.Msg($"[LEO2K] WeaponsManager found, Weapons count: {weapons_manager.Weapons.Length}, Weapons[0] name: {weapons_manager.Weapons[0].Name}");
                WeaponSystemInfo main_gun_info = weapons_manager.Weapons[0];
                WeaponSystem main_gun = main_gun_info.Weapon;

                main_gun_info.Name = "Rh120 L/44";
                main_gun.Impulse = 68000;
                main_gun.CodexEntry = gun_l44;
                main_gun.WeaponSound.SingleShotEventPaths[0] = "event:/Weapons/canon_120mm-RH120";

                Transform muzzleFlashes = main_gun.MuzzleEffects[1].transform;
                muzzleFlashes.GetChild(1).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(2).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(3).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(4).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);
                muzzleFlashes.GetChild(5).transform.localScale = new Vector3(1.3f, 1.3f, 1.1f);

                loadoutManager.TotalAmmoCounts = new int[] { ap_count, heat_count };
                loadoutManager.LoadedAmmoList.AmmoClips = new AmmoClipCodexScriptable[] { sabotClipCodex, heatClipCodex };
                loadoutManager._totalAmmoTypes = 2;
                loadoutManager._totalAmmoCount = ap_count + heat_count;

                AmmoClipCodexScriptable[] clips = loadoutManager.LoadedAmmoList.AmmoClips;
                clips[0] = sabotClipCodex;
                clips[1] = heatClipCodex;
                for (int t = 2; t < clips.Length; t++)
                    clips[t] = heatClipCodex;

                int[] counts = loadoutManager.TotalAmmoCounts;
                if (counts != null && counts.Length >= 2)
                {
                    counts[0] = ap_count;
                    counts[1] = heat_count;
                    for (int t = 2; t < counts.Length; t++)
                        counts[t] = 0;
                }

                loadoutManager._totalAmmoCount = ap_count + heat_count;
                // do not change _totalAmmoTypes

                for (int i = 0; i < loadoutManager.RackLoadouts.Length; i++)
                {
                    var rackLoadout = loadoutManager.RackLoadouts[i];

                    // Remove the Leopard 1A4's original ammo allocation.
                    rackLoadout.AmmoCounts = null;

                    // Remove all predefined visible-round choices.
                    // Use an empty array rather than null because SpawnRackLoadout()
                    // foreach'es this array without a null check.
                    rackLoadout.FixedChoices = new LoadoutManager.RackLoadoutFixedChoice[0];

                    // Remove original initial-ammo overrides (e.g. DM13).
                    rackLoadout.OverrideInitialClips = null;

                    // Remove any original restrictions on ammo types.
                    rackLoadout.ForbiddenAmmoIndices = new int[0];

                    // Actually empty the physical rack.
                    Util.EmptyRack(rackLoadout.Rack);
                }

                // Set the actual Leopard 2K physical ammunition storage.
                // Rack 0 = turret ring, Rack 1 = LFP/driver rack.
                // Racks 2 and 3 are unused.

                loadoutManager.RackLoadouts[0].Rack.ClipCapacity = 13; // Turret ring
                loadoutManager.RackLoadouts[2].Rack.ClipCapacity = 0;  // Turret floor
                loadoutManager.RackLoadouts[3].Rack.ClipCapacity = 0;  // Bustle racks

                // Base 2K and 2KA1 "Vogel" both carry 41 rounds.
                // Later armour variants progressively use more of the LFP storage.
                loadoutManager.RackLoadouts[1].Rack.ClipCapacity = 28;

                switch (vic.FriendlyName)
                {
                    case "Leopard 2KA2 'Falke'":
                        loadoutManager.RackLoadouts[1].Rack.ClipCapacity = 33; // 46 total
                        break;

                    case "Leopard 2KA3 'Adler'":
                        loadoutManager.RackLoadouts[1].Rack.ClipCapacity = 39; // 53 total
                        break;
                }

                // LFP/Driver area - remove the first three visual slots because they clip through the hull.
                GHPC.Weapons.AmmoRack frontRack = loadoutManager.RackLoadouts[1].Rack;

                int removedFrontRackSlots = frontRack.VisualSlots.RemoveAll(slot => slot != null && (slot.name == "rack_01" || slot.name == "rack_02" || slot.name == "rack_03"));

                frontRack.RegenerateSlotIndices();

                //MelonLogger.Msg($"[LEO2K] Removed {removedFrontRackSlots} slots from front_rack. " + $"Remaining slots: {frontRack.VisualSlots.Count}");

                //MelonLogger.Msg($"[LEO2K] Removed {removedFrontRackSlots} slots from front_rack. " + $"Remaining slots: {frontRack.VisualSlots.Count}");


                loadoutManager.SpawnCurrentLoadout(true);

                for (int i = 0; i < loadoutManager.RackLoadouts.Length; i++)
                {
                    GHPC.Weapons.AmmoRack rack = loadoutManager.RackLoadouts[i].Rack;
                    int visualCount = 0;
                    int missingType = 0;
                    int missingCodex = 0;

                    //MelonLogger.Msg($"[LEO2K RACK DEBUG] Rack {i}: Capacity={rack.ClipCapacity}, VisualSlots={rack.VisualSlots.Count}, StoredClips={rack.StoredClips.Count}");

                    foreach (Transform slot in rack.VisualSlots)
                    {
                        if (slot == null) continue;

                        AmmoStoredVisual visual = slot.GetComponentInChildren<AmmoStoredVisual>(true);
                        if (visual == null) continue;

                        visualCount++;

                        if (visual.AmmoType == null) missingType++;
                        if (visual.AmmoScriptable == null) missingCodex++;

                        //MelonLogger.Msg($"[LEO2K RACK DEBUG] Rack {i} | Slot={slot.name} | Visual={visual.name} | Type={visual.AmmoType?.Name ?? "NULL"} | Codex={visual.AmmoScriptable?.name ?? "NULL"}");
                    }

                    int mappedSlots = 0;
                    foreach (var entry in rack.SlotIndicesByAmmoType)
                    {
                        int count = entry.Value != null ? entry.Value.Count : 0;
                        mappedSlots += count;
                        //MelonLogger.Msg($"[LEO2K RACK DEBUG] Rack {i} | TrackedType={entry.Key?.Name ?? "NULL"} | MappedSlots={count}");
                    }

                    //MelonLogger.Msg($"[LEO2K RACK DEBUG] Rack {i} SUMMARY | Visuals={visualCount} | MappedSlots={mappedSlots} | MissingAmmoType={missingType} | MissingAmmoCodex={missingCodex}");
                }

                //MelonLogger.Msg($"[LEO2K] stored clips: {loadoutManager.RackLoadouts[0].Rack.StoredClips.Count}");

                //MelonLogger.Msg("[LEO2K] R: SpawnCurrentLoadout completed");

                main_gun.Feed.AmmoTypeInBreech = null;
                main_gun.Feed.Start();
                loadoutManager.RegisterAllBallistics();

                SetLeo2KAmmoExplosivity(loadoutManager);
                FireControlSystem fcs = vic.GetComponentInChildren<FireControlSystem>();

                if (fcs != null)
                {
                    UsableOptic day_optic = Util.GetDayOptic(fcs);
                    CameraSlot night_slot = day_optic?.slot?.LinkedNightSight;
                    UsableOptic night_optic = night_slot != null ? night_slot.PairedOptic : null;
                    fcs._originalRangeLimits = new Vector2(200f, 15000f);

                    if (day_optic != null)
                    {
                        // Re-enable the entire day/night linked sight relationship.
                        EnsureLinkedSightAvailability(day_optic.slot);

                        //MelonLogger.Msg($"[LEO2K] Day optic enabled: {day_optic.enabled}");
                        //MelonLogger.Msg($"[LEO2K] Day slot enabled: {day_optic.slot.enabled}");
                        //MelonLogger.Msg($"[LEO2K] Night slot: {night_slot}");
                        //MelonLogger.Msg($"[LEO2K] Night slot enabled: {night_slot?.enabled}");
                        //MelonLogger.Msg($"[LEO2K] Night optic enabled: {night_slot?.PairedOptic?.enabled}");
                        //MelonLogger.Msg($"[LEO2K] Night slot active: {night_slot?.IsActive}");
                        //MelonLogger.Msg($"[LEO2K] NightSightAtNightOnly: {night_slot?.NightSightAtNightOnly}");
                    }

                    if (vic.FriendlyName != "Leopard 2K")
                    {
                        if (day_optic == null)
                        {
                            MelonLogger.Warning($"[LEO2K LRF] Could not find day optic for {vic.FriendlyName}.");
                        }
                        else
                        {
                            GameObject lase = new GameObject("lase");
                            lase.transform.SetParent(day_optic.transform, false);
                            lase.transform.localPosition = new Vector3(0f, 0f, 0.3f);
                            lase.transform.localRotation = Quaternion.identity;

                            //// Temporary visible guide for debugging the LRF origin.
                            //GameObject laserMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            //laserMarker.name = "LEO2K_LaserOrigin_DebugMarker";
                            //laserMarker.transform.SetParent(lase.transform, false);
                            //laserMarker.transform.localPosition = Vector3.zero;
                            //laserMarker.transform.localScale = Vector3.one * 0.025f;

                            //Collider markerCollider = laserMarker.GetComponent<Collider>();
                            //if (markerCollider != null)
                            //    UnityEngine.Object.Destroy(markerCollider);

                            //GameObject laserGuide = new GameObject("LEO2K_LaserAxis_DebugGuide");
                            //laserGuide.transform.SetParent(lase.transform, false);

                            //LineRenderer line = laserGuide.AddComponent<LineRenderer>();
                            //line.useWorldSpace = false;
                            //line.positionCount = 2;
                            //line.SetPosition(0, Vector3.zero);
                            //line.SetPosition(1, Vector3.forward * 3f);
                            //line.startWidth = 0.006f;
                            //line.endWidth = 0.002f;
                            //line.startColor = new Color(1f, 0.05f, 0.05f, 1f);
                            //line.endColor = new Color(1f, 0.05f, 0.05f, 0.2f);

                            //Shader debugShader = Shader.Find("Sprites/Default");
                            //if (debugShader != null)
                            //    line.material = new Material(debugShader);

                            fcs.LaserOrigin = lase.transform;
                            fcs.LaserAim = LaserAimMode.ImpactPoint;
                            fcs.MaxLaserRange = 4000f;
                            fcs.IgnoreHorizontalForFireGating = false;
                            fcs.IgnoreRangeBelow = 200f;
                            fcs.OpticalRangefinder = null;

                            if (night_optic != null)
                            {
                                RH202LaserPointCorrection laserFix = fcs.GetComponent<RH202LaserPointCorrection>();

                                if (laserFix == null)
                                    laserFix = fcs.gameObject.AddComponent<RH202LaserPointCorrection>();

                                laserFix.laser = lase.transform;
                                laserFix.dayOptic = day_optic;
                                laserFix.nightOptic = night_optic;

                                //MelonLogger.Msg($"[LEO2K LRF] Optic-following laser correction configured for {vic.FriendlyName}. " + $"Day={day_optic.name}, Night={night_optic.name}.");
                            }
                            else
                            {
                                MelonLogger.Warning($"[LEO2K LRF] No linked night optic found for {vic.FriendlyName}; " + "laser correction is using the day optic only.");
                            }
                        }
                    }

                    switch (vic.FriendlyName)
                    {
                        case "Leopard 2K":
                            day_optic.RotateAzimuth = false;
                            fcs.FireGateAngle = 0.25f;
                            break;

                        case "Leopard 2KA1 'Vogel'":
                            day_optic.RotateAzimuth = true;
                            day_optic.slot.BaseBlur = 0f;
                            day_optic.slot.VibrationBlurScale = 0.10f;
                            day_optic.slot.VibrationShakeMultiplier = 0.02f;
                            day_optic.slot.VibrationPreBlur = true;
                            fcs.FireGateAngle = 0.05f;
                            fcs._leadUpdatesLocked = false;
                            fcs._horizontalLeadOnly = true;
                            fcs._useSeparateLead = true;
                            fcs.DynamicLead = true;
                            //fcs._fixParallaxForVectorMode = false;
                            fcs.PointStabAffectsLead = true;
                            fcs.PointStabIncludesVertical = false;
                            fcs.InertiaLeadIncludesVertical = true;
                            fcs.ImperfectFixedRangePointStab = true;
                            fcs.CurrentStabMode = StabilizationMode.WorldPoint;
                            day_optic.slot.LinkedNightSight.PairedOptic.RotateAzimuth = true;
                            day_optic.slot.LinkedNightSight.PairedOptic.Alignment = OpticAlignment.BoresightStabilized;
                            LEO2KThermals.Add(day_optic.slot.LinkedNightSight.PairedOptic, LEO2KThermals.ThermalVariant.Leopard2K1);
                            break;

                        case "Leopard 2KA2 'Falke'":
                            day_optic.RotateAzimuth = true;
                            day_optic.slot.BaseBlur = 0f;
                            day_optic.slot.VibrationBlurScale = 0.05f;
                            day_optic.slot.VibrationShakeMultiplier = 0.005f;
                            day_optic.slot.VibrationPreBlur = true;
                            fcs.FireGateAngle = 0.05f;
                            fcs._leadUpdatesLocked = false;
                            fcs._horizontalLeadOnly = false;
                            fcs._useSeparateLead = false;
                            fcs.DynamicLead = true;
                            //fcs._fixParallaxForVectorMode = false;
                            fcs.PointStabAffectsLead = true;
                            fcs.PointStabIncludesVertical = true;
                            fcs.InertiaLeadIncludesVertical = true;
                            fcs.ImperfectFixedRangePointStab = true;
                            fcs.CurrentStabMode = StabilizationMode.WorldPoint;
                            day_optic.Alignment = OpticAlignment.BoresightStabilized;
                            day_optic.slot.LinkedNightSight.PairedOptic.RotateAzimuth = true;
                            day_optic.slot.LinkedNightSight.PairedOptic.Alignment = OpticAlignment.BoresightStabilized;
                            LEO2KThermals.Add(day_optic.slot.LinkedNightSight.PairedOptic, LEO2KThermals.ThermalVariant.Leopard2K2);
                            break;

                        case "Leopard 2KA3 'Adler'":
                            day_optic.RotateAzimuth = true;
                            day_optic.slot.BaseBlur = 0f;
                            day_optic.slot.VibrationBlurScale = 0f;
                            day_optic.slot.VibrationShakeMultiplier = 0f;
                            day_optic.slot.VibrationPreBlur = true;
                            fcs.FireGateAngle = 0.05f;
                            fcs._leadUpdatesLocked = false;
                            fcs._horizontalLeadOnly = false;
                            fcs._useSeparateLead = false;
                            fcs.DynamicLead = true;
                            //fcs._fixParallaxForVectorMode = false;
                            fcs.PointStabAffectsLead = true;
                            fcs.PointStabIncludesVertical = true;
                            fcs.InertiaLeadIncludesVertical = true;
                            fcs.InertialCompensation = true;
                            fcs.ImperfectFixedRangePointStab = true;
                            fcs.CurrentStabMode = StabilizationMode.WorldPoint;
                            day_optic.Alignment = OpticAlignment.BoresightStabilized;
                            day_optic.slot.LinkedNightSight.PairedOptic.RotateAzimuth = true;
                            day_optic.slot.LinkedNightSight.PairedOptic.Alignment = OpticAlignment.BoresightStabilized;
                            LEO2KThermals.Add(day_optic.slot.LinkedNightSight.PairedOptic, LEO2KThermals.ThermalVariant.Leopard2K3);
                            break;
                    }
                    //MelonLogger.Msg("[LEO2K] Laser rangefinder added");
                }

                // Disable unecessary 1A4 items
                GHPC.Equipment.Lamps.LampType lamp_type = GHPC.Equipment.Lamps.LampType.GunnerSpotlight;
                EquipmentManager equipment_manager = vic._equipmentManager;
                equipment_manager.lampsDict.Remove(lamp_type);
                equipment_manager.AllLamps = equipment_manager.AllLamps.Where(o => o.Type != lamp_type).ToArray();
                string[] objectsToDisable =
                {
    "LEO1A1A1_rig/HULL/1A3 hull net",
    "LEO1A3_mesh/A3 turret",
    "LEO1A1A1_rig/HULL/TURRET/1A3 turret net",
    "LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/1A3 mantlet net",
    "LEO1A1A1_rig/HULL/TURRET/lafette002",
    "LEO1A1A1_rig/HULL/TURRET/hole blocker turret",
    "hole blocker hull",
    "LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/searchlight closed",
    "LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/searchlight open",
    "LEO1A3_mesh/1a3_searchlight",
    "LEO1_electrics",
    "LEO1A3_mesh/PERI R12",
    "LEO1A3_mesh/tools001",
    "LEO1A3_mesh/TRP2",
    "LEO1A3_mesh/ir sight a3",
    "LEO1A3_mesh/a3_flaps",
    "LEO1A3_mesh/a3_flaps_folded",
    "LEO1A3_mesh/a3_skirt_cut",
    "LEO1A3_mesh/a3_skirt_full",
    "LEO1A3_mesh/1A3_hull",
    "LEO1A1A1_rig/antenna socket right",
    "LEO1A1A1_rig/antenna socket left",
    "LEO1A1A1_rig/HULL/TURRET/numbers",
    "LEO1A1A1_rig/HULL/TURRET/antennas",
    "LEO1A1A1_rig/HULL/TURRET/kreuz",
    "LEO1A3_markings",
    "--Electrics--",
            };

                foreach (string path in objectsToDisable)
                {
                    Transform obj = vic.transform.Find(path);

                    if (obj != null)
                    {
                        obj.gameObject.SetActive(false);
                    }
                }
                Transform guneffects = vic_go.transform.Find("LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/--Gun Scripts--/105mm Gun L7A3 L52/GameObject");
                if (guneffects != null)
                {
                    guneffects.localPosition = new Vector3(0f, 0f, 0.175f);
                }
                Transform leftexhaust = vic_go.transform.Find("Particles/Exhaust Leopard 1 L");
                if (leftexhaust != null)
                {
                    leftexhaust.localPosition = new Vector3(-0.5f, 2.526f, -3.772f);
                }
                Transform rightexhaust = vic_go.transform.Find("Particles/Exhaust Leopard 1 R");
                if (rightexhaust != null)
                {
                    rightexhaust.localPosition = new Vector3(0.5f, 2.526f, -3.772f);
                }
                Transform gunNet = vic_go.transform.Find("LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/1A3 muzzle/1A3  gun net");
                if (gunNet != null)
                {
                    gunNet.localPosition = new Vector3(0f, -1.8441f, -5.35f);
                    gunNet.localScale = new Vector3(1.2f, 1.05f, 1f);
                }
                Transform bustleNet = vic_go.transform.Find("LEO1A1A1_rig/HULL/TURRET/camo nets");
                if (bustleNet != null)
                {
                    bustleNet.localPosition = new Vector3(0.1f, -1.5f, -0.7f);
                }
                //Transform antenna1 = vic_go.transform.Find("LEO1A1A1_rig/HULL/TURRET/antennas/SEM25");
                //Transform antenna2 = vic_go.transform.Find("LEO1A1A1_rig/HULL/TURRET/antennas/SEM25 (1)");
                //if (antenna1 != null)
                //{
                //    antenna1.localPosition = new Vector3 (-0.775f, 2.265f, -2.24f);
                //}
                //if (antenna2 != null)
                //{
                //    antenna2.localPosition = new Vector3 (0.775f, 2.265f, -2.24f);
                //}
                Transform maingun = vic_go.transform.Find("LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/--Gun Scripts--/105mm Gun L7A3 L52");
                switch (vic.FriendlyName)
                {
                    case "Leopard 2K":
                        {
                            AmmoFeed feed = maingun.GetComponent<AmmoFeed>();
                            if (feed != null)
                            {
                                feed._totalReloadTime = 5f;
                                feed.SlowReloadMultiplier = 3.5f;
                            }

                            ReloadAzimuthLock reloadLock = maingun.GetComponent<ReloadAzimuthLock>() ?? maingun.gameObject.AddComponent<ReloadAzimuthLock>();

                            reloadLock.ReloadStageIndexStart = 0;
                            reloadLock.ReloadStageIndexEnd = 0;
                            reloadLock.LockDelay = 0f;
                            reloadLock.PauseStageUntilLockReached = false;

                            reloadLock.Lock.PitchLockType = AzimuthLock.LockType.MoveToValue;
                            reloadLock.Lock.PitchLockValue = 5f;
                            reloadLock.Lock.YawLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.RollLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.DetachPlatform = true;
                            //MelonLogger.Msg($"[LEO2K] Reload lock configured: Start={reloadLock.ReloadStageIndexStart}, End={reloadLock.ReloadStageIndexEnd}, Pause={reloadLock.PauseStageUntilLockReached}");
                        }
                        break;

                    case "Leopard 2KA1 'Vogel'":
                        {
                            AmmoFeed feed = maingun.GetComponent<AmmoFeed>();
                            if (feed != null)
                            {
                                feed._totalReloadTime = 5.1f;
                                feed.SlowReloadMultiplier = 3.5f;
                            }

                            ReloadAzimuthLock reloadLock = maingun.GetComponent<ReloadAzimuthLock>() ?? maingun.gameObject.AddComponent<ReloadAzimuthLock>();

                            reloadLock.ReloadStageIndexStart = 0;
                            reloadLock.ReloadStageIndexEnd = 0;
                            reloadLock.LockDelay = 0f;
                            reloadLock.PauseStageUntilLockReached = false;

                            reloadLock.Lock.PitchLockType = AzimuthLock.LockType.MoveToValue;
                            reloadLock.Lock.PitchLockValue = 5f;
                            reloadLock.Lock.YawLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.RollLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.DetachPlatform = true;
                        }
                        break;

                    case "Leopard 2KA2 'Falke'":
                        {
                            AmmoFeed feed = maingun.GetComponent<AmmoFeed>();
                            if (feed != null)
                            {
                                feed._totalReloadTime = 5.35f;
                                feed.SlowReloadMultiplier = 3.5f;
                            }

                            ReloadAzimuthLock reloadLock = maingun.GetComponent<ReloadAzimuthLock>() ?? maingun.gameObject.AddComponent<ReloadAzimuthLock>();

                            reloadLock.ReloadStageIndexStart = 0;
                            reloadLock.ReloadStageIndexEnd = 0;
                            reloadLock.LockDelay = 0f;
                            reloadLock.PauseStageUntilLockReached = false;

                            reloadLock.Lock.PitchLockType = AzimuthLock.LockType.MoveToValue;
                            reloadLock.Lock.PitchLockValue = 5f;
                            reloadLock.Lock.YawLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.RollLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.DetachPlatform = true;
                        }
                        break;
                    case "Leopard 2KA3 'Adler'":
                        {
                            AmmoFeed feed = maingun.GetComponent<AmmoFeed>();
                            if (feed != null)
                            {
                                feed._totalReloadTime = 5.6f;
                                feed.SlowReloadMultiplier = 3.5f;
                            }

                            ReloadAzimuthLock reloadLock = maingun.GetComponent<ReloadAzimuthLock>() ?? maingun.gameObject.AddComponent<ReloadAzimuthLock>();

                            reloadLock.ReloadStageIndexStart = 0;
                            reloadLock.ReloadStageIndexEnd = 0;
                            reloadLock.LockDelay = 0f;
                            reloadLock.PauseStageUntilLockReached = false;

                            reloadLock.Lock.PitchLockType = AzimuthLock.LockType.MoveToValue;
                            reloadLock.Lock.PitchLockValue = 5f;
                            reloadLock.Lock.YawLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.RollLockType = AzimuthLock.LockType.None;
                            reloadLock.Lock.DetachPlatform = true;
                        }
                        break;
                }

                Transform reloadAudio = vic_go.transform.Find("LEO1A1A1_rig/HULL/TURRET/1A3 mantlet/--Gun Scripts--/main gun reload audio");

                if (reloadAudio != null)
                {
                    Component sourceAudio = reloadAudio.GetComponent("AudioSource");
                    AmmoFeed reloadFeed = maingun.GetComponent<AmmoFeed>();

                    if (sourceAudio != null && reloadFeed != null)
                    {
                        PropertyInfo timeProperty = sourceAudio.GetType().GetProperty("time");
                        PropertyInfo isPlayingProperty = sourceAudio.GetType().GetProperty("isPlaying");

                        if (timeProperty != null && isPlayingProperty != null)
                        {
                            float audioStartTime = 0f;

                            switch (vic.FriendlyName)
                            {
                                case "Leopard 2K":
                                    audioStartTime = 1f;
                                    break;

                                case "Leopard 2KA1 'Vogel'":
                                    audioStartTime = 0.9f;
                                    break;

                                case "Leopard 2KA2 'Falke'":
                                    audioStartTime = 0.65f;
                                    break;

                                case "Leopard 2KA3 'Adler'":
                                    audioStartTime = 0.4f;
                                    break;
                            }

                            reloadFeed.ReloadStageStarted += index =>
                            {
                                if (index == 0 && audioStartTime > 0f)
                                    MelonCoroutines.Start(SetReloadAudioTime(sourceAudio, timeProperty, isPlayingProperty, audioStartTime));
                            };

                            //MelonLogger.Msg($"[LEO2K] Reload audio start time set to {audioStartTime:F2}s for {vic.FriendlyName}");
                        }
                        else
                        {
                            MelonLogger.Warning("[LEO2K] Could not access AudioSource time/isPlaying properties.");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Reload audio or AmmoFeed component missing.");
                    }
                }
                else
                {
                    MelonLogger.Warning("[LEO2K] Could not find main gun reload audio object.");
                }

                LateFollow hullArmourFollow = null;

                foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
                {
                    if (follow == null || !follow.gameObject.scene.IsValid() || follow.gameObject.name != "HULL ARMOR")
                        continue;

                    Component parentUnit = follow.ParentUnit as Component;
                    Transform originalParent = follow.OriginalParent;

                    bool belongsToThisVehicle = parentUnit == vic || originalParent == vic.transform || (parentUnit != null && parentUnit.gameObject == vic.gameObject) || (originalParent != null && originalParent.IsChildOf(vic.transform));

                    if (!belongsToThisVehicle)
                        continue;

                    hullArmourFollow = follow;
                    break;
                }

                LateFollow turretArmourFollow = null;

                foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
                {
                    if (follow == null || !follow.gameObject.scene.IsValid() || follow.gameObject.name != "TURRET ARMOR")
                        continue;

                    Component parentUnit = follow.ParentUnit as Component;
                    Transform originalParent = follow.OriginalParent;

                    bool belongsToThisVehicle = parentUnit == vic || originalParent == vic.transform || (parentUnit != null && parentUnit.gameObject == vic.gameObject) || (originalParent != null && originalParent.IsChildOf(vic.transform));

                    if (!belongsToThisVehicle)
                        continue;

                    turretArmourFollow = follow;
                    break;
                }

                LateFollow turretFloorArmourFollow = null;

                foreach (LateFollow follow in Resources.FindObjectsOfTypeAll<LateFollow>())
                {
                    if (follow == null || !follow.gameObject.scene.IsValid() || follow.gameObject.name != "TURRET FLOOR ARMOR")
                        continue;

                    Component parentUnit = follow.ParentUnit as Component;
                    Transform originalParent = follow.OriginalParent;

                    bool belongsToThisVehicle = parentUnit == vic || originalParent == vic.transform || (parentUnit != null && parentUnit.gameObject == vic.gameObject) || (originalParent != null && originalParent.IsChildOf(vic.transform));

                    if (!belongsToThisVehicle)
                        continue;

                    turretFloorArmourFollow = follow;
                    break;
                }

                if (hullArmourFollow != null)
                {
                    Transform hullvisualarmour = hullArmourFollow.transform.Find("LEO1_armour_hull");

                    if (hullvisualarmour != null)
                    {
                        foreach (Transform child in hullvisualarmour.GetComponentsInChildren<Transform>(true))
                        {
                            if (child == hullvisualarmour)
                                continue;

                            if (child.name == "track right" || child.name == "track left")
                                continue;

                            child.gameObject.SetActive(false);
                        }

                        //MelonLogger.Msg($"[LEO2K] Disabled original hull armour on {vic.FriendlyName}.");
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Found HULL ARMOUR LateFollow, but could not find LEO1_armour_hull.");
                    }
                }
                else
                {
                    MelonLogger.Warning("[LEO2K] Could not find HULL ARMOUR LateFollow belonging to this vehicle.");
                }

                if (turretArmourFollow != null)
                {
                    Transform turretvisualarmour = turretArmourFollow.transform.Find("LEO1A3_armour_turret");

                    if (turretvisualarmour != null)
                    {
                        turretvisualarmour.gameObject.SetActive(false);
                        //MelonLogger.Msg($"[LEO2K] Disabled original turret armour on {vic.FriendlyName}.");
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Found TURRET ARMOUR LateFollow, but could not find LEO1A3_armour_turret.");
                    }
                }
                else
                {
                    MelonLogger.Warning("[LEO2K] Could not find TURRET ARMOUR LateFollow belonging to this vehicle.");
                }
                GameObject daynighttog = vic.gameObject.transform.Find("LEO1A1A1_rig/HULL/TURRET/--Turret Scripts--/DayNight toggle").gameObject;
                GameObject.Destroy(daynighttog);
                Transform tacfront = visual_hull.Find("tac marker front");
                Transform platefront = visual_hull.Find("license plate");
                Transform kreuzr = visual_turret.Find("kreuz right");
                Transform kreuzl = visual_turret.Find("kreuz left");
                Transform smokes = visual_turret.Find("Smokes");
                Transform tacnumr = visual_turret.Find("tac numbers right");
                Transform tacnuml = visual_turret.Find("tac numbers left");
                Transform boxr = visual_hull.Find("boxes right");
                Transform boxl = visual_hull.Find("boxes left");
                Transform MCL = visual_hull.Find("MCL marker front");

                AimablePlatform mount = fcs.Mounts[1];

                mount.ParentPlatform = fcs.Mounts[0];

                switch (vic.FriendlyName)
                {
                    case "Leopard 2KA1 'Vogel'":
                        boxr.gameObject.SetActive(false);
                        boxl.gameObject.SetActive(false);
                        MCL.gameObject.SetActive(false);
                        L2KA1visualhull.gameObject.SetActive(true);
                        break;

                    case "Leopard 2KA2 'Falke'":
                        MCL.gameObject.SetActive(false);
                        L2KA2visualhull.gameObject.SetActive(true);
                        L2KA2visualturret.gameObject.SetActive(true);
                        L2KA2visualmantlet.gameObject.SetActive(true);
                        Transform aarTransformk2hull = L2KA2visualhull.transform.Find("AAR");
                        if (aarTransformk2hull != null)
                        {
                            AarVisual aar = aarTransformk2hull.GetComponent<AarVisual>() ?? aarTransformk2hull.gameObject.AddComponent<AarVisual>();

                            aar._renderers.Clear();
                            aar._renderers.AddRange(aarTransformk2hull.GetComponentsInChildren<MeshRenderer>(true));

                            aar.OriginalMaterials.Clear();
                            aar.InitialiseRenderers();

                            aar.RenderMode = AarVisual.AarVisualRenderMode.HighlightedOnly;
                            aar.SwitchMaterials = false;
                            aar.UpdateVisibility();
                        }
                        Transform aarTransformk2turret = L2KA2visualturret.transform.Find("AAR");
                        if (aarTransformk2turret != null)
                        {
                            AarVisual aar = aarTransformk2turret.GetComponent<AarVisual>() ?? aarTransformk2turret.gameObject.AddComponent<AarVisual>();

                            aar._renderers.Clear();
                            aar._renderers.AddRange(aarTransformk2turret.GetComponentsInChildren<MeshRenderer>(true));

                            aar.OriginalMaterials.Clear();
                            aar.InitialiseRenderers();

                            aar.RenderMode = AarVisual.AarVisualRenderMode.HighlightedOnly;
                            aar.SwitchMaterials = false;
                            aar.UpdateVisibility();
                        }
                        Transform aarTransform2kmantlet = L2KA2visualmantlet.transform.Find("AAR");
                        if (aarTransform2kmantlet != null)
                        {
                            AarVisual aar = aarTransform2kmantlet.GetComponent<AarVisual>() ?? aarTransform2kmantlet.gameObject.AddComponent<AarVisual>();

                            aar._renderers.Clear();
                            aar._renderers.AddRange(aarTransform2kmantlet.GetComponentsInChildren<MeshRenderer>(true));

                            aar.OriginalMaterials.Clear();
                            aar.InitialiseRenderers();

                            aar.RenderMode = AarVisual.AarVisualRenderMode.HighlightedOnly;
                            aar.SwitchMaterials = false;
                            aar.UpdateVisibility();
                        }
                        break;

                    case "Leopard 2KA3 'Adler'":
                        MCL.gameObject.SetActive(false);
                        tacfront.gameObject.SetActive(false);
                        platefront.gameObject.SetActive(false);
                        kreuzl.gameObject.SetActive(false);
                        kreuzr.gameObject.SetActive(false);
                        tacnuml.gameObject.SetActive(false);
                        tacnumr.gameObject.SetActive(false);
                        L2KA3visualhull.gameObject.SetActive(true);
                        L2KA3visualturret.gameObject.SetActive(true);
                        L2KA3visualmantlet.gameObject.SetActive(true);
                        smokes.localPosition = new Vector3(0f, 0.002f, 0.001f);
                        smokes.localScale = new Vector3(0.9f, 1f, 1f);
                        Transform aarTransform = L2KA3visualhull.transform.Find("AAR");
                        if (aarTransform != null)
                        {
                            AarVisual aar = aarTransform.GetComponent<AarVisual>() ?? aarTransform.gameObject.AddComponent<AarVisual>();

                            aar._renderers.Clear();
                            aar._renderers.AddRange(aarTransform.GetComponentsInChildren<MeshRenderer>(true));

                            aar.OriginalMaterials.Clear();
                            aar.InitialiseRenderers();

                            aar.RenderMode = AarVisual.AarVisualRenderMode.HighlightedOnly;
                            aar.SwitchMaterials = false;
                            aar.UpdateVisibility();
                        }
                        Transform aarTransform2 = L2KA3visualturret.transform.Find("AAR");
                        if (aarTransform2 != null)
                        {
                            AarVisual aar = aarTransform2.GetComponent<AarVisual>() ?? aarTransform2.gameObject.AddComponent<AarVisual>();

                            aar._renderers.Clear();
                            aar._renderers.AddRange(aarTransform2.GetComponentsInChildren<MeshRenderer>(true));

                            aar.OriginalMaterials.Clear();
                            aar.InitialiseRenderers();

                            aar.RenderMode = AarVisual.AarVisualRenderMode.HighlightedOnly;
                            aar.SwitchMaterials = false;
                            aar.UpdateVisibility();
                        }
                        Transform aarTransform3 = L2KA3visualmantlet.transform.Find("AAR");
                        if (aarTransform3 != null)
                        {
                            AarVisual aar = aarTransform3.GetComponent<AarVisual>() ?? aarTransform3.gameObject.AddComponent<AarVisual>();

                            aar._renderers.Clear();
                            aar._renderers.AddRange(aarTransform3.GetComponentsInChildren<MeshRenderer>(true));

                            aar.OriginalMaterials.Clear();
                            aar.InitialiseRenderers();

                            aar.RenderMode = AarVisual.AarVisualRenderMode.HighlightedOnly;
                            aar.SwitchMaterials = false;
                            aar.UpdateVisibility();
                        }
                        break;
                }

                VehicleSmokeManager smoke_manager = vic.GetComponentInChildren<VehicleSmokeManager>();

                if (smoke_manager != null)
                {

                    if (smokes != null)
                    {
                        List<Transform> new_smokes = new List<Transform>();

                        for (int i = 1; i <= 4; i++)
                        {
                            Transform smoke = smokes.Find("smoke_r_" + i);

                            if (smoke != null) new_smokes.Add(smoke);
                        }

                        for (int i = 1; i <= 4; i++)
                        {
                            Transform smoke = smokes.Find("smoke_l_" + i);

                            if (smoke != null) new_smokes.Add(smoke);
                        }

                        for (int i = 0; i < new_smokes.Count; i++)
                        {
                            VehicleSmokeManager.SmokeSlot slot = smoke_manager._smokeSlots[i];

                            Transform smoke = new_smokes[i];

                            slot.DisplayBone = smoke;

                            if (slot.SpawnLocation != null)
                            {
                                slot.SpawnLocation.transform.SetParent(smoke);
                                slot.SpawnLocation.transform.position = smoke.GetComponent<Renderer>().bounds.center;
                            }
                        }
                    }
                }

                if (vic.FriendlyName is ("Leopard 2K"))
                {
                    VehicleController this_vic_controller = vic_go.GetComponent<VehicleController>();
                    NwhChassis chassis = vic_go.GetComponent<NwhChassis>();
                    var transmission = vic.VehicleController.transmission;

                    this_vic_controller.engine.maxPower = 1000f;
                    this_vic_controller.engine.maxRPM = 2600f;
                    chassis._maxForwardSpeed = 21f;
                    chassis._maxReverseSpeed = 20f;
                    chassis._originalEnginePower = 1000f;

                    this_vic_controller.tracks.turnSpeedLimit = 0.576f;
                    this_vic_controller.vehicleRigidbody.maxAngularVelocity = 0.576f;
                    transmission.reverseThrottleLimit = 1.0f;

                    transmission.gears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f,
                    0f,
                    4.504f,
                    2.209f,
                    1.523f,
                    1.003f
                    };
                    transmission.forwardGears = new List<float>
                    {
                    4.64f,
                    2.60f,
                    1.67f,
                    1.00f,
                    };

                    transmission.reverseGears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f
                    };

                    Transform turretScripts = vic_go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "--Turret Scripts--");

                    if (turretScripts != null)
                    {
                        AimablePlatform turretPlatform = turretScripts.GetComponent<AimablePlatform>();

                        if (turretPlatform != null)
                        {
                            turretPlatform.SpeedPowered = 40f;
                            turretPlatform.SpeedUnpowered = 10f;

                            //MelonLogger.Msg($"[LEO2K] Turret AimablePlatform found: " + $"Powered={turretPlatform.SpeedPowered} | " + $"Unpowered={turretPlatform.SpeedUnpowered} | " + $"Desirable={turretPlatform._desirableSpeed}");
                        }
                        else
                        {
                            MelonLogger.Warning("[LEO2K] --Turret Scripts-- has no AimablePlatform!");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Could not find --Turret Scripts--!");
                    }
                }
                if (vic.FriendlyName is ("Leopard 2KA1 'Vogel'"))
                {
                    VehicleController this_vic_controller = vic_go.GetComponent<VehicleController>();
                    NwhChassis chassis = vic_go.GetComponent<NwhChassis>();
                    var transmission = vic.VehicleController.transmission;

                    this_vic_controller.engine.maxPower = 1000f;
                    this_vic_controller.engine.maxRPM = 2600f;
                    chassis._maxForwardSpeed = 21f;
                    chassis._maxReverseSpeed = 20f;
                    chassis._originalEnginePower = 1000f;

                    this_vic_controller.tracks.turnSpeedLimit = 0.576f;
                    this_vic_controller.vehicleRigidbody.maxAngularVelocity = 0.576f;
                    transmission.reverseThrottleLimit = 1.0f;

                    transmission.gears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f,
                    0f,
                    4.504f,
                    2.209f,
                    1.523f,
                    1.003f
                    };
                    transmission.forwardGears = new List<float>
                    {
                    4.64f,
                    2.60f,
                    1.67f,
                    1.00f,
                    };

                    transmission.reverseGears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f
                    };

                    Transform turretScripts = vic_go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "--Turret Scripts--");

                    if (turretScripts != null)
                    {
                        AimablePlatform turretPlatform = turretScripts.GetComponent<AimablePlatform>();

                        if (turretPlatform != null)
                        {
                            turretPlatform.SpeedPowered = 40f;
                            turretPlatform.SpeedUnpowered = 10f;

                            //MelonLogger.Msg($"[LEO2K1] Turret AimablePlatform found: " + $"Powered={turretPlatform.SpeedPowered} | " + $"Unpowered={turretPlatform.SpeedUnpowered} | " + $"Desirable={turretPlatform._desirableSpeed}");
                        }
                        else
                        {
                            MelonLogger.Warning("[LEO2K1] --Turret Scripts-- has no AimablePlatform!");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K1] Could not find --Turret Scripts--!");
                    }
                }
                if (vic.FriendlyName is ("Leopard 2KA2 'Falke'"))
                {
                    VehicleController this_vic_controller = vic_go.GetComponent<VehicleController>();
                    NwhChassis chassis = vic_go.GetComponent<NwhChassis>();
                    var transmission = vic.VehicleController.transmission;

                    this_vic_controller.engine.maxPower = 911f;
                    this_vic_controller.engine.maxRPM = 2600f;
                    chassis._maxForwardSpeed = 18.2f;
                    chassis._maxReverseSpeed = 18.2f;
                    chassis._originalEnginePower = 911f;

                    this_vic_controller.tracks.turnSpeedLimit = 0.524f;
                    this_vic_controller.vehicleRigidbody.maxAngularVelocity = 0.524f;
                    transmission.reverseThrottleLimit = 1.0f;

                    transmission.gears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f,
                    0f,
                    4.504f,
                    2.209f,
                    1.523f,
                    1.003f
                    };
                    transmission.forwardGears = new List<float>
                    {
                    4.64f,
                    2.60f,
                    1.67f,
                    1.00f,
                    };

                    transmission.reverseGears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f
                    };

                    Transform turretScripts = vic_go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "--Turret Scripts--");

                    if (turretScripts != null)
                    {
                        AimablePlatform turretPlatform = turretScripts.GetComponent<AimablePlatform>();

                        if (turretPlatform != null)
                        {
                            turretPlatform.SpeedPowered = 34f;
                            turretPlatform.SpeedUnpowered = 7f;

                            //MelonLogger.Msg($"[LEO2K2] Turret AimablePlatform found: " + $"Powered={turretPlatform.SpeedPowered} | " + $"Unpowered={turretPlatform.SpeedUnpowered} | " + $"Desirable={turretPlatform._desirableSpeed}");
                        }
                        else
                        {
                            MelonLogger.Warning("[LEO2K2] --Turret Scripts-- has no AimablePlatform!");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K2] Could not find --Turret Scripts--!");
                    }
                }
                if (vic.FriendlyName is ("Leopard 2KA3 'Adler'"))
                {
                    VehicleController this_vic_controller = vic_go.GetComponent<VehicleController>();
                    NwhChassis chassis = vic_go.GetComponent<NwhChassis>();
                    var transmission = vic.VehicleController.transmission;

                    this_vic_controller.engine.maxPower = 818f;
                    this_vic_controller.engine.maxRPM = 2600f;
                    chassis._maxForwardSpeed = 16.17f;
                    chassis._maxReverseSpeed = 16.17f;
                    chassis._originalEnginePower = 818f;

                    this_vic_controller.tracks.turnSpeedLimit = 0.466f;
                    this_vic_controller.vehicleRigidbody.maxAngularVelocity = 0.466f;
                    transmission.reverseThrottleLimit = 1.0f;

                    transmission.gears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f,
                    0f,
                    4.504f,
                    2.209f,
                    1.523f,
                    1.003f
                    };
                    transmission.forwardGears = new List<float>
                    {
                    4.64f,
                    2.60f,
                    1.67f,
                    1.00f,
                    };

                    transmission.reverseGears = new List<float>
                    {
                    -1.003f,
                    -1.523f,
                    -2.209f,
                    -4.504f
                    };

                    Transform turretScripts = vic_go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "--Turret Scripts--");

                    if (turretScripts != null)
                    {
                        AimablePlatform turretPlatform = turretScripts.GetComponent<AimablePlatform>();

                        if (turretPlatform != null)
                        {
                            turretPlatform.SpeedPowered = 28.5f;
                            turretPlatform.SpeedUnpowered = 6f;

                            //MelonLogger.Msg($"[LEO2K3] Turret AimablePlatform found: " + $"Powered={turretPlatform.SpeedPowered} | " + $"Unpowered={turretPlatform.SpeedUnpowered} | " + $"Desirable={turretPlatform._desirableSpeed}");
                        }
                        else
                        {
                            MelonLogger.Warning("[LEO2K3] --Turret Scripts-- has no AimablePlatform!");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K3] Could not find --Turret Scripts--!");
                    }
                }

                // AI changes for 2K variants

                UnitAI ai = vic.GetComponentInChildren<UnitAI>();

                if (ai != null)
                {
                    if (vic.FriendlyName == "Leopard 2K")
                    {
                        ai.LeadType = AimTimeCalculator.AimLeadAdjustmentType.MultiStep;
                        ai.RangefinderType = AimTimeCalculator.RangeFinderType.Coincidence;
                        ai.FiringSpeedLimit = 3.5f;
                        ai.SpotTimeMax = 9f;
                        GunnerAI gunnerAI = vic.GetComponentInChildren<GunnerAI>();
                        if (gunnerAI != null)
                            gunnerAI._sweepSpeed = 10;
                            gunnerAI._sweepAngle = 80f;
                    }
                    else if (vic.FriendlyName == "Leopard 2KA1 'Vogel'")
                    {
                        ai.LeadType = AimTimeCalculator.AimLeadAdjustmentType.MultiStep;
                        ai.RangefinderType = AimTimeCalculator.RangeFinderType.Laser;
                        ai.FiringSpeedLimit = 4f;
                        ai.SpotTimeMax = 7.5f;
                        GunnerAI gunnerAI = vic.GetComponentInChildren<GunnerAI>();
                        if (gunnerAI != null)
                            gunnerAI._sweepSpeed = 15;
                            gunnerAI._sweepAngle = 90f;
                    }
                    else if (vic.FriendlyName == "Leopard 2KA2 'Falke'")
                    {
                        ai.LeadType = AimTimeCalculator.AimLeadAdjustmentType.Automatic;
                        ai.RangefinderType = AimTimeCalculator.RangeFinderType.Laser;
                        ai.FiringSpeedLimit = 4.5f;
                        ai.SpotTimeMax = 6f;
                        GunnerAI gunnerAI = vic.GetComponentInChildren<GunnerAI>();
                        if (gunnerAI != null)
                            gunnerAI._sweepSpeed = 25;
                            gunnerAI._sweepAngle = 105f;
                    }
                    else if (vic.FriendlyName == "Leopard 2KA3 'Adler'")
                    {
                        ai.LeadType = AimTimeCalculator.AimLeadAdjustmentType.Automatic;
                        ai.RangefinderType = AimTimeCalculator.RangeFinderType.Laser;
                        ai.FiringSpeedLimit = 5.5f;
                        ai.SpotTimeMax = 4f;
                        GunnerAI gunnerAI = vic.GetComponentInChildren<GunnerAI>();
                        if (gunnerAI != null)
                            gunnerAI._sweepSpeed = 20;
                            gunnerAI._sweepAngle = 115f;
                    }
                    // Change AI accuracy performance for each variant
                    AIAccuracyModifiers accuracy = ai.AccuracyModifiers;

                    if (vic.FriendlyName == "Leopard 2K")
                    {
                        accuracy.Range.Min = 0.90f;
                        accuracy.Range.Max = 1.10f;
                        accuracy.Range.Target = 1f;
                        accuracy.Range.IncreaseAccuracyPerShot = true;

                        accuracy.Velocity.Min = 0.9f;
                        accuracy.Velocity.Max = 1f;
                        accuracy.Velocity.Target = 1f;
                        accuracy.Velocity.IncreaseAccuracyPerShot = true;

                        accuracy.Angle.MaxDistance = 2500f;
                        accuracy.Angle.MaxRadius = 5f;
                        accuracy.Angle.MinRadius = 3f;
                        accuracy.Angle.IncreaseAccuracyPerShot = false;
                    }
                    else if (vic.FriendlyName == "Leopard 2KA1 'Vogel'")
                    {
                        accuracy.Range.Min = 0.925f;
                        accuracy.Range.Max = 1f;
                        accuracy.Range.Target = 1f;
                        accuracy.Range.IncreaseAccuracyPerShot = false;

                        accuracy.Velocity.Min = 0.925f;
                        accuracy.Velocity.Max = 0.95f;
                        accuracy.Velocity.Target = 1f;
                        accuracy.Velocity.IncreaseAccuracyPerShot = true;

                        accuracy.Angle.MaxDistance = 2850f;
                        accuracy.Angle.MaxRadius = 3.5f;
                        accuracy.Angle.MinRadius = 2f;
                        accuracy.Angle.IncreaseAccuracyPerShot = false;
                    }
                    else if (vic.FriendlyName == "Leopard 2KA2 'Falke'")
                    {
                        accuracy.Range.Min = 1f;
                        accuracy.Range.Max = 1f;
                        accuracy.Range.Target = 1f;
                        accuracy.Range.IncreaseAccuracyPerShot = false;

                        accuracy.Velocity.Min = 0.95f;
                        accuracy.Velocity.Max = 0.975f;
                        accuracy.Velocity.Target = 1.02f;
                        accuracy.Velocity.IncreaseAccuracyPerShot = true;

                        accuracy.Angle.MaxDistance = 3000f;
                        accuracy.Angle.MaxRadius = 2f;
                        accuracy.Angle.MinRadius = 1f;
                        accuracy.Angle.IncreaseAccuracyPerShot = false;
                    }
                    else if (vic.FriendlyName == "Leopard 2KA3 'Adler'")
                    {
                        accuracy.Range.Min = 1f;
                        accuracy.Range.Max = 1f;
                        accuracy.Range.Target = 1f;
                        accuracy.Range.IncreaseAccuracyPerShot = false;

                        accuracy.Velocity.Min = 1f;
                        accuracy.Velocity.Max = 1.02f;
                        accuracy.Velocity.Target = 1f;
                        accuracy.Velocity.IncreaseAccuracyPerShot = true;

                        accuracy.Angle.MaxDistance = 3250f;
                        accuracy.Angle.MaxRadius = 1.75f;
                        accuracy.Angle.MinRadius = 1f;
                        accuracy.Angle.IncreaseAccuracyPerShot = false;
                    }

                    // AI WEAPON USAGE PREFERENCES

                    vic._weaponsManager.Preferences = new WeaponSystemPreference[11];

                    for (int i = 0; i < vic._weaponsManager.Preferences.Length; i++) vic._weaponsManager.Preferences[i] = new WeaponSystemPreference();

                    // Tanks → 120 mm
                    vic._weaponsManager.Preferences[0].RangeBounds = new Vector2(0f, 10000f);
                    vic._weaponsManager.Preferences[0].Role = WeaponSystemRole.MainGun;
                    vic._weaponsManager.Preferences[0].TargetType = TargetShortNameUs.Tank;

                    // AT → 120 mm
                    vic._weaponsManager.Preferences[1].RangeBounds = new Vector2(0f, 2500f);
                    vic._weaponsManager.Preferences[1].Role = WeaponSystemRole.RoofGun;
                    vic._weaponsManager.Preferences[1].TargetType = TargetShortNameUs.Antitank;

                    vic._weaponsManager.Preferences[2].RangeBounds = new Vector2(2500f, 10000f);
                    vic._weaponsManager.Preferences[2].Role = WeaponSystemRole.MainGun;
                    vic._weaponsManager.Preferences[2].TargetType = TargetShortNameUs.Antitank;

                    // APCs / IFVs → 120mm, then RH202
                    vic._weaponsManager.Preferences[3].RangeBounds = new Vector2(0f, 3000f);
                    vic._weaponsManager.Preferences[3].Role = WeaponSystemRole.MainGun;
                    vic._weaponsManager.Preferences[3].TargetType = TargetShortNameUs.Pc;

                    vic._weaponsManager.Preferences[4].RangeBounds = new Vector2(0f, 1500f);
                    vic._weaponsManager.Preferences[4].Role = WeaponSystemRole.RoofGun;
                    vic._weaponsManager.Preferences[4].TargetType = TargetShortNameUs.Pc;

                    // Trucks → RH202, then 120 mm
                    vic._weaponsManager.Preferences[5].RangeBounds = new Vector2(0f, 2500f);
                    vic._weaponsManager.Preferences[5].Role = WeaponSystemRole.RoofGun;
                    vic._weaponsManager.Preferences[5].TargetType = TargetShortNameUs.Truck;

                    vic._weaponsManager.Preferences[6].RangeBounds = new Vector2(2500f, 10000f);
                    vic._weaponsManager.Preferences[6].Role = WeaponSystemRole.MainGun;
                    vic._weaponsManager.Preferences[6].TargetType = TargetShortNameUs.Truck;

                    // Infantry → Coax first, Rh202 at longer ranges
                    vic._weaponsManager.Preferences[7].RangeBounds = new Vector2(0f, 1200f);
                    vic._weaponsManager.Preferences[7].Role = WeaponSystemRole.Coaxial;
                    vic._weaponsManager.Preferences[7].TargetType = TargetShortNameUs.Troops;

                    vic._weaponsManager.Preferences[8].RangeBounds = new Vector2(1200f, 2000f);
                    vic._weaponsManager.Preferences[8].Role = WeaponSystemRole.RoofGun;
                    vic._weaponsManager.Preferences[8].TargetType = TargetShortNameUs.Troops;

                    // Helicopters → RH202, then 120 mm
                    vic._weaponsManager.Preferences[9].RangeBounds = new Vector2(0f, 3000f);
                    vic._weaponsManager.Preferences[9].Role = WeaponSystemRole.RoofGun;
                    vic._weaponsManager.Preferences[9].TargetType = TargetShortNameUs.Chopper;

                    vic._weaponsManager.Preferences[10].RangeBounds = new Vector2(3000f, 10000f);
                    vic._weaponsManager.Preferences[10].Role = WeaponSystemRole.MainGun;
                    vic._weaponsManager.Preferences[10].TargetType = TargetShortNameUs.Chopper;

                    // AI AMMO PREFERENCES 

                    AmmoPreferencesUs84Scriptable leo2kAmmoPref = new AmmoPreferencesUs84Scriptable();
                    leo2kAmmoPref.name = "LEO2K Special Ammo Pref";

                    leo2kAmmoPref.PreferencesDescending = new List<AmmoPreferencesUs84Scriptable.AmmoPrefEntryUs>(6)
                        {
                        new AmmoPreferencesUs84Scriptable.AmmoPrefEntryUs(),
                        new AmmoPreferencesUs84Scriptable.AmmoPrefEntryUs(),
                        new AmmoPreferencesUs84Scriptable.AmmoPrefEntryUs(),
                        new AmmoPreferencesUs84Scriptable.AmmoPrefEntryUs(),
                        new AmmoPreferencesUs84Scriptable.AmmoPrefEntryUs(),
                        new AmmoPreferencesUs84Scriptable.AmmoPrefEntryUs()
                        };

                    // Tanks → 20mm HE first, then 120mm HEAT
                    leo2kAmmoPref.PreferencesDescending[0].TargetName = TargetShortNameUs.Tank;
                    leo2kAmmoPref.PreferencesDescending[0].AmmoPrefsDescending = new List<AmmoType.AmmoShortName>
                    {AmmoType.AmmoShortName.He, AmmoType.AmmoShortName.Heat, AmmoType.AmmoShortName.Ap, AmmoType.AmmoShortName.Sabot};

                    // APCs / IFVs → RH202 AP first
                    leo2kAmmoPref.PreferencesDescending[1].TargetName = TargetShortNameUs.Pc;
                    leo2kAmmoPref.PreferencesDescending[1].AmmoPrefsDescending = new List<AmmoType.AmmoShortName>
                    {AmmoType.AmmoShortName.Ap, AmmoType.AmmoShortName.He, AmmoType.AmmoShortName.Heat, AmmoType.AmmoShortName.Sabot};

                    // Antitank → 20mm HE priority, will also use 120mm HEAT for max damage as a backup
                    leo2kAmmoPref.PreferencesDescending[2].TargetName = TargetShortNameUs.Antitank;
                    leo2kAmmoPref.PreferencesDescending[2].AmmoPrefsDescending = new List<AmmoType.AmmoShortName>
                    {AmmoType.AmmoShortName.He, AmmoType.AmmoShortName.Heat, AmmoType.AmmoShortName.Ap, AmmoType.AmmoShortName.Sabot};

                    // Trucks → RH202 HE first for combat range, 120mm for far ranges.
                    leo2kAmmoPref.PreferencesDescending[3].TargetName = TargetShortNameUs.Truck;
                    leo2kAmmoPref.PreferencesDescending[3].AmmoPrefsDescending = new List<AmmoType.AmmoShortName>
                    {AmmoType.AmmoShortName.He, AmmoType.AmmoShortName.Heat, AmmoType.AmmoShortName.Ap, AmmoType.AmmoShortName.Sabot};

                    // Infantry → Coax first, Rh202 for later ranges
                    leo2kAmmoPref.PreferencesDescending[4].TargetName = TargetShortNameUs.Troops;
                    leo2kAmmoPref.PreferencesDescending[4].AmmoPrefsDescending = new List<AmmoType.AmmoShortName>
                    {AmmoType.AmmoShortName.Coax, AmmoType.AmmoShortName.He, AmmoType.AmmoShortName.Ap};

                    // Helicopters → RH202 AP first
                    leo2kAmmoPref.PreferencesDescending[5].TargetName = TargetShortNameUs.Chopper;
                    leo2kAmmoPref.PreferencesDescending[5].AmmoPrefsDescending = new List<AmmoType.AmmoShortName>
                    {AmmoType.AmmoShortName.Ap, AmmoType.AmmoShortName.He, AmmoType.AmmoShortName.Heat, AmmoType.AmmoShortName.Sabot};

                    vic._weaponsManager.AmmoPreferences = leo2kAmmoPref;

                    // FINAL VISUAL AMMO RACK ADJUSTMENTS - SIZED UP TO 120mm SHELLS

                    if (hullArmourFollow != null)
                    {
                        Transform frontammoRack = hullArmourFollow.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "front_rack");

                        if (frontammoRack != null)
                        {
                            frontammoRack.localPosition = new Vector3(-0.4231f, 0.75f, 1.5502f);

                            if (frontammoRack.GetComponent<LEO2KAmmoVisualScaler>() == null)
                                frontammoRack.gameObject.AddComponent<LEO2KAmmoVisualScaler>();

                            //MelonLogger.Msg("[LEO2K] Configured front_rack ammo visual scaling.");
                        }
                        else
                        {
                            MelonLogger.Warning("[LEO2K] Could not find front_rack under HULL ARMOR.");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Could not find HULL ARMOR LateFollow.");
                    }

                    if (turretFloorArmourFollow != null)
                    {
                        Transform turretRack = turretFloorArmourFollow.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "turret rack");

                        if (turretRack != null)
                        {
                            turretRack.localPosition = new Vector3(-0.851f, 0.6933f, 0.5871f);

                            if (turretRack.GetComponent<LEO2KAmmoVisualScaler>() == null)
                                turretRack.gameObject.AddComponent<LEO2KAmmoVisualScaler>();

                            //MelonLogger.Msg("[LEO2K] Configured turret rack ammo visual scaling.");
                        }
                        else
                        {
                            MelonLogger.Warning("[LEO2K] Could not find turret rack under TURRET FLOOR ARMOR.");
                        }
                    }
                    else
                    {
                        MelonLogger.Warning("[LEO2K] Could not find TURRET FLOOR ARMOR LateFollow.");
                    }
                }
            }
            yield return null;
        }

        private static IEnumerator SetReloadAudioTime(Component audioSource, PropertyInfo timeProperty, PropertyInfo isPlayingProperty, float startTime)
        {
            float timeout = 1f;

            while (timeout > 0f)
            {
                bool isPlaying = (bool)isPlayingProperty.GetValue(audioSource, null);

                if (isPlaying)
                {
                    timeProperty.SetValue(audioSource, startTime, null);
                    yield break;
                }

                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        private static void SetLeo2KAmmoExplosivity(LoadoutManager loadoutManager)
        {
            if (loadoutManager == null)
                return;

            int modified = 0;

            foreach (var rackLoadout in loadoutManager.RackLoadouts)
            {
                GHPC.Weapons.AmmoRack rack = rackLoadout.Rack;

                if (rack == null)
                    continue;

                FlammablesCluster cluster = f_ammoRackFlammables.GetValue(rack) as FlammablesCluster;

                if (cluster == null || cluster.Items == null)
                    continue;

                foreach (FlammableItem item in cluster.Items)
                {
                    if (item == null || !item.Explosive)
                        continue;

                    item._tntEquivalent = LEO2K_AMMO_TNT_EQUIVALENT;
                    modified++;
                }
            }
            //MelonLogger.Msg($"[LEO2K] Set ammo explosivity to " + $"{LEO2K_AMMO_TNT_EQUIVALENT} kg TNT " + $"for {modified} explosive ammo flammables.");
        }

        private static void ApplyLeo2KCamo(GameObject vehicleRoot, string variant)
        {
            if (vehicleRoot == null)
                return;

            bool useOliveDrab = variant == "Leopard 2K" || leo2k_olive_drab.Value;

            if (!useOliveDrab)
            {
                //MelonLogger.Msg($"[LEO2K CAMO] {variant}: standard tricolor camouflage retained.");

                return;
            }

            if (LEO2K_Assets.oliveDrabOverlay == null)
            {
                MelonLogger.Warning($"[LEO2K CAMO] {variant}: olive drab texture is unavailable.");

                return;
            }

            int renderersChecked = 0;
            int changed = 0;

            foreach (Renderer renderer in vehicleRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                renderersChecked++;

                Material[] materials = renderer.materials;

                foreach (Material material in materials)
                {
                    if (material == null)
                        continue;

                    bool materialLooksLikeTricolor = material.name.IndexOf("tricolor", StringComparison.OrdinalIgnoreCase) >= 0 || material.name.IndexOf("camo", StringComparison.OrdinalIgnoreCase) >= 0 || material.name.IndexOf("rough-paper", StringComparison.OrdinalIgnoreCase) >= 0;

                    foreach (string propertyName in material.GetTexturePropertyNames())
                    {
                        Texture texture = material.GetTexture(propertyName);

                        if (texture == null)
                            continue;

                        bool textureLooksLikeTricolor = texture.name.IndexOf("tricolor", StringComparison.OrdinalIgnoreCase) >= 0 || texture.name.IndexOf("rough-paper", StringComparison.OrdinalIgnoreCase) >= 0 || texture.name.IndexOf("camo", StringComparison.OrdinalIgnoreCase) >= 0;

                        if (!materialLooksLikeTricolor && !textureLooksLikeTricolor)
                            continue;

                        material.SetTexture(propertyName, LEO2K_Assets.oliveDrabOverlay);

                        changed++;
                    }
                }

                renderer.materials = materials;
            }

            //MelonLogger.Msg($"[LEO2K CAMO] {variant}: checked {renderersChecked} renderers, " + $"replaced {changed} tricolor texture references.");
        }
        public override void LoadStaticAssets()
        {
            if (!leo2k_patch.Value) return;

            Armour.Init();

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "leo2k_full");

            if (LEO2K_Assets.leo2k_full == null)
            {
                MelonLogger.Error("Could not find leo2k_full inside bundle, please check that your fulda1989 assets are installed!");
                return;
            }

            Util.SetupFLIRShaders(LEO2K_Assets.leo2k_full);
            LEO2K_Assets.leo2k_full.AddComponent<HeatSource>().heat = 0.75f;
            MelonLogger.Msg("Loaded LEO2K asset successfully");

            Transform new_collider = LEO2K_Assets.leo2k_full.transform.Find("leo2k hull collider");
            foreach (MeshCollider hullcollider in new_collider.GetComponentsInChildren<MeshCollider>(true))
            {
                hullcollider.tag = "Penetrable";
                hullcollider.gameObject.layer = LayerMask.NameToLayer("Main Body");
            }

            Transform hull_armour = LEO2K_Assets.leo2k_full.transform.Find("hull/hull armour");
            foreach (MeshCollider collider in hull_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                armor.tag = "Penetrable";
                armor.gameObject.layer = 8;
                armor._canShatterLongRods = true;   
                armor._normalizesHits = true;
                armor.AngleMatters = true;

                if (name is ("front mud flap right"))
                {
                    armor._name = "rubber flap";
                    armor.PrimaryHeatRha = 5f;
                    armor.PrimarySabotRha = 5f;
                }
                else if (name is ("front mud flap left"))
                {
                    armor._name = "rubber flap";
                    armor.PrimaryHeatRha = 5f;
                    armor.PrimarySabotRha = 5f;
                }
                else if (name is ("rear mud flap left"))
                {
                    armor._name = "rubber flap";
                    armor.PrimaryHeatRha = 5f;
                    armor.PrimarySabotRha = 5f;
                }
                else if (name is ("rear mud flap right"))
                {
                    armor._name = "rubber flap";
                    armor.PrimaryHeatRha = 5f;
                    armor.PrimarySabotRha = 5f;
                }
                else if (name is ("driver hatch"))
                {
                    armor._name = "driver hatch";
                    armor.PrimaryHeatRha = 22f;
                    armor.PrimarySabotRha = 22f;
                }
                else if (name is ("hull floor"))
                {
                    armor._name = "hull floor";
                    armor.PrimaryHeatRha = 20f;
                    armor.PrimarySabotRha = 20f;
                }
                else if (name is ("front hull wedges"))
                {
                    armor._name = "front hull wedge";
                    armor.PrimaryHeatRha = 25f;
                    armor.PrimarySabotRha = 25f;
                }
                else if (name is ("left hull side"))
                {
                    armor._name = "hull side";
                    armor.PrimaryHeatRha = 20f;
                    armor.PrimarySabotRha = 20f;
                }
                else if (name is ("right hull side"))
                {
                    armor._name = "hull side";
                    armor.PrimaryHeatRha = 20f;
                    armor.PrimarySabotRha = 20f;
                }
                else if (name is ("lower glacis"))
                {
                    armor._name = "lower front plate";
                    armor.PrimaryHeatRha = 65f;
                    armor.PrimarySabotRha = 65f;
                }
                else if (name is ("lower upper glacis"))
                {
                    armor._name = "upper front plate";
                    armor.PrimaryHeatRha = 63f;
                    armor.PrimarySabotRha = 63f;
                }
                else if (name is ("higher upper glacis"))
                {
                    armor._name = "upper front plate";
                    armor.PrimaryHeatRha = 35f;
                    armor.PrimarySabotRha = 35f;
                }
                else if (name is ("hull roof"))
                {
                    armor._name = "hull roof";
                    armor.PrimaryHeatRha = 15f;
                    armor.PrimarySabotRha = 15f;
                }
                else if (name is ("left upper side"))
                {
                    armor._name = "upper side";
                    armor.PrimaryHeatRha = 22f;
                    armor.PrimarySabotRha = 22f;
                }
                else if (name is ("right upper side"))
                {
                    armor._name = "upper side";
                    armor.PrimaryHeatRha = 22f;
                    armor.PrimarySabotRha = 22f;
                }
                else if (name is ("right skirt front"))
                {
                    armor._name = "side skirt";
                    armor.PrimaryHeatRha = 10f;
                    armor.PrimarySabotRha = 10f;
                }
                else if (name is ("right skirt rear"))
                {
                    armor._name = "side skirt";
                    armor.PrimaryHeatRha = 10f;
                    armor.PrimarySabotRha = 10f;
                }
                else if (name is ("left skirt front"))
                {
                    armor._name = "side skirt";
                    armor.PrimaryHeatRha = 10f;
                    armor.PrimarySabotRha = 10f;
                }
                else if (name is ("left skirt rear"))
                {
                    armor._name = "side skirt";
                    armor.PrimaryHeatRha = 10f;
                    armor.PrimarySabotRha = 10f;
                }
            }
            Transform turret_armour = LEO2K_Assets.leo2k_full.transform.Find("turret/turret armour");
            foreach (MeshCollider collider in turret_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                armor.tag = "Penetrable";
                armor.gameObject.layer = 8;
                armor._canShatterLongRods = true;
                armor._normalizesHits = true;
                armor.AngleMatters = true;

                if (name is ("10mm turret floor"))
                {
                    armor._name = "turret floor";
                    armor.PrimaryHeatRha = 10f;
                    armor.PrimarySabotRha = 10f;
                }
                else if (name is ("12mm rear turret"))
                {
                    armor._name = "turret rear";
                    armor.PrimaryHeatRha = 12f;
                    armor.PrimarySabotRha = 12f;
                }
                else if (name is ("20mm roof"))
                {
                    armor._name = "turret roof";
                    armor.PrimaryHeatRha = 20f;
                    armor.PrimarySabotRha = 20f;
                }
                else if (name is ("15mm commander hatch"))
                {
                    armor._name = "commander hatch";
                    armor.PrimaryHeatRha = 15f;
                    armor.PrimarySabotRha = 15f;
                }
                else if (name is ("15mm loader hatch"))
                {
                    armor._name = "loader hatch";
                    armor.PrimaryHeatRha = 15f;
                    armor.PrimarySabotRha = 15f;
                }
                else if (name is ("50mm mantlet outer"))
                {
                    armor._name = "upper internal mantlet";
                    armor.PrimaryHeatRha = 50f;
                    armor.PrimarySabotRha = 50f;
                }
                else if (name is ("50mm mantlet underneath"))
                {
                    armor._name = "lower internal mantlet";
                    armor.PrimaryHeatRha = 50f;
                    armor.PrimarySabotRha = 50f;
                }
                else if (name is ("80mm left side"))
                {
                    armor._name = "turret side";
                    armor.PrimaryHeatRha = 80;
                    armor.PrimarySabotRha = 80;
                }
                else if (name is ("80mm right side"))
                {
                    armor._name = "turret side";
                    armor.PrimaryHeatRha = 80;
                    armor.PrimarySabotRha = 80;
                }
                else if (name is ("140mm cheek left"))
                {
                    armor._name = "turret cheek";
                    armor.PrimaryHeatRha = 140;
                    armor.PrimarySabotRha = 140;
                }
                else if (name is ("140mm cheek right"))
                {
                    armor._name = "turret cheek";
                    armor.PrimaryHeatRha = 140;
                    armor.PrimarySabotRha = 140;
                }
            }
            Transform mantlet_collider = LEO2K_Assets.leo2k_full.transform.Find("mantlet/mantlet armour");
            foreach (MeshCollider collider in mantlet_collider.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor mantlet_armour = collider.gameObject.AddComponent<UniformArmor>();

                mantlet_armour.tag = "Penetrable";
                mantlet_armour.gameObject.layer = 8;
                mantlet_armour._name = "mantlet";
                mantlet_armour.PrimaryHeatRha = 60f;
                mantlet_armour.PrimarySabotRha = 60f;
                mantlet_armour._canShatterLongRods = true;
                mantlet_armour._normalizesHits = true;
                mantlet_armour.AngleMatters = true;
            }
            Transform rh202_base_collider = LEO2K_Assets.leo2k_full.transform.Find("turret/20MM RIG/RH202_TRAVERSE_PIVOT/20mm_MOUNT/RH202_ELEVATION_PIVOT/20mm_GUN/20mm gun mantlet");
            UniformArmor rh202_base_armour = rh202_base_collider.gameObject.AddComponent<UniformArmor>();

            rh202_base_armour.tag = "Penetrable";
            rh202_base_armour.gameObject.layer = 8;
            rh202_base_armour._name = "rh202 gun";
            rh202_base_armour.PrimaryHeatRha = 25f;
            rh202_base_armour.PrimarySabotRha = 25f;
            rh202_base_armour._canShatterLongRods = true;
            rh202_base_armour._normalizesHits = true;
            rh202_base_armour.AngleMatters = true;

            if (Bundeswehr_120mm_Assets.l44_obj == null)
            {
                MelonLogger.Error("[LEO2K NRE TRACE] Bundeswehr_120mm_Assets.l44_obj is NULL. The L/44 asset has not been assigned when LEO2K.LoadStaticAssets() runs.");
            }
            else
            {
                Transform main_barrel_armour = Bundeswehr_120mm_Assets.l44_obj.transform.Find("Cylinder/barrel armour");

                if (main_barrel_armour == null)
                {
                    MelonLogger.Error("[LEO2K NRE TRACE] L/44 asset exists, but 'Cylinder/barrel armour' was not found.");
                }
                else
                {
                    UniformArmor barrel_armour = main_barrel_armour.gameObject.AddComponent<UniformArmor>();

                    barrel_armour.tag = "Penetrable";
                    barrel_armour.gameObject.layer = 8;
                    barrel_armour._name = "main gun barrel";
                    barrel_armour.PrimaryHeatRha = 20f;
                    barrel_armour.PrimarySabotRha = 20f;
                    barrel_armour._canShatterLongRods = true;
                    barrel_armour._normalizesHits = true;
                    barrel_armour.AngleMatters = true;
                }
            }

            Transform rh202_mount_collider = LEO2K_Assets.leo2k_full.transform.Find("turret/20MM RIG/RH202_TRAVERSE_PIVOT/20mm_MOUNT/20mm gun base");
            UniformArmor rh202_mount_armour_collider = rh202_mount_collider.gameObject.AddComponent<UniformArmor>();

            rh202_mount_armour_collider.tag = "Penetrable";
            rh202_mount_armour_collider.gameObject.layer = 8;
            rh202_mount_armour_collider._name = "rh202 gun base";
            rh202_mount_armour_collider.PrimaryHeatRha = 25f;
            rh202_mount_armour_collider.PrimarySabotRha = 25f;
            rh202_mount_armour_collider._canShatterLongRods = true;
            rh202_mount_armour_collider._normalizesHits = true;
            rh202_mount_armour_collider.AngleMatters = true;

            Transform l2ka1applique = LEO2K_Assets.leo2k_full.transform.Find("hull/l2ka1 hull/l2ka1 hull armour");

            foreach (MeshCollider collider in l2ka1applique.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                string name = collider.gameObject.name;

                armor.tag = "Penetrable";
                armor.gameObject.layer = 8;
                armor._canShatterLongRods = true;
                armor._normalizesHits = true;
                armor.AngleMatters = true;

                if (name is ("LFP Applique"))
                {
                    armor._name = "lower glacis applique";
                    armor.PrimaryHeatRha = 40f;
                    armor.PrimarySabotRha = 40f;
                }
                else if (name is ("UFP Applique"))
                {
                    armor._name = "upper glacis applique";
                    armor.PrimaryHeatRha = 45f;
                    armor.PrimarySabotRha = 45f;
                }
                else if (name is ("Left Applique"))
                {
                    armor._name = "left side applique";
                    armor.PrimaryHeatRha = 25f;
                    armor.PrimarySabotRha = 25f;
                }
                else if (name is ("Right Applique"))
                {
                    armor._name = "right side applique";
                    armor.PrimaryHeatRha = 25f;
                    armor.PrimarySabotRha = 25f;
                }
            }

            Transform hull2ka2_armour = LEO2K_Assets.leo2k_full.transform.Find("hull/l2ka2 hull/l2ka2 hull armour");
            foreach (MeshCollider collider in hull2ka2_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                if (name is ("right") || name is ("left") || name is ("right (1)") || name is ("left (1)") || name is ("left skirt") || name is ("right skirt"))
                {
                    VariableArmor armor = collider.gameObject.AddComponent<VariableArmor>();

                    armor._name = "Eber composite";
                    armor._armorType = Armour.eber;
                    armor.AverageRha = 160f;
                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
                else if (name is ("right skirt slat") || name is ("left skirt slat"))
                {
                    UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                    armor._name = "hull slat";
                    armor.PrimaryHeatRha = 65;
                    armor.PrimarySabotRha = 15;
                    armor._canShatterLongRods = true;
                    armor._normalizesHits = true;
                    armor.AngleMatters = true;
                    armor._isSlatArmor = true;

                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
                else if (name is ("side rubber skirt right") || name is ("side rubber skirt left"))
                {
                    UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                    armor._name = "side skirt rubber";
                    armor.PrimaryHeatRha = 5;
                    armor.PrimarySabotRha = 5;
                    armor._canShatterLongRods = true;
                    armor._normalizesHits = true;
                    armor.AngleMatters = true;

                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
            }

            Transform turret2ka2_armour = LEO2K_Assets.leo2k_full.transform.Find("turret/l2ka2 turret/l2ka2 turret armour");
            foreach (MeshCollider collider in turret2ka2_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                if (name is ("left turret composite") || name is ("right turret composite"))
                {
                    VariableArmor armor = collider.gameObject.AddComponent<VariableArmor>();

                    armor._name = "Eber composite";
                    armor._armorType = Armour.eber;
                    armor.AverageRha = 180f;
                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
            }

            Transform mantlet2ka2_armour = LEO2K_Assets.leo2k_full.transform.Find("mantlet/l2ka2 mantlet/l2ka2 mantlet armour");
            foreach (MeshCollider collider in mantlet2ka2_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                if (name is ("left mantlet") || name is ("right mantlet"))
                {
                    VariableArmor armor = collider.gameObject.AddComponent<VariableArmor>();

                    armor._name = "Eber composite";
                    armor._armorType = Armour.eber;
                    armor.AverageRha = 120f;
                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
            }

            Transform hull2ka3_armour = LEO2K_Assets.leo2k_full.transform.Find("hull/l2ka3 hull/l2ka3 hull armour");
            foreach (MeshCollider collider in hull2ka3_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                if (name is ("UFP") || name is ("LFP") || name is ("Skirt Left") || name is ("Skirt Right"))
                {
                    VariableArmor armor = collider.gameObject.AddComponent<VariableArmor>();

                    armor._name = "Vorschlaghammer composite";
                    armor._armorType = Armour.vorschlaghammer;
                    armor.AverageRha = 225f;
                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
                else if (name is ("side rubber skirt right") || name is ("side rubber skirt left"))
                {
                    UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                    armor._name = "side skirt rubber";
                    armor.PrimaryHeatRha = 5;
                    armor.PrimarySabotRha = 5;
                    armor._canShatterLongRods = true;
                    armor._normalizesHits = true;
                    armor.AngleMatters = true;

                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
                else if (name is ("RU slat") || name is ("RL slat") || name is ("LU slat") || name is ("LL slat") || name is ("R slat"))
                {
                    UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                    armor._name = "hull slat";
                    armor.PrimaryHeatRha = 65;
                    armor.PrimarySabotRha = 15;
                    armor._canShatterLongRods = true;
                    armor._normalizesHits = true;
                    armor.AngleMatters = true;
                    armor._isSlatArmor = true;

                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
            }

            Transform turret2ka3_armour = LEO2K_Assets.leo2k_full.transform.Find("turret/l2ka3 turret composites/l2ka3 turret armour");
            foreach (MeshCollider collider in turret2ka3_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                if (name is ("Turret Left") || name is ("Turret Right") || name is ("left mantlet") || name is ("right mantlet"))
                {
                    VariableArmor armor = collider.gameObject.AddComponent<VariableArmor>();

                    armor._name = "Vorschlaghammer composite";
                    armor._armorType = Armour.vorschlaghammer;
                    armor.AverageRha = 250f;
                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
                else if (name is ("R slat") || name is ("L slat") || name is ("RI slat"))
                {
                    UniformArmor armor = collider.gameObject.AddComponent<UniformArmor>();

                    armor._name = "turret slat";
                    armor.PrimaryHeatRha = 65;
                    armor.PrimarySabotRha = 15;
                    armor._canShatterLongRods = true;
                    armor._normalizesHits = true;
                    armor.AngleMatters = true;
                    armor._isSlatArmor = true;

                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
            }

            Transform mantlet2ka3_armour = LEO2K_Assets.leo2k_full.transform.Find("mantlet/l2ka3 mantlet/l2ka3 mantlet armour");
            foreach (MeshCollider collider in mantlet2ka3_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                string name = collider.gameObject.name;

                if (name is ("lower mantlet") || name is ("upper mantlet"))
                {
                    VariableArmor armor = collider.gameObject.AddComponent<VariableArmor>();

                    armor._name = "Vorschlaghammer composite";
                    armor._armorType = Armour.vorschlaghammer;
                    armor.AverageRha = 150f;
                    armor.tag = "Penetrable";
                    armor.gameObject.layer = 8;
                }
            }
        }
        private static void EnsureLinkedSightAvailability(CameraSlot slot)
        {
            if (slot == null) return;

            try
            {
                if (!slot.enabled) slot.enabled = true;
                if (slot.NightSightAtNightOnly) slot.NightSightAtNightOnly = false;

                var linkedNight = slot.LinkedNightSight;
                if (linkedNight != null)
                {
                    if (!linkedNight.enabled) linkedNight.enabled = true;
                    if (linkedNight.NightSightAtNightOnly) linkedNight.NightSightAtNightOnly = false;
                    if (!linkedNight.IsLinkedNightSight) linkedNight.IsLinkedNightSight = true;
                    if (linkedNight.LinkedDaySight != slot) linkedNight.LinkedDaySight = slot;
                    try { linkedNight.RefreshAvailability(); } catch { }
                }

                var linkedDay = slot.LinkedDaySight;
                if (linkedDay != null)
                {
                    if (!linkedDay.enabled) linkedDay.enabled = true;
                    if (linkedDay.NightSightAtNightOnly) linkedDay.NightSightAtNightOnly = false;
                    if (linkedDay.LinkedNightSight != slot) linkedDay.LinkedNightSight = slot;
                    try { linkedDay.RefreshAvailability(); } catch { }
                }

                try { slot.RefreshAvailability(); } catch { }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Warning($"[LEO2K] Failed to enable linked sight availability: {ex.Message}");
            }
        }
        public static void Init()
        {
            if (gun_l44 == null)
            {
                gun_l44 = ScriptableObject.CreateInstance<WeaponSystemCodexScriptable>();
                gun_l44.name = "gun_l44";
                gun_l44.CaliberMm = 120;
                gun_l44.FriendlyName = "Rh-120 L/44";
                gun_l44.Type = WeaponSystemCodexScriptable.WeaponType.LargeCannon;
            }
            if (!leo2k_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}