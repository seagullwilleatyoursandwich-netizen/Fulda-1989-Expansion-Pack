using UnityEngine;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using ModUtil;

namespace Fulda1989
{
    public class T64Assets : Module
    {
        internal static GameObject t64bv_full;

        public override void LoadStaticAssets()
        {
            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "t64bv_full");

            AssetBundle bundle = AssetBundle.LoadFromFile(path);

            if (bundle == null)
            {
                MelonLogger.Error("FAILED TO LOAD T64 ASSETBUNDLE");
                return;
            }

            t64bv_full = bundle.LoadAsset<GameObject>("t64bv_full");

            if (t64bv_full == null)
            {
                MelonLogger.Error("FAILED TO FIND t64bv_full");
                return;
            }
        }
    }
}