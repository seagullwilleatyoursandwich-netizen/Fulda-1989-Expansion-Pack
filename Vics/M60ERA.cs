using GHPC.Equipment;
using GHPC.State;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weaponry;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Fulda1989
{
    public class M60ERA : Module
    {
        static MelonPreferences_Entry<bool> m60era_patch;
        static MelonPreferences_Entry<bool> m1era;
        static MelonPreferences_Entry<bool> m833_apfsds;
        static MelonPreferences_Entry<int> m60a1p_chance;
        static MelonPreferences_Entry<int> m60a3tts_chance;
        static MelonPreferences_Entry<bool> m900;
        static MelonPreferences_Entry<int> m900_chance;

        public static void Config(MelonPreferences_Category category)
        {
            m60era_patch = category.CreateEntry("M60 ERA Enabled", true);
            m60era_patch.Comment = "Enables the M60 ERA";

            m1era = category.CreateEntry("M1 ERA Enabled", true);
            m1era.Comment = "Enables/Disables ERA spawning on M60's";

            m60a1p_chance = category.CreateEntry("M60A1 RISE PASSIVE Conversion Chance (%)", 70);
            m60a1p_chance.Comment = "Random chance for an M60A1 RISE PASSIVE to spawn with ERA";

            m60a3tts_chance = category.CreateEntry("M60A3 TTS Conversion Chance (%)", 40);
            m60a3tts_chance.Comment = "Random chance for an M60A3 TTS to spawn with ERA";

            m833_apfsds = category.CreateEntry("M60s Use M833 APFSDS-T", true);
            m833_apfsds.Comment = "Allows ERA-converted M60A1 RISE (Passive) and M60A3 TTS vehicles to use M833.";

            m900 = category.CreateEntry("M60A3 TTS Uses M900 APFSDS-T", false);
            m900.Comment = "Allows ERA-converted M60A3 TTS's to use M900 APFSDS.";

            m900_chance = category.CreateEntry("M60A3 TTS M900 Chance (%)", 10);
            m900_chance.Comment = "Chance for an ERA-converted M60A3 TTS to receive M900 instead of M833.";
        }
        public static IEnumerator Convert(GameState _)
        {
            var probe = M60_Assets.m60_full.transform.Find("HULL M1/HULL M1 ERA");
            bool ready = probe != null && probe.GetChild(0).GetComponent<GHPC.UniformArmor>() != null;
            //MelonLogger.Msg($"[M60ERA] Convert() firing — template armor-ready = {ready}");

            foreach (Vehicle vic in Mod.vics)
            {
                if (vic == null) continue;
                //MelonLogger.Msg("Detected vehicle: " + vic.FriendlyName);

                GameObject vic_go = vic.gameObject;
                if (!vic.UniqueName.Contains("M60")) continue;
                if (vic_go.GetComponent<AlreadyConverted>() != null)
                    continue;

                int conversion_chance = 0;

                if (vic.FriendlyName.Contains("M60A1 RISE (Passive)"))
                {
                    conversion_chance = m60a1p_chance.Value;
                }
                else if (vic.FriendlyName.Contains("M60A3 TTS"))
                {
                    conversion_chance = m60a3tts_chance.Value;
                }

                bool converted_to_m60era = m1era.Value && UnityEngine.Random.Range(0, 100) < conversion_chance;
                if (converted_to_m60era)
                {
                    vic_go.AddComponent<AlreadyConverted>();

                    if (vic.FriendlyName.Contains("M60A3 TTS"))
                    {
                        vic._friendlyName = "M60A3 TTS ERA";
                    }
                    else if (vic.FriendlyName.Contains("M60A1 RISE (Passive)"))
                    {
                        vic._friendlyName = "M60A1 RISE(P) ERA";
                    }

                    Transform hull = vic.transform.Find("M60_meshes/hull_late");
                    Transform turret = null;

                    if (vic.FriendlyName.Contains("M60A3 TTS"))
                    {
                        turret = vic.transform.Find("M60A3TTS_rig/hull/turret");
                    }
                    else if (vic.FriendlyName.Contains("M60A1 RISE"))
                    {
                        turret = vic.transform.Find("--RIG/hull/turret");
                    }

                    Transform mantlet = null;

                    if (vic.FriendlyName.Contains("M60A3 TTS"))
                    {
                        mantlet = vic.transform.Find("M60A3TTS_rig/hull/turret/main gun mantlet");
                    }
                    else if (vic.FriendlyName.Contains("M60A1 RISE"))
                    {
                        mantlet = vic.transform.Find("--RIG/hull/turret/main gun mantlet");
                    }

                    if (vic.FriendlyName.Contains("M60A3 TTS"))
                    {
                        bool getsM900 = m900.Value && UnityEngine.Random.Range(0, 100) < Mathf.Clamp(m900_chance.Value, 0, 100);

                        bool m900Applied = false;

                        if (getsM900)
                        {
                            m900Applied = ApplyApfsdsToRefurbishedM60(vic, Ammo_120mm_USA.clip_codex_m900, "M900 APFSDS-T");

                            if (m900Applied)
                            {
                                Transform gunTransform = vic.transform.Find("M60A3TTS_rig/hull/turret/main gun mantlet/Gun Scripts/105mm Gun M68");

                                WeaponSystem gun = gunTransform != null ? gunTransform.GetComponent<WeaponSystem>() : null;

                                if (gun != null && gun.CodexEntry != null)
                                {
                                    WeaponSystemCodexScriptable m68a1Codex = ScriptableObject.Instantiate(gun.CodexEntry);

                                    m68a1Codex.name = "gun_M68A1";
                                    m68a1Codex.FriendlyName = "105mm Gun M68A1";
                                    gun.CodexEntry = m68a1Codex;

                                    //MelonLogger.Msg($"[M60 ERA] Set main gun designation to M68A1 on {vic.FriendlyName} (M900 equipped).");
                                }
                                else
                                {
                                    MelonLogger.Warning($"[M60 ERA] M900 was applied, but the M68 WeaponSystem or CodexEntry was not found on {vic.FriendlyName}.");
                                }
                            }
                        }

                        // M833 is the fallback whenever M900 was not successfully applied.
                        if (!m900Applied && m833_apfsds.Value)
                        {
                            ApplyApfsdsToRefurbishedM60(vic, FindM833Clip(), "M833 APFSDS-T");
                        }
                    }

                    GameObject m1_full = GameObject.Instantiate(M60_Assets.m60_full);

                    Transform hull_m1 = M60_Assets.m60_full.transform.Find("HULL M1/HULL M1 ERA");
                    Transform turret_m1 = M60_Assets.m60_full.transform.Find("TURRET M1/TURRET M1 ERA");
                    Transform mantlet_m1 = M60_Assets.m60_full.transform.Find("MANTLET M1/MANTLET M1 ERA");

                    Transform era_hull = m1_full.transform.Find("HULL M1");
                    Transform era_turret = m1_full.transform.Find("TURRET M1");
                    Transform era_mantlet = m1_full.transform.Find("MANTLET M1");

                    // HULL ERA
                    if (era_hull != null)
                    {
                        era_hull.SetParent(hull, false);

                        era_hull.localPosition = new Vector3(0.14f, -0.035f, 0.03f);
                        era_hull.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_hull.localScale = new Vector3(0.1725f, 0.1725f, 0.1725f);

                        Transform hull_late_follow = vic.transform.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_hull.SetParent(hull_late_follow);
                    }

                    // TURRET ERA
                    if (era_turret != null)
                    {
                        era_turret.SetParent(turret, false);

                        era_turret.localPosition = new Vector3(0.8f, -0.51f, 2.7f);
                        era_turret.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_turret.localScale = new Vector3(1f, 1f, 1f);

                        Transform turret_late_follow = turret.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_turret.SetParent(turret_late_follow);
                    }
                    // MANTLET ERA
                    if (era_mantlet != null)
                    {
                        era_mantlet.SetParent(mantlet, false);

                        era_mantlet.localPosition = new Vector3(0.81f, -0.88f, 1.7f);
                        era_mantlet.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_mantlet.localScale = new Vector3(1f, 1f, 1f);

                        Transform mantlet_late_follow = mantlet.GetComponent<LateFollowTarget>()._lateFollowers[0].transform;
                        era_mantlet.SetParent(mantlet_late_follow);
                    }
                    GameObject.Destroy(m1_full);

                    // Remove the M60A1 RISE (Passive) IR lamp from the equipment system and model. (+ destroy the actual model to prevent it from reactivating)
                    if (vic.FriendlyName.Contains("M60A1 RISE"))
                    {
                        GHPC.Equipment.Lamps.LampType lamp_type = GHPC.Equipment.Lamps.LampType.GunnerSpotlight;

                        EquipmentManager equipment_manager = vic._equipmentManager;

                        if (equipment_manager != null)
                        {
                            equipment_manager.lampsDict.Remove(lamp_type);
                            equipment_manager.AllLamps = equipment_manager.AllLamps.Where(o => o.Type != lamp_type).ToArray();
                        }

                        Transform irLamp = vic.transform.Find("--RIG/hull/turret/main gun mantlet/M60_VVS");

                        if (irLamp != null)
                        {
                            GameObject.Destroy(irLamp.gameObject);
                            //MelonLogger.Msg("[M60 ERA] Destroyed M60A1 RISE (Passive) IR lamp.");
                        }
                        else
                        {
                            MelonLogger.Warning("[M60 ERA] Could not find M60A1 IR lamp at the expected path.");
                        }
                    }

                string[] objectsToDisable =
                {
                    "M60A3TTS_rig/hull/M60 hull net",
                    "M60A3TTS_rig/hull/turret/M60 turret net",
                    "--RIG/hull/M60 hull net",
                    "--RIG/hull/turret/M60 turret net",
                    "M60A3TTS_rig/hull/turret/main gun mantlet/spare_wheel_mantlet",
                    "us_20L_water",
                    "us_20L_water",
                };

                    foreach (string path in objectsToDisable)
                    {
                        Transform obj = vic.transform.Find(path);

                        if (obj != null)
                        {
                            obj.gameObject.SetActive(false);
                        }
                    }
                    ApplyM60EraCamo(vic_go);
                    yield return null;
                }
            }
        }

        private static bool IsApfsdsRound(AmmoCodexScriptable round)
        {
            return round != null && round.AmmoType != null && !string.IsNullOrEmpty(round.AmmoType.Name) && round.AmmoType.Name.IndexOf("APFSDS", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsApfsdsClip(AmmoClipCodexScriptable clip)
        {
            return clip != null && clip.ClipType != null && clip.ClipType.MinimalPattern != null && clip.ClipType.MinimalPattern.Any(IsApfsdsRound);
        }

        private static bool IsApfsdsClip(AmmoType.AmmoClip clip)
        {
            return clip != null && clip.MinimalPattern != null && clip.MinimalPattern.Any(IsApfsdsRound);
        }

        private static AmmoClipCodexScriptable FindM833Clip()
        {
            return Resources.FindObjectsOfTypeAll<AmmoClipCodexScriptable>().FirstOrDefault(c => c != null && c.name.Equals("clip_M833", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ApplyApfsdsToRefurbishedM60(Vehicle vic, AmmoClipCodexScriptable replacementClip, string roundName)
        {
            LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();

            if (loadoutManager == null || loadoutManager.LoadedAmmoList == null ||
                loadoutManager.LoadedAmmoList.AmmoClips == null || loadoutManager.RackLoadouts == null)
            {
                MelonLogger.Warning($"[M60 ERA] Cannot apply {roundName} to {vic.FriendlyName}: loadout unavailable.");
                return false;
            }

            if (replacementClip == null || replacementClip.ClipType == null)
            {
                MelonLogger.Warning($"[M60 ERA] Cannot apply {roundName} to {vic.FriendlyName}: replacement clip unavailable.");
                return false;
            }

            AmmoClipCodexScriptable[] loadedClips = loadoutManager.LoadedAmmoList.AmmoClips;
            List<int> apIndices = new List<int>();

            for (int i = 0; i < loadedClips.Length; i++)
            {
                if (IsApfsdsClip(loadedClips[i]))
                    apIndices.Add(i);
            }

            if (apIndices.Count == 0)
            {
                string loadedNames = string.Join(", ", loadedClips.Select(c => c != null ? $"{c.name} ({c.ClipType?.Name})" : "NULL"));
                MelonLogger.Warning($"[M60 ERA] No existing APFSDS clip found for {vic.FriendlyName}. Loaded clips: {loadedNames}");
                return false;
            }

            // Replace the existing APFSDS entries without changing their positions or counts.
            foreach (int index in apIndices)
                loadedClips[index] = replacementClip;

            loadoutManager.LoadedAmmoList.AmmoClips = loadedClips;

            int initialClipsReplaced = 0;
            int rackTypesReplaced = 0;

            for (int i = 0; i < loadoutManager.RackLoadouts.Length; i++)
            {
                var rackLoadout = loadoutManager.RackLoadouts[i];
                GHPC.Weapons.AmmoRack rack = rackLoadout.Rack;

                if (rack == null)
                    continue;

                AmmoClipCodexScriptable[] initialClips = rackLoadout.OverrideInitialClips;

                if (initialClips != null)
                {
                    for (int j = 0; j < initialClips.Length; j++)
                    {
                        if (!IsApfsdsClip(initialClips[j]))
                            continue;

                        initialClips[j] = replacementClip;
                        initialClipsReplaced++;
                    }

                    rackLoadout.OverrideInitialClips = initialClips;
                }

                if (rack.ClipTypes != null)
                {
                    for (int j = 0; j < rack.ClipTypes.Length; j++)
                    {
                        if (!IsApfsdsClip(rack.ClipTypes[j]))
                            continue;

                        rack.ClipTypes[j] = replacementClip.ClipType;
                        rackTypesReplaced++;
                    }
                }

                Util.EmptyRack(rack);
            }

            loadoutManager.SpawnCurrentLoadout();

            WeaponsManager weaponsManager = vic.GetComponent<WeaponsManager>();

            if (weaponsManager != null && weaponsManager.Weapons != null && weaponsManager.Weapons.Length > 0)
            {
                WeaponSystem mainGun = weaponsManager.Weapons[0].Weapon;

                if (mainGun != null && mainGun.Feed != null)
                {
                    mainGun.Feed.AmmoTypeInBreech = null;
                    mainGun.Feed.Start();
                }
            }

            loadoutManager.RegisterAllBallistics();

            //MelonLogger.Msg($"[M60 ERA] {roundName} applied to {vic.FriendlyName}. " + $"AP entries={apIndices.Count}, initial clips replaced={initialClipsReplaced}, rack types replaced={rackTypesReplaced}.");

            return true;
        }
        public override void LoadStaticAssets()
        {
            if (!m60era_patch.Value) return;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "m60_full");

            if (M60_Assets.m60_full == null)
            {
                MelonLogger.Error("Could not find m60_full inside bundle, please check that your fulda1989 assets are installed!");
                return;
            }

            M60_Assets.m60_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Transform hull_m1 = M60_Assets.m60_full.transform.Find("HULL M1/HULL M1 ERA");
            Transform turret_m1 = M60_Assets.m60_full.transform.Find("TURRET M1/TURRET M1 ERA");
            Transform mantlet_m1 = M60_Assets.m60_full.transform.Find("MANTLET M1/MANTLET M1 ERA");

            M1ERA.Setup(hull_m1, hull_m1.parent);
            M1ERA.Setup(turret_m1, turret_m1.parent);
            M1ERA.Setup(mantlet_m1, mantlet_m1.parent);

            Util.SetupFLIRShaders(M60_Assets.m60_full);

            MelonLogger.Msg("Loaded M60 ERA asset successfully");
        }

        private static void ApplyM60EraCamo(GameObject vehicleRoot)
        {
            if (vehicleRoot == null)
                return;

            Texture2D replacement = M60_Assets.woodlandM60Era;

            if (replacement == null)
            {
                MelonLogger.Warning("[M60 ERA CAMO] Woodland M60 ERA texture is unavailable.");
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

                    foreach (string propertyName in material.GetTexturePropertyNames())
                    {
                        Texture texture = material.GetTexture(propertyName);

                        if (texture == null)
                            continue;

                        string textureName = texture.name.Replace(" (Instance)", "");

                        if (!textureName.Equals("MERDC", StringComparison.OrdinalIgnoreCase))
                            continue;

                        material.SetTexture(propertyName, replacement);
                        changed++;

                        //MelonLogger.Msg($"[M60 ERA CAMO] Replaced MERDC on {renderer.name}, " + $"material {material.name}, property {propertyName}.");
                    }
                }

                renderer.materials = materials;
            }

            //MelonLogger.Msg($"[M60 ERA CAMO] {vehicleRoot.name}: checked {renderersChecked} renderers, " + $"replaced {changed} MERDC texture references.");
        }
        public static void Init()
        {
            if (!m60era_patch.Value) return;

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}
