using GHPC;
using GHPC.Camera;
using GHPC.Thermals;
using GHPC.Vehicle;
using GHPC.Weaponry;
using GHPC.Weapons;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Fulda1989
{
    internal class Bundeswehr_120mm_Assets : Module
    {
        public static AmmoType ammo_APFSDS;
        public static AmmoType ammo_HEAT;
        public static GameObject l44_obj;

        public override void LoadStaticAssets()
        {
            Resources.LoadAll<AmmoCodexScriptable>("");

            var ammoCodexes = Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>();

            var m833 = ammoCodexes.FirstOrDefault(o => o.name == "ammo_M833");
            var m456 = ammoCodexes.FirstOrDefault(o => o.name == "ammo_M456");

            MelonLogger.Msg($"[120MM] M833: {m833 != null}");
            MelonLogger.Msg($"[120MM] M456: {m456 != null}");

            if (m833 == null || m456 == null )
            {
                MelonLogger.Error("[120MM] One or more base ammo assets are missing!");
                return;
            }

            ammo_APFSDS = m833.AmmoType;
            ammo_HEAT = m456.AmmoType;

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "l44");

            AssetBundle bundle = AssetBundle.LoadFromFile(path);

            if (bundle == null)
            {
                MelonLogger.Error("FAILED TO LOAD L/44 ASSETBUNDLE");
                return;
            }

            l44_obj = bundle.LoadAsset<GameObject>("l44");

            if (l44_obj == null)
            {
                MelonLogger.Error("FAILED TO FIND l44");
                return;
            }

            l44_obj.hideFlags = HideFlags.DontUnloadUnusedAsset;
            MeshRenderer l44Renderer = l44_obj.GetComponentInChildren<MeshRenderer>(true);
            if (l44Renderer == null)
            {
                MelonLogger.Error("[120MM] Could not find MeshRenderer on l44 or its children!");
                return;
            }
            Shader flirShader = Shader.Find("Standard (FLIR)");
            if (flirShader == null)
            {
                MelonLogger.Error("[120MM] Could not find Standard (FLIR) shader!");
                return;
            }

            l44Renderer.material.shader = flirShader;
            l44_obj.AddComponent<HeatSource>().heat = 0.75f;
            MelonLogger.Msg("[120MM] L/44 asset initialized successfully.");
        }
    }
}
