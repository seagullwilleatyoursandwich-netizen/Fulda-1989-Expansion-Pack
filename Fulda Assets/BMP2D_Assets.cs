using UnityEngine;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using ModUtil;

namespace Fulda1989
{
    public class BMP2D_Assets : Module
    {
        internal static GameObject bmp2d_full;

        public override void LoadStaticAssets()
        {
            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "bmp2d_full");

            AssetBundle bundle = AssetBundle.LoadFromFile(path);

            if (bundle == null)
            {
                MelonLogger.Error("FAILED TO LOAD BMP-2D ASSETBUNDLE");
                return;
            }

            bmp2d_full = bundle.LoadAsset<GameObject>("BMP2Dfull");

            if (bmp2d_full == null)
            {
                MelonLogger.Error("FAILED TO FIND bmp2d_full");
                return;
            }
        }
    }
}