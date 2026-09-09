using GHPC;
using GHPC.Effects.Voices;
using GHPC.Equipment;
using GHPC.Equipment.Optics;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using NWH.VehiclePhysics;
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
                MelonLogger.Msg("Detected vehicle: " + vic.FriendlyName);

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
                    vic_go.AddComponent<AlreadyConverted>();

                    if (vic.FriendlyName is ("BMP-2") && bmp2d_patch.Value)
                    {
                        vic._friendlyName = "BMP-2D Obr. 1984";
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
                            vic._friendlyName = "BMP-2MD";
                        }
                    }
                    yield return null;
                }
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

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}