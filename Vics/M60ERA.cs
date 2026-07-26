using GHPC;
using GHPC.Equipment;
using GHPC.Equipment.Optics;
using GHPC.State;
using GHPC.Vehicle;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using NWH.VehiclePhysics;
using Reticle;
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
        static MelonPreferences_Entry<int> m60a1p_chance;
        static MelonPreferences_Entry<int> m60a3tts_chance;

        public static void Config(MelonPreferences_Category category)
        {
            m60era_patch = category.CreateEntry("M60 ERA Enabled", true);
            m60era_patch.Comment = "Enables the M60 ERA";

            m1era = category.CreateEntry("M1 ERA Enabled", true);
            m1era.Comment = "Enables/Disables ERA spawning on M60's";

            m60a1p_chance = category.CreateEntry("M60A1 RISE PASSIVE Conversion Chance (%)", 70);
            m60a1p_chance.Comment = "Random chance for an M60A1 RISE PASSIVE to spawn with ERA";

            m60a3tts_chance = category.CreateEntry("M60A3 TTS Conversion Chance (%)", 30);
            m60a3tts_chance.Comment = "Random chance for an M60A3 TTS to spawn with ERA";
        }
        public static IEnumerator Convert(GameState _)
        {
            yield return new WaitForSeconds(1f);

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

                    //MelonLogger.Msg("Hull target: " + hull);
                    //MelonLogger.Msg("Turret target: " + turret);
                    //MelonLogger.Msg("Mantlet target: " + mantlet);

                    GameObject m1_full = GameObject.Instantiate(M60_Assets.m60_full);

                    Transform era_hull = m1_full.transform.Find("HULL M1");
                    Transform era_turret = m1_full.transform.Find("TURRET M1");
                    Transform era_mantlet = m1_full.transform.Find("MANTLET M1");

                    //MelonLogger.Msg("ERA mantlet asset: " + era_mantlet);
                    //MelonLogger.Msg("ERA hull asset: " + era_hull);
                    //MelonLogger.Msg("ERA turret asset: " + era_turret);

                    // HULL ERA
                    if (era_hull != null)
                    {
                        era_hull.SetParent(hull, false);

                        era_hull.localPosition = new Vector3(0.14f, -0.035f, 0.03f);
                        era_hull.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_hull.localScale = new Vector3(0.1725f, 0.1725f, 0.1725f);
                    }

                    // TURRET ERA
                    if (era_turret != null)
                    {
                        era_turret.SetParent(turret, false);

                        era_turret.localPosition = new Vector3(0.8f, -0.51f, 2.7f);
                        era_turret.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_turret.localScale = new Vector3(1f, 1f, 1f);

                    }
                    // MANTLET ERA
                    if (era_mantlet != null)
                    {
                        era_mantlet.SetParent(mantlet, false);

                        era_mantlet.localPosition = new Vector3(0.81f, -0.88f, 1.7f);
                        era_mantlet.localEulerAngles = new Vector3(0f, 90f, 0f);
                        era_mantlet.localScale = new Vector3(1f, 1f, 1f);

                    }

                    m1_full.SetActive(false);
                }
                yield return null;
            } 
        }

        public override void LoadStaticAssets()
        {
            if (!m60era_patch.Value) return;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "m60_full");

            if (M60_Assets.m60_full == null)
            {
                MelonLogger.Error("Could not find m60_full inside bundle");
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

        public static void Init()
        {
            if (!m60era_patch.Value) return;

            StateController.WaitForComplete(GameState.GameReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}