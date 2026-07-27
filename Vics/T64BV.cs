using BehaviorDesigner.Runtime.Tasks.Unity.UnityPlayerPrefs;
using GHPC;
using GHPC.Equipment;
using GHPC.Equipment.Optics;
using GHPC.State;
using GHPC.UI.Tips;
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
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using static Rewired.Controller;
using static UnityEngine.GraphicsBuffer;

namespace Fulda1989
{
    public class T64BV : Module
    {
        static MelonPreferences_Entry<bool> t64bv_patch;
        static MelonPreferences_Entry<string> t64bv_ammo_type;
        static MelonPreferences_Entry<string> t64bv_heat_type;
        static MelonPreferences_Entry<bool> t64bv_random_ammo;
        static MelonPreferences_Entry<List<string>> t64bv_random_ammo_pool;
        static MelonPreferences_Entry<bool> t64bv_thermals;
        static MelonPreferences_Entry<string> thermals_qualityt64bv;
        static MelonPreferences_Entry<bool> kontakt1;
        static MelonPreferences_Entry<int> t64b1_1981_chance;
        static MelonPreferences_Entry<int> t64b1_1984_chance;
        static MelonPreferences_Entry<int> t64b_1981_chance;
        static MelonPreferences_Entry<int> t64b_1984_chance;
        public static void Config(MelonPreferences_Category cfg)
        {
            var random_ammo_pool = new List<string>()
            {
                "3BM26",
                "3BM32",
                "3BM42",
                "3BM46"
            };

            t64bv_patch = cfg.CreateEntry<bool>("T-64BV Patch", true);
            t64bv_patch.Comment = "Enables T64BV Patch";

            t64bv_ammo_type = cfg.CreateEntry<string>("AP Round (T-64BV)", "3BM32");
            t64bv_ammo_type.Comment = "3BM32, 3BM26 (composite optimized), 3BM42 (composite optimized), 3BM46";
            t64bv_ammo_type.Description = " ";

            t64bv_heat_type = cfg.CreateEntry<string>("HEAT Round (T-64BV)", "3BK18M");
            t64bv_heat_type.Comment = "3BK18M";
            t64bv_heat_type.Description = " ";

            t64bv_random_ammo = cfg.CreateEntry<bool>("Random AP Round (T-64BV)", false);
            t64bv_random_ammo_pool = cfg.CreateEntry<List<string>>("Random AP Round Pool (T-64BV)", random_ammo_pool);
            t64bv_random_ammo_pool.Comment = "3BM26, 3BM32, 3BM42, 3BM46";

            kontakt1 = cfg.CreateEntry<bool>("Kontakt-1 (T-64BV & T-64B1V)", true);
            t64b_1981_chance = cfg.CreateEntry<int>("T-64B 1981 -> T-64BV Chance", 30);
            t64b_1984_chance = cfg.CreateEntry<int>("T-64B 1984 -> T-64BV Chance", 70);

            t64b1_1981_chance = cfg.CreateEntry<int>("T-64B1 1981 -> T-64BV Chance", 30);
            t64b1_1984_chance = cfg.CreateEntry<int>("T-64B1 1984 -> T-64BV Chance", 70);

            t64bv_thermals = cfg.CreateEntry<bool>("Has Thermals (T-64BV)", true);
            t64bv_thermals.Description = " ";
            t64bv_thermals.Comment = "Replaces night vision sight with thermal sight for T-64BV/B1V variants";
            thermals_qualityt64bv = cfg.CreateEntry<string>("Thermals Quality (T-64BV & T-64BV1)", "Low");
            thermals_qualityt64bv.Comment = "Low, High";
        }

        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;

                GameObject vic_go = vic.gameObject;
                if (!vic.FriendlyName.Contains("T-64B")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                {
                    // MelonLogger.Msg("T64BV skipped " + vic.FriendlyName + " because AlreadyConverted exists");
                    continue;
                }

                int conversion_chance = 0;

                if (vic.FriendlyName.Contains("T-64B1 obr.1981"))
                {
                    conversion_chance = t64b1_1981_chance.Value;
                }
                else if (vic.FriendlyName.Contains("T-64B1 obr.1984"))
                {
                    conversion_chance = t64b1_1984_chance.Value;
                }
                else if (vic.FriendlyName.Contains("T-64B obr.1981"))
                {
                    conversion_chance = t64b_1981_chance.Value;
                }
                else if (vic.FriendlyName.Contains("T-64B obr.1984"))
                {
                    conversion_chance = t64b_1984_chance.Value;
                }

                bool converted_to_t64bv = kontakt1.Value && UnityEngine.Random.Range(0, 100) < conversion_chance;
                if (converted_to_t64bv)
                {
                    vic_go.AddComponent<AlreadyConverted>();

                    if (vic.FriendlyName.Contains("T-64B1"))
                    {
                        vic._friendlyName = "T-64B1V";
                    }
                    else
                    {
                        vic._friendlyName = "T-64BV";
                    }

                    Transform hull = vic.transform.Find("---T64A_MESH---/HULL");
                    Transform turret = vic.transform.Find("---T64A_MESH---/HULL/TURRET");

                    GameObject k1_full = GameObject.Instantiate(T64Assets.t64bv_full);

                    Transform era_hull = k1_full.transform.Find("HULL K1");
                    Transform era_turret = k1_full.transform.Find("TURRET K1");

                    Transform hull_misc = k1_full.transform.Find("HULL MISC");
                    Transform turret_misc = k1_full.transform.Find("TURRET MISC");

                    // HULL ERA
                    if (era_hull != null)
                    {
                        era_hull.SetParent(hull, false);
                        if (hull_misc != null)
                            hull_misc.SetParent(hull, false);

                        era_hull.localPosition = new Vector3(-0.06f, -0.35f, -0.785f);
                        era_hull.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_hull.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform hull_late_follow = hull.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_hull.SetParent(hull_late_follow);

                        hull_misc.localPosition = new Vector3(-0.06f, 0.39f, -0.785f);
                        hull_misc.localEulerAngles = new Vector3(0f, 90f, 0f);
                        hull_misc.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform hullmisc_late_follow = hull.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        hull_misc.SetParent(hullmisc_late_follow);
                    }
                    // TURRET ERA
                    if (era_turret != null)
                    {
                        era_turret.SetParent(turret, false);
                        if (turret_misc != null)
                            turret_misc.SetParent(turret, false);

                        era_turret.localPosition = new Vector3(-0.06f, -0.26f, -0.24f);
                        era_turret.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_turret.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform turret_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_turret.SetParent(turret_late_follow);

                        turret_misc.localPosition = new Vector3(-0.06f, -0.27f, -0.43f);
                        turret_misc.localEulerAngles = new Vector3(0f, 90f, 0f);
                        turret_misc.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform turretmisc_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        turret_misc.SetParent(turretmisc_late_follow);
                    }

                    VehicleSmokeManager smoke_manager = vic.GetComponentInChildren<VehicleSmokeManager>();

                    if (smoke_manager != null)
                    {
                        Transform smokes = turret_misc.Find("SMOKES");

                        if (smokes != null)
                        {
                            // MelonLogger.Msg("Found K1 smoke rack");
                            List<Transform> new_smokes = new List<Transform>();

                            for (int i = 0; i < 8; i++)
                            {
                                Transform cap = smokes.Find("SMOKE CAP " + i);

                                if (cap != null)
                                    new_smokes.Add(cap);
                            }
                            // MelonLogger.Msg("K1 smoke caps found: " + new_smokes.Count);

                            for (int i = 0; i < smoke_manager._smokeSlots.Length; i++)
                            {
                                VehicleSmokeManager.SmokeSlot slot = smoke_manager._smokeSlots[i];

                                Transform smoke_cap = new_smokes[i];

                                slot.DisplayBone = smoke_cap;

                                slot.SpawnLocation.transform.SetParent(smoke_cap);
                                slot.SpawnLocation.transform.position =
                                    smoke_cap.GetComponent<Renderer>().bounds.center;
                            }
                        }
                        else
                        {
                            MelonLogger.Msg("FAILED TO FIND TURRET K1");
                        }
                    }
                    else
                    {
                        MelonLogger.Msg("FAILED TO FIND SMOKE MANAGER");
                    }

                    GameObject.Destroy(k1_full);

                    Transform old_smokes = turret.Find("T64B_smoke");

                    if (old_smokes != null)
                    {
                        old_smokes.gameObject.SetActive(false);
                    }
                }

                if (!converted_to_t64bv)
                {
                    continue;
                }

                bool use_thermal = t64bv_thermals.Value;

                if (converted_to_t64bv)
                {
                    use_thermal = t64bv_thermals.Value;
                }
                if (converted_to_t64bv && t64bv_thermals.Value)
                {
                    if (thermals_qualityt64bv.Value.Equals("Low", StringComparison.OrdinalIgnoreCase))
                    {
                        if (vic.FriendlyName.Contains("T-64B1V"))
                            vic._friendlyName = "T-64B1V obr.1987";
                        else
                            vic._friendlyName = "T-64BV obr.1987";
                    }
                    else if (thermals_qualityt64bv.Value.Equals("High", StringComparison.OrdinalIgnoreCase))
                    {
                        if (vic.FriendlyName.Contains("T-64B1V"))
                            vic._friendlyName = "T-64B1V obr.1989";
                        else
                            vic._friendlyName = "T-64BV obr.1989";
                    }
                }

                WeaponSystem weapon = vic.GetComponent<WeaponsManager>().Weapons[0].Weapon;
                LoadoutManager loadout_manager = vic.GetComponent<LoadoutManager>();

                string ammo_str = t64bv_ammo_type.Value;

                if (converted_to_t64bv)
                {
                    int ammo_rand_bv = UnityEngine.Random.Range(0, t64bv_random_ammo_pool.Value.Count);

                    ammo_str = t64bv_random_ammo.Value
                        ? t64bv_random_ammo_pool.Value.ElementAt(ammo_rand_bv)
                        : t64bv_ammo_type.Value;
                }

                FireControlSystem fcs = vic.GetComponentInChildren<FireControlSystem>();
                UsableOptic day_optic = Util.GetDayOptic(fcs);
                UsableOptic night_optic = day_optic.slot.LinkedNightSight.PairedOptic;

                try
                {
                    loadout_manager.LoadedAmmoList.AmmoClips[0] = Ammo_125mm.ap[ammo_str];

                    if (converted_to_t64bv && t64bv_heat_type.Value == "3BK18M")
                        loadout_manager.LoadedAmmoList.AmmoClips[1] = SharedAssets.clip_codex_3bk18m;

                    for (int i = 0; i < loadout_manager.RackLoadouts.Length; i++)
                    {
                        GHPC.Weapons.AmmoRack rack = loadout_manager.RackLoadouts[i].Rack;
                        Util.EmptyRack(rack);
                    }

                    loadout_manager.SpawnCurrentLoadout();
                    weapon.Feed.AmmoTypeInBreech = null;
                    weapon.Feed.Start();
                    loadout_manager.RegisterAllBallistics();
                }
                catch (Exception)
                {
                    MelonLogger.Msg("Failed, loading default ammo for " + vic.FriendlyName);
                }

                Transform canvas = vic.transform.Find("---T64A_MESH---/HULL/TURRET/Main gun/---MAIN GUN SCRIPTS---/2A46/1G42 gunner's sight/GPS/1G42 Canvas/GameObject");
                canvas.Find("ammo text APFSDS (TMP)").gameObject.SetActive(true);

                vic.AimablePlatforms[3].transform.Find("optic cover parent").gameObject.SetActive(false);

                if (use_thermal)
                {
                    PactThermal.Add(
                            night_optic,
                            converted_to_t64bv
                                ? thermals_qualityt64bv.Value.ToLower()
                                : thermals_qualityt64bv.Value.ToLower()
                        );

                    night_optic.Alignment = OpticAlignment.BoresightStabilized;
                    night_optic.RotateAzimuth = true;
                }
                yield return null;
            }
        }

        public override void LoadStaticAssets()
        {
            if (!t64bv_patch.Value) return;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "t64bv_full");

            if (T64Assets.t64bv_full == null)
            {
                MelonLogger.Error("Could not find t64bv_full inside bundle");
                return;
            }

            T64Assets.t64bv_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Transform hull_k1 = T64Assets.t64bv_full.transform.Find("HULL K1/ARMOUR");
            Transform turret_k1 = T64Assets.t64bv_full.transform.Find("TURRET K1/TURRET K1 ARMOUR");

            Kontakt1.Setup(hull_k1, hull_k1.parent);
            Kontakt1.Setup(turret_k1, turret_k1.parent);

            Util.SetupFLIRShaders(T64Assets.t64bv_full);

            MelonLogger.Msg("Loaded T64BV ERA asset successfully");
        }

        public static void Init()
        {
            if (!t64bv_patch.Value) return;

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}