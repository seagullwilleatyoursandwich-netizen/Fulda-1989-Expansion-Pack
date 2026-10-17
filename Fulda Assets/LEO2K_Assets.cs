using GHPC.Thermals;
using GHPC.Vehicle;
using MelonLoader;
using MelonLoader.Utils;
using ModUtil;
using System;
using System.IO;
using UnityEngine;
using VanillaAssetUtil.AssetUtil;

namespace Fulda1989
{
    public class LEO2K_Assets : Module
    {
        internal static GameObject leo2k_full;
        internal static AssetBundle leo2k_bundle;
        internal static Texture2D oliveDrabOverlay;
        internal static Material leo2kBlack3;
        public static Vehicle marder;
        public static Vehicle leopard2;
        public override void LoadDynamicAssets()
        {
            MelonLogger.Msg("[LEO2K Assets] Loading Marder donor...");

            marder = AssetUtilApi.LoadVanillaVehicle("MARDERA1PLUS");

            MelonLogger.Msg("[LEO2K Assets] Marder donor result: " + $"{(marder != null ? marder.name : "NULL")}");
        }

        public override void LoadStaticAssets()
        {
            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "leo2k_full");

            leo2k_bundle = AssetBundle.LoadFromFile(path);

            if (leo2k_bundle == null)
            {
                MelonLogger.Error("FAILED TO LOAD LEO2K ASSETBUNDLE");
                return;
            }

            leo2k_full = leo2k_bundle.LoadAsset<GameObject>("LEO2Kfull");

            if (leo2k_full == null)
            {
                MelonLogger.Error("FAILED TO FIND leo2k_full");
                return;
            }

            leo2k_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            foreach (string assetName in leo2k_bundle.GetAllAssetNames())
            {
                if (assetName.IndexOf("olive drab", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                oliveDrabOverlay = leo2k_bundle.LoadAsset<Texture2D>(assetName);

                if (oliveDrabOverlay != null)
                {
                    MelonLogger.Msg($"[LEO2K CAMO] Loaded olive drab texture from AssetBundle: {assetName}");

                    break;
                }
            }

            if (oliveDrabOverlay == null) 
                MelonLogger.Warning("[LEO2K CAMO] Could not find olive drab texture in LEO2K AssetBundle.");

            foreach (string assetName in leo2k_bundle.GetAllAssetNames())
            {
                if (assetName.IndexOf("leo2k black3", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                leo2kBlack3 = leo2k_bundle.LoadAsset<Material>(assetName);

                if (leo2kBlack3 != null)
                {
                    leo2kBlack3.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    MelonLogger.Msg($"[LEO2K MATERIAL] Loaded leo2k black3 material from AssetBundle: {assetName}");
                    break;
                }
            }

            if (leo2kBlack3 == null) 
                MelonLogger.Warning("[LEO2K MATERIAL] Could not find leo2k black3 material in LEO2K AssetBundle.");

            Shader thermalShader = Shader.Find("Standard (FLIR)");

            if (thermalShader == null)
            {
                MelonLogger.Error("FAILED TO FIND Standard (FLIR) SHADER");
                return;
            }

            MeshRenderer[] renderers = leo2k_full.GetComponentsInChildren<MeshRenderer>(true);

            MelonLogger.Msg($"LEO2K: Found {renderers.Length} MeshRenderers.");

            foreach (MeshRenderer renderer in renderers)
                renderer.material.shader = thermalShader;

            leo2k_full.AddComponent<HeatSource>().heat = 0.75f;
        }
    }
}