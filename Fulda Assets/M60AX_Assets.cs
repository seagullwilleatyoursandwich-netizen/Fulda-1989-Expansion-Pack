using UnityEngine;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using ModUtil;

namespace Fulda1989
{
    public class M60AX_Assets : Module
    {
        internal static GameObject m60ax_full;

        public override void LoadStaticAssets()
        {
            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "m60ax_full");

            AssetBundle bundle = AssetBundle.LoadFromFile(path);

            if (bundle == null)
            {
                MelonLogger.Error("FAILED TO LOAD M60AX ASSETBUNDLE");
                return;
            }

            m60ax_full = bundle.LoadAsset<GameObject>("m60ax_full");

            if (m60ax_full == null)
            {
                MelonLogger.Error("FAILED TO FIND m60ax_full");
                return;
            }
        }
    }
}