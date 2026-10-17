using GHPC.Thermals;
using GHPC.Weaponry;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System.IO;
using System.Linq;
using UnityEngine;
using VanillaAssetUtil.AssetUtil;

namespace Fulda1989
{
    internal class Bundeswehr_120mm_Assets : Module
    {
        public static AmmoType ammo_APFSDS;
        public static AmmoType ammo_HEAT;
        public static GameObject l44_obj;

        public override void LoadStaticAssets()
        {

            // Load the vanilla vehicle that provides the 120mm ammunition assets first.
            FuldaAssetUtil.LoadVanillaVehicle("LEO2A3");

            var clipCodexes = Resources.FindObjectsOfTypeAll<AmmoClipCodexScriptable>();

            var dm23 = clipCodexes.FirstOrDefault(o => o.name == "clip_DM23(120)");
            var dm12a1 = clipCodexes.FirstOrDefault(o => o.name == "clip_DM12A1(120)");

            ammo_APFSDS = dm23?.ClipType?.MinimalPattern?.FirstOrDefault()?.AmmoType;
            ammo_HEAT = dm12a1?.ClipType?.MinimalPattern?.FirstOrDefault()?.AmmoType;

            if (ammo_APFSDS == null)
                MelonLogger.Error("[120MM] Could not find the DM23 ammunition type.");

            if (ammo_HEAT == null)
                MelonLogger.Error("[120MM] Could not find the DM12A1 ammunition type.");

            MelonLogger.Msg($"[120MM] DM23 visual: {(ammo_APFSDS?.VisualModel != null)}");
            MelonLogger.Msg($"[120MM] DM12A1 visual: {(ammo_HEAT?.VisualModel != null)}");

            MelonLogger.Msg($"[120MM] DM23 visual found: {ammo_APFSDS.VisualModel != null}");
            MelonLogger.Msg($"[120MM] DM12A1 visual found: {ammo_HEAT.VisualModel != null}");

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
