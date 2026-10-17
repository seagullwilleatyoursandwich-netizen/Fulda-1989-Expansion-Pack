using GHPC;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Module = ModUtil.Module;

namespace Fulda1989
{
    // BMP2D_Applied bypasses other mods alreadyconverted check, meaning the tank can apply even after being checked as "converted" by mods like PIL.
    public class BMP2D_Applied : MonoBehaviour { }
    public class BMP2D : Module
    {
        static MelonPreferences_Entry<bool> bmp2d_patch;
        static MelonPreferences_Entry<int> bmp2d_chance;

        public static void Config(MelonPreferences_Category cfg)
        {
            bmp2d_patch = cfg.CreateEntry<bool>("BMP-2D Patch", true);
            bmp2d_patch.Comment = "Enables the BMP-2D, which gives the BMP-2 applique side hull and turret armour.";

            bmp2d_chance = cfg.CreateEntry<int>("BMP-2D Spawnrate (%)", 100);
        }

        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;
                //MelonLogger.Msg("Detected vehicle: " + vic.FriendlyName);

                GameObject vic_go = vic.gameObject;
                if (!vic.FriendlyName.Contains("BMP")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                {
                    MelonLogger.Msg("BMP-2D convert skipped " + vic.FriendlyName + " because AlreadyConverted exists!");
                    continue;
                }

                int conversion_chance = 0;

                if (vic.FriendlyName.Contains("BMP-2"))
                {
                    conversion_chance = bmp2d_chance.Value;
                }

                // BMP-2 Guard against other alreadyconverted checks
                if (vic_go.GetComponent<BMP2D_Applied>() != null) continue;

                vic_go.AddComponent<AlreadyConverted>();

                bool converted_to_bmp2d = bmp2d_patch.Value && UnityEngine.Random.Range(0, 100) < conversion_chance;
                if (converted_to_bmp2d)
                {
                    vic_go.AddComponent<BMP2D_Applied>();
                    vic_go.AddComponent<AlreadyConverted>();

                    if (vic.FriendlyName is ("BMP-2") && bmp2d_patch.Value)
                    {
                        vic._friendlyName = "BMP-2D obr.1984";
                    }

                    Transform hull = vic.transform.Find("BMP2_visual/HULL");
                    Transform turret = vic.transform.Find("BMP2_rig/HULL/TURRET");

                    //MelonLogger.Msg("Hull target: " + hull);
                    //MelonLogger.Msg("Turret target: " + turret);

                    GameObject bmp2d_full = GameObject.Instantiate(BMP2D_Assets.bmp2d_full);

                    Transform armour_hull = bmp2d_full.transform.Find("addon sides");
                    Transform armour_turret = bmp2d_full.transform.Find("turret applique");

                    // HULL
                    if (armour_hull != null)
                    {
                        armour_hull.SetParent(hull, false);

                        armour_hull.localPosition = new Vector3(-0.06f, -1.125f, -0.05f);
                        armour_hull.localEulerAngles = new Vector3(0f, 90f, 0f);
                        armour_hull.localScale = new Vector3(1f, 1f, 1f);

                        Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        armour_hull.SetParent(hull_late_follow);
                    }

                    // TURRET
                    if (armour_turret != null)
                    {
                        armour_turret.SetParent(turret, false);

                        armour_turret.localPosition = new Vector3(0f, -0.575f, 0.715f);
                        armour_turret.localEulerAngles = new Vector3(0f, 90f, 0f);
                        armour_turret.localScale = new Vector3(1f, 1f, 1f);

                        Transform turret_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        armour_turret.SetParent(turret_late_follow);
                    }

                    GameObject.Destroy(bmp2d_full);

                    Transform camonet1 = vic.transform.Find("BMP2_rig/HULL/bmp2 net front");
                    if (camonet1 != null)
                    {
                        camonet1.gameObject.SetActive(false);
                    }

                    Transform camonet2 = vic.transform.Find("BMP2_rig/HULL/bmp2 net side");
                    if (camonet2 != null)
                    {
                        camonet2.gameObject.SetActive(false);
                    }

                    Transform camonet3 = vic.transform.Find("BMP2_rig/HULL/TURRET/bmp2 net turret");
                    if (camonet3 != null)
                    {
                        camonet3.gameObject.SetActive(false);
                    }
                    Transform BMP2MEhull = armour_hull.transform.Find("2MD-E hull");
                    Transform BMP2MEturret = armour_turret.transform.Find("2MD-E turret");
                    BMP2MEhull.gameObject.SetActive(false);
                    BMP2MEturret.gameObject.SetActive(false);
                    // BMP-2M Name Check for PIL
                    string BMP2M_type_name = Assembly.CreateQualifiedName("PactIncreasedLethality", "PactIncreasedLethality.BMP2");
                    Type BMP2M_type = Type.GetType(BMP2M_type_name);
                    // check if its enabled
                    if (BMP2M_type != null)
                    {
                        // flags corresponding to the access modifiers of the field
                        BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
                        // get the field
                        FieldInfo field = BMP2M_type.GetField("has_kornets", flags);
                        // get the value held by the field
                        MelonPreferences_Entry<bool> cfg_has_kornets = (MelonPreferences_Entry<bool>)field.GetValue(null);

                        if (cfg_has_kornets != null && cfg_has_kornets.Value)
                        {
                            vic._friendlyName = "BMP-2M-E 'Ekrany'";
                        }
                        if (vic.FriendlyName == "BMP-2M-E 'Ekrany'")
                        {
                            BMP2MEhull.gameObject.SetActive(true);
                            BMP2MEturret.gameObject.SetActive(true);
                        }
                    }
                    ArmyBinocularLookupFix.EnsureSovietEntries(vic.FriendlyName);
                    yield return null;
                }
            }
        }

        [HarmonyPatch]
        public static class PILBMP2CompatibilityPatch
        {
            static readonly Dictionary<Vehicle, Stack<string>> _nameStacks = new Dictionary<Vehicle, Stack<string>>();

            static MethodBase TargetMethod()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.BMP2");
                return type != null
                    ? AccessTools.Method(type, "HandleConversion", new[] { typeof(Vehicle) })
                    : null;
            }

            static bool Prepare()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.BMP2");
                return type != null && AccessTools.Method(type, "HandleConversion", new[] { typeof(Vehicle) }) != null;
            }

            static void Prefix(Vehicle vic)
            {
                if (vic == null || vic.GetComponent<BMP2D_Applied>() == null)
                    return;

                if (!_nameStacks.TryGetValue(vic, out Stack<string> stack))
                {
                    stack = new Stack<string>();
                    _nameStacks[vic] = stack;
                }

                stack.Push(vic._friendlyName);
                MelonLogger.Msg($"[BMP2 PIL COMPAT] PREFIX: {vic._friendlyName} -> BMP-2");

                vic._friendlyName = "BMP-2";
            }

            static void Postfix(Vehicle vic)
            {
                if (vic == null || !_nameStacks.TryGetValue(vic, out Stack<string> stack) || stack.Count == 0)
                    return;

                string originalName = stack.Pop();

                // Nested/repeated HandleConversion call: leave the vehicle as BMP-2
                // until the outermost call finishes.
                if (stack.Count > 0)
                    return;

                if (vic.FriendlyName == "BMP-2M")
                    vic._friendlyName = "BMP-2M-E 'Ekrany'";
                else
                    vic._friendlyName = originalName;

                _nameStacks.Remove(vic);

                // Apply Fulda's smoke launcher adjustments AFTER PIL has created its kit.
                if (vic.FriendlyName == "BMP-2M-E 'Ekrany'")
                {
                    Transform turret = vic.transform.Find("BMP2_rig/HULL/TURRET");

                    if (turret == null)
                    {
                        MelonLogger.Warning("[BMP2 PIL COMPAT] Could not find BMP-2 turret for smoke launcher adjustment.");
                        return;
                    }

                    Transform kit = turret.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "BMP2M_KIT(Clone)" || t.name == "BMP2M_KIT");

                    Transform pilSmokes = kit != null ? kit.Find("bmp2_front_smokes") : null;

                    if (pilSmokes == null)
                    {
                        MelonLogger.Warning("[BMP2 PIL COMPAT] Could not find bmp2_front_smokes inside PIL's runtime kit.");
                        return;
                    }

                    pilSmokes.localPosition = new Vector3(0.1f, 0.1f, 0f);
                    pilSmokes.localScale = new Vector3(100f, 80f, 100f);

                    MelonLogger.Msg("[BMP2 PIL COMPAT] Applied Fulda smoke launcher position and scale after PIL conversion.");
                }

                MelonLogger.Msg($"[BMP2 PIL COMPAT] RESTORED: {vic.FriendlyName}");
            }
        }
        public override void LoadStaticAssets()
        {
            if (!bmp2d_patch.Value) return;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "bmp2d_full");
            if (BMP2D_Assets.bmp2d_full == null)
            {
                MelonLogger.Error("Could not find bmp2d_full inside bundle, please check that your fulda1989 assets are installed!");
                return;
            }
            BMP2D_Assets.bmp2d_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Transform hull_armour = BMP2D_Assets.bmp2d_full.transform.Find("addon sides/addon sides collider");

            foreach (MeshCollider collider in hull_armour.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor hull_applique = collider.gameObject.AddComponent<UniformArmor>();

                hull_applique.tag = "Penetrable";
                hull_applique.gameObject.layer = 8;
                hull_applique._name = "hull addon side armour";
                hull_applique.PrimaryHeatRha = 10f;
                hull_applique.PrimarySabotRha = 10f;
                hull_applique._canShatterLongRods = true;
                hull_applique._normalizesHits = true;
                hull_applique.AngleMatters = true;
            }

            Transform bmp2mehull = BMP2D_Assets.bmp2d_full.transform.Find("addon sides/2MD-E hull/2MD-E colliders");

            foreach (MeshCollider collider in bmp2mehull.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor bmp2mehullarmour = collider.gameObject.AddComponent<UniformArmor>();

                string name = collider.gameObject.name;

                bmp2mehullarmour.tag = "Penetrable";
                bmp2mehullarmour.gameObject.layer = 8;
                bmp2mehullarmour._canShatterLongRods = true;
                bmp2mehullarmour._normalizesHits = true;
                bmp2mehullarmour.AngleMatters = true;

                if (name is ("LFP"))
                {
                    bmp2mehullarmour._name = "lower glacis plate addon";
                    bmp2mehullarmour.PrimaryHeatRha = 12f;
                    bmp2mehullarmour.PrimarySabotRha = 12f;
                }
                else if (name is ("ufp"))
                {
                    bmp2mehullarmour._name = "upper glacis plate addon";
                    bmp2mehullarmour.PrimaryHeatRha = 10f;
                    bmp2mehullarmour.PrimarySabotRha = 10f;
                }
                else if (name is ("ufp left"))
                {
                    bmp2mehullarmour._name = "upper glacis plate addon";
                    bmp2mehullarmour.PrimaryHeatRha = 9f;
                    bmp2mehullarmour.PrimarySabotRha = 9f;
                }
                else if (name is ("hull roof"))
                {
                    bmp2mehullarmour._name = ("hull roof addon");
                    bmp2mehullarmour.PrimaryHeatRha = 7f;
                    bmp2mehullarmour.PrimarySabotRha = 7f;
                }
                else if (name is ("hull roof mount"))
                {
                    bmp2mehullarmour._name = ("hull roof mount addon");
                    bmp2mehullarmour.PrimaryHeatRha = 4f;
                    bmp2mehullarmour.PrimarySabotRha = 4f;
                }
                else if (name is ("hull roof mount 2"))
                {
                    bmp2mehullarmour._name = ("hull roof mount addon");
                    bmp2mehullarmour.PrimaryHeatRha = 4f;
                    bmp2mehullarmour.PrimarySabotRha = 4f;
                }
                else if (name is ("boxr"))
                {
                    bmp2mehullarmour._name = ("steel box");
                    bmp2mehullarmour.PrimaryHeatRha = 5f;
                    bmp2mehullarmour.PrimarySabotRha = 5f;
                }
                else if (name is ("boxl"))
                {
                    bmp2mehullarmour._name = ("steel box");
                    bmp2mehullarmour.PrimaryHeatRha = 5f;
                    bmp2mehullarmour.PrimarySabotRha = 5f;
                }
            }

            Transform bmp2meturret = BMP2D_Assets.bmp2d_full.transform.Find("turret applique/2MD-E turret/2MD-E turret colliders");

            foreach (MeshCollider collider in bmp2meturret.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor bmp2meturretarmour = collider.gameObject.AddComponent<UniformArmor>();

                string name = collider.gameObject.name;

                bmp2meturretarmour.tag = "Penetrable";
                bmp2meturretarmour.gameObject.layer = 8;
                bmp2meturretarmour._canShatterLongRods = true;
                bmp2meturretarmour._normalizesHits = true;
                bmp2meturretarmour.AngleMatters = true;

                if (name is ("rear box applique (1)"))
                {
                    bmp2meturretarmour._name = "rear turret Ekrany screening";
                    bmp2meturretarmour.PrimaryHeatRha = 35f;
                    bmp2meturretarmour.PrimarySabotRha = 35f;
                }
                else if (name is ("right (1)"))
                {
                    bmp2meturretarmour._name = "right Ekrany cheek";
                    bmp2meturretarmour.PrimaryHeatRha = 25f;
                    bmp2meturretarmour.PrimarySabotRha = 25f;
                }
                else if (name is ("left (1)"))
                {
                    bmp2meturretarmour._name = "left Ekrany cheek";
                    bmp2meturretarmour.PrimaryHeatRha = 25f;
                    bmp2meturretarmour.PrimarySabotRha = 25f;
                }
            }

            Transform turret_rear = BMP2D_Assets.bmp2d_full.transform.Find("turret applique/turret applique collider/rear turret colliders");

            foreach (MeshCollider collider in turret_rear.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor turret_rear_applique = collider.gameObject.AddComponent<UniformArmor>();

                turret_rear_applique.tag = "Penetrable";
                turret_rear_applique.gameObject.layer = 8;
                turret_rear_applique._name = "rear turret applique";
                turret_rear_applique.PrimaryHeatRha = 6f;
                turret_rear_applique.PrimarySabotRha = 6f;
                turret_rear_applique._canShatterLongRods = true;
                turret_rear_applique._normalizesHits = true;
                turret_rear_applique.AngleMatters = true;
            }

            Transform turret_front = BMP2D_Assets.bmp2d_full.transform.Find("turret applique/turret applique collider/turret colliders");

            foreach (MeshCollider collider in turret_front.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor turret_front_applique = collider.gameObject.AddComponent<UniformArmor>();
                turret_front_applique.tag = "Penetrable";
                turret_front_applique.gameObject.layer = 8;
                turret_front_applique._name = "front turret applique";
                turret_front_applique.PrimaryHeatRha = 35f;
                turret_front_applique.PrimarySabotRha = 35f;
                turret_front_applique._canShatterLongRods = true;
                turret_front_applique._normalizesHits = true;
                turret_front_applique.AngleMatters = true;
            }

            Transform turret_roof = BMP2D_Assets.bmp2d_full.transform.Find("turret applique/turret applique roof collider");

            foreach (MeshCollider collider in turret_roof.GetComponentsInChildren<MeshCollider>(true))
            {
                UniformArmor turret_roof_applique = collider.gameObject.AddComponent<UniformArmor>();
                turret_roof_applique.tag = "Penetrable";
                turret_roof_applique.gameObject.layer = 8;
                turret_roof_applique._name = "front turret applique";
                turret_roof_applique.PrimaryHeatRha = 8f;
                turret_roof_applique.PrimarySabotRha = 8f;
                turret_roof_applique._canShatterLongRods = true;
                turret_roof_applique._normalizesHits = true;
                turret_roof_applique.AngleMatters = true;
            }
            Util.SetupFLIRShaders(BMP2D_Assets.bmp2d_full);

            MelonLogger.Msg("Loaded BMP-2D armour asset successfully");

        }
        public static void Init()
        {
            if (!bmp2d_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
