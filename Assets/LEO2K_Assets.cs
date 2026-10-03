using GHPC.Thermals;
using GHPC.Vehicle;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System.IO;
using UnityEngine;

namespace Fulda1989
{
    public class LEO2K_Assets : Module
    {
        internal static GameObject leo2k_full;
        public static Vehicle marder;
        public override void LoadDynamicAssets()
        {
            MelonLogger.Msg("[LEO2K Assets] Loading Marder donor...");

            marder = AssetUtil.LoadVanillaVehicle("MARDERA1PLUS");

            MelonLogger.Msg($"[LEO2K Assets] Marder donor result: " + $"{(marder != null ? marder.name : "NULL")}");
        }
        public override void LoadStaticAssets()
        {

            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "leo2k_full");

            AssetBundle bundle = AssetBundle.LoadFromFile(path);

            if (bundle == null)
            {
                MelonLogger.Error("FAILED TO LOAD LEO2K ASSETBUNDLE");
                return;
            }

            leo2k_full = bundle.LoadAsset<GameObject>("LEO2Kfull");

            if (leo2k_full == null)
            {
                MelonLogger.Error("FAILED TO FIND leo2k_full");
                return;
            }

            leo2k_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Shader thermalShader = Shader.Find("Standard (FLIR)");

            if (thermalShader == null)
            {
                MelonLogger.Error("FAILED TO FIND Standard (FLIR) SHADER");
                return;
            }

            MeshRenderer[] renderers = leo2k_full.GetComponentsInChildren<MeshRenderer>(true);

            MelonLogger.Msg($"LEO2K: Found {renderers.Length} MeshRenderers.");

            foreach (MeshRenderer renderer in renderers)
            {
                renderer.material.shader = thermalShader;
            }

            leo2k_full.AddComponent<HeatSource>().heat = 0.75f;
        }
    }
}