using GHPC.Equipment;
using GHPC.Equipment.Optics;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Runtime.CompilerServices;
using Module = ModUtil.Module;
using HarmonyLib;

namespace Fulda1989
{
    public class T64AV : Module
    {
        static MelonPreferences_Entry<bool> t64av_patch;
        static MelonPreferences_Entry<string> t64av_ammo_type;
        static MelonPreferences_Entry<string> t64av_heat_type;
        static MelonPreferences_Entry<bool> kontakt1;
        static MelonPreferences_Entry<int> t64a_1981_av_chance;
        static MelonPreferences_Entry<int> t64a_1983_av_chance;
        static MelonPreferences_Entry<int> t64a_1984_av_chance;

        private sealed class ConversionDecision
        {
            public bool WillConvert;
        }

        private static readonly ConditionalWeakTable<Vehicle, ConversionDecision> conversionDecisions = new ConditionalWeakTable<Vehicle, ConversionDecision>();
        public static void Config(MelonPreferences_Category cfg)
        {
            t64av_patch = cfg.CreateEntry<bool>("T-64AV Patch", true);

            kontakt1 = cfg.CreateEntry<bool>("Kontakt-1 (T-64AV)", true);
            t64a_1981_av_chance = cfg.CreateEntry<int>("T-64A 1981 -> T-64AV Chance (%)", 40);
            t64a_1983_av_chance = cfg.CreateEntry<int>("T-64A 1983 -> T-64AV Chance (%)", 60);
            t64a_1984_av_chance = cfg.CreateEntry<int>("T-64A 1984 -> T-64AV Chance (%)", 80);

            t64av_ammo_type = cfg.CreateEntry<string>("AP Round (T-64AV)", "3BM26");
            t64av_ammo_type.Comment = "3BM32, 3BM26 (composite optimized), 3BM42 (composite optimized), 3BM46";
            t64av_ammo_type.Description = " ";

            t64av_heat_type = cfg.CreateEntry<string>("HEAT Round (T-64AV)", "3BK18M");
            t64av_heat_type.Comment = "3BK18M";
            t64av_heat_type.Description = " ";

            //has_drozd = cfg.CreateEntry<bool>("Drozd APS (T-64A)", false);
            //has_drozd.Comment = "Intercepts incoming projectiles; covers the frontal arc of the tank relative to where the turret is facing";
        }

        public static bool WillConvertToT64AV(Vehicle vic)
        {
            if (vic == null)
                return false;

            // Reuse the same roll when PIL and Fulda both query this vehicle.
            if (conversionDecisions.TryGetValue(vic, out ConversionDecision cached))
                return cached.WillConvert;

            if (t64av_patch == null || kontakt1 == null || !t64av_patch.Value || !kontakt1.Value || vic.GetComponent<AlreadyConverted>() != null || string.IsNullOrEmpty(vic.FriendlyName) || !vic.FriendlyName.Contains("T-64A") || vic.FriendlyName.Contains("T-64AV"))
                return false;

            int conversionChance = 0;

            if (vic.FriendlyName.Contains("T-64A obr.1981"))
                conversionChance = t64a_1981_av_chance.Value;
            else if (vic.FriendlyName.Contains("T-64A obr.1983"))
                conversionChance = t64a_1983_av_chance.Value;
            else if (vic.FriendlyName.Contains("T-64A obr.1984"))
                conversionChance = t64a_1984_av_chance.Value;

            bool willConvert = UnityEngine.Random.Range(0, 100) < conversionChance;

            conversionDecisions.Add(vic, new ConversionDecision
            {WillConvert = willConvert});

            return willConvert;
        }
        public static IEnumerator Convert(GameState _)
        {
            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;

                GameObject vic_go = vic.gameObject;
                if (!vic.FriendlyName.Contains("T-64A")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null) continue;

                bool converted_to_t64av = WillConvertToT64AV(vic);
                if (converted_to_t64av)
                {
                    vic_go.AddComponent<AlreadyConverted>();
                    if (vic.FriendlyName.Contains("T-64A obr.1981"))
                    {
                        vic._friendlyName = "T-64AV obr.1981";
                    }
                    else if (vic.FriendlyName.Contains("T-64A obr.1983"))
                    {
                        vic._friendlyName = "T-64AV obr.1983";
                    }
                    else if (vic.FriendlyName.Contains("T-64A obr.1984"))
                    {
                        vic._friendlyName = "T-64AV obr.1984";
                    }
                }
                ArmyBinocularLookupFix.EnsureSovietEntries(vic.FriendlyName);

                if (converted_to_t64av)
                {
                    // ERA CONVERSION (stolen from bv lol)
                    Transform hull = vic.transform.Find("---T64A_MESH---/HULL");
                    Transform turret = vic.transform.Find("---T64A_MESH---/HULL/TURRET");

                    GameObject k1_full = GameObject.Instantiate(T64Assets.t64bv_full);
                    Transform era_hull = k1_full.transform.Find("HULL K1");
                    Transform era_turret = k1_full.transform.Find("TURRET K1");
                    Transform hull_misc = k1_full.transform.Find("HULL MISC");
                    Transform turret_misc = k1_full.transform.Find("TURRET MISC");

                    // REPLACEMENT TURRET
                    // T-64A may use turret_1979/turret_1984; T-64B uses turret_mesh.
                    Transform markers = turret != null ? turret.Find("T64A_markings") : null;

                    if (markers != null)
                        markers.gameObject.SetActive(false);

                    string[] originalTurretNames = {"turret_1979", "turret_1984"};

                    List<Transform> originalTurretMeshes = new List<Transform>();

                    if (turret != null)
                    {
                        foreach (string meshName in originalTurretNames)
                        {
                            Transform candidate = turret.Find(meshName);

                            if (candidate != null)
                                originalTurretMeshes.Add(candidate);
                        }
                    }

                    // Prefer the active original mesh, so we copy the material from the
                    // variant actually being rendered.
                    Transform originalTurretMesh = originalTurretMeshes.FirstOrDefault(t => t.gameObject.activeInHierarchy && t.GetComponentInChildren<Renderer>(true) != null);

                    if (originalTurretMesh == null)
                    {
                        originalTurretMesh = originalTurretMeshes.FirstOrDefault(t => t.GetComponentInChildren<Renderer>(true) != null);
                    }

                    Renderer originalRenderer = originalTurretMesh != null ? originalTurretMesh.GetComponentInChildren<Renderer>(true) : null;
                    GameObject newTurretInstance = T64Assets.t64av_turret != null ? GameObject.Instantiate(T64Assets.t64av_turret) : null;
                    Transform newturret = newTurretInstance != null ? newTurretInstance.transform : null;

                    if (newturret != null && turret != null)
                    {
                        Renderer replacementRenderer = newturret.GetComponentInChildren<Renderer>(true);

                        if (originalRenderer != null && replacementRenderer != null)
                        {
                            Material originalMaterial = originalRenderer.sharedMaterial;

                            if (originalMaterial != null && originalMaterial.HasProperty("_Albedo") && originalMaterial.GetTexture("_Albedo") != null)
                            {
                                replacementRenderer.sharedMaterial = originalMaterial;

                                MelonLogger.Msg($"[T64AV MATERIAL] Applied {originalMaterial.name} " + $"from {originalTurretMesh.name} to " + $"{replacementRenderer.name}.");
                            }
                            else
                            {
                                MelonLogger.Warning($"[T64AV MATERIAL] No valid _Albedo material found on " + $"{originalTurretMesh.name}.");
                            }
                        }
                        else
                        {
                            MelonLogger.Warning("[T64AV MATERIAL] Original or replacement turret renderer not found.");
                        }

                        // Hide every vanilla turret mesh variant that exists.
                        foreach (Transform originalMesh in originalTurretMeshes)
                            originalMesh.gameObject.SetActive(false);

                        newturret.SetParent(turret, false);
                        newturret.localPosition = new Vector3(0f, -0.33f, -0.0175f);
                        newturret.localEulerAngles = new Vector3(270f, 270f, 0f);
                        newturret.localScale = new Vector3(355f, 355f, 355f);

                        MelonLogger.Msg($"[T64AV] Replacement turret attached. Original mesh: " + $"{(originalTurretMesh != null ? originalTurretMesh.name : "not found")}.");
                    }
                    else
                    {
                        MelonLogger.Error("[T64AV] Replacement turret prefab or vanilla turret hierarchy is missing.");
                    }

                    // HULL ERA
                    if (era_hull != null)
                    {
                        era_hull.SetParent(hull, false);
                        if (hull_misc != null)
                            hull_misc.SetParent(hull, false);

                        era_hull.localPosition = new Vector3(-0.06f, -0.35f, -0.785f);
                        era_hull.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_hull.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_hull.SetParent(hull_late_follow);

                        hull_misc.localPosition = new Vector3(-0.06f, 0.39f, -0.785f);
                        hull_misc.localEulerAngles = new Vector3(0f, 90f, 0f);
                        hull_misc.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        hull_misc.SetParent(hull_late_follow);
                    }
                    // TURRET ERA
                    if (era_turret != null)
                    {
                        era_turret.SetParent(turret, false);
                        if (turret_misc != null)
                            turret_misc.SetParent(turret, false);

                        era_turret.localPosition = new Vector3(-0.075f, -0.25f, -0.24f);
                        era_turret.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_turret.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        Transform turret_late_follow = turret.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_turret.SetParent(turret_late_follow);

                        turret_misc.localPosition = new Vector3(-0.06f, -0.27f, -0.43f);
                        turret_misc.localEulerAngles = new Vector3(0f, 90f, 0f);
                        turret_misc.localScale = new Vector3(0.12f, 0.12f, 0.12f);

                        turret_misc.SetParent(turret_late_follow);
                    }

                    VehicleSmokeManager smoke_manager = vic.GetComponentInChildren<VehicleSmokeManager>();

                    if (smoke_manager != null)
                    {
                        Transform smokes = turret_misc.Find("SMOKES");

                        if (smokes != null)
                        {
                            List<Transform> new_smokes = new List<Transform>();

                            // T-64AV has 8 smoke launchers
                            for (int i = 0; i < 8; i++)
                            {
                                Transform cap = smokes.Find("SMOKE CAP " + i);

                                if (cap != null)
                                    new_smokes.Add(cap);
                            }

                            // MelonLogger.Msg("T64AV smoke caps found: " + new_smokes.Count);

                            // Only move the existing smoke slots onto the K1 rack
                            for (int i = 0; i < new_smokes.Count; i++)
                            {
                                VehicleSmokeManager.SmokeSlot slot = smoke_manager._smokeSlots[i];

                                Transform smoke_cap = new_smokes[i];

                                slot.DisplayBone = smoke_cap;

                                if (slot.SpawnLocation != null)
                                {
                                    slot.SpawnLocation.transform.SetParent(smoke_cap);
                                    slot.SpawnLocation.transform.position = smoke_cap.GetComponent<Renderer>().bounds.center;
                                }
                            }


                            // Remove the extra T-64A salvo
                            List<VehicleSmokeManager.SmokePattern> temp =
                                smoke_manager._smokeGroups.ToList();

                            temp.RemoveAt(2);

                            smoke_manager._smokeGroups = temp.ToArray();
                        }
                    }

                    GameObject.Destroy(k1_full);
                    Transform old_smokes = turret.Find("smoke mortars");

                    if (old_smokes != null)
                    {
                        old_smokes.gameObject.SetActive(false);
                    }

                    Transform camonet1 = vic.transform.Find("---T64A_MESH---/HULL/t64a hull net");
                    if (camonet1 != null)
                    {
                        camonet1.gameObject.SetActive(false);
                    }

                    Transform camonet2 = vic.transform.Find("---T64A_MESH---/HULL/TURRET/t64a turret net");
                    if (camonet2 != null)
                    {
                        camonet2.gameObject.SetActive(false);
                    }

                    Transform camonet3 = vic.transform.Find("---T64A_MESH---/HULL/TURRET/Main gun/Muzzle identity/t64a gun net");
                    if (camonet3 != null)
                    {
                        camonet3.gameObject.SetActive(false);
                    }
                }

                if (!converted_to_t64av)
                {
                    continue;
                }

                WeaponSystem weapon = vic.GetComponent<WeaponsManager>().Weapons[0].Weapon;
                LoadoutManager loadout_manager = vic.GetComponent<LoadoutManager>();

                FireControlSystem fcs = vic.GetComponentInChildren<FireControlSystem>();
                UsableOptic day_optic = Util.GetDayOptic(fcs);

                string ammo_str = t64av_ammo_type.Value;
                MelonLogger.Msg($"[T64AV] Config AP round loaded as: {ammo_str}");
                MelonLogger.Msg($"[T64AV] Config HEAT round loaded as: {t64av_heat_type.Value}");

                try
                {
                    loadout_manager.LoadedAmmoList.AmmoClips[0] = Ammo_125mm.ap[ammo_str];

                    if (t64av_heat_type.Value == "3BK18M")
                        loadout_manager.LoadedAmmoList.AmmoClips[1] = SharedAssets.clip_codex_3bk18m;

                    for (int i = 0; i < loadout_manager.RackLoadouts.Length; i++)
                    {
                        var rackLoadout = loadout_manager.RackLoadouts[i];

                        rackLoadout.AmmoCounts = null;
                        rackLoadout.FixedChoices = new LoadoutManager.RackLoadoutFixedChoice[0];
                        rackLoadout.OverrideInitialClips = null;
                        rackLoadout.ForbiddenAmmoIndices = new int[0];

                        Util.EmptyRack(rackLoadout.Rack);
                    }

                    loadout_manager.SpawnCurrentLoadout();

                    weapon.Feed.AmmoTypeInBreech = null;
                    weapon.Feed.Start();

                    loadout_manager.RegisterAllBallistics();

                    MelonLogger.Msg($"[T64AV] Applied ammo: AP={ammo_str} | HEAT={t64av_heat_type.Value}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[T64AV] Ammo setup failed: {ex}");
                }

                vic.AimablePlatforms[3].transform.Find("optic cover parent").gameObject.SetActive(false);

            }
            yield return null;
        }

        public override void LoadStaticAssets()
        {
            if (!t64av_patch.Value) return;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "t64bv_full");

            if (T64Assets.t64bv_full == null)
            {
                MelonLogger.Error(" (T-64AV) Could not find t64bv_full inside bundle, please check that your fulda1989 assets are installed!");
                return;
            }

            T64Assets.t64bv_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Transform hull_k1 = T64Assets.t64bv_full.transform.Find("HULL K1/ARMOUR");
            Transform turret_k1 = T64Assets.t64bv_full.transform.Find("TURRET K1/TURRET K1 ARMOUR");

            Kontakt1.Setup(hull_k1, hull_k1.parent);
            Kontakt1.Setup(turret_k1, turret_k1.parent);

            Util.SetupFLIRShaders(T64Assets.t64bv_full);

            MelonLogger.Msg("(T-64AV) Loaded T64BV ERA asset successfully");
        }

        [HarmonyPatch]
        public static class PILT64ACompatibilityPatch
        {
            static bool Prepare()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.T64A");
                return type != null && AccessTools.Method(type, "HandleConversion") != null;
            }

            static MethodBase TargetMethod()
            {
                Type type = AccessTools.TypeByName("PactIncreasedLethality.T64A");
                return AccessTools.Method(type, "HandleConversion");
            }

            static bool Prefix(Vehicle vic)
            {
                if (vic == null)
                    return true;

                // Already converted by Fulda: never let PIL process it again.
                if (vic.FriendlyName != null && vic.FriendlyName.Contains("T-64AV"))
                {
                    MelonLogger.Msg($"[T64AV] PIL compatibility shield: ignoring already-converted {vic.FriendlyName}.");

                    return false;
                }

                // Fulda has reserved this vehicle using the shared conversion roll.
                if (T64AV.WillConvertToT64AV(vic))
                {
                    MelonLogger.Msg($"[T64AV] PIL compatibility shield: reserving {vic.FriendlyName} for Fulda conversion.");

                    return false;
                }

                // Fulda will not convert it, so PIL retains normal behaviour.
                return true;
            }
        }

        public static void Init()
        {
            if (!t64av_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
