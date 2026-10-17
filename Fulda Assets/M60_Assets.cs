using UnityEngine;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using ModUtil;

namespace Fulda1989
{
    public class M60_Assets : Module
    {
        internal static GameObject m60_full;
        public static Texture2D woodlandM60Era;
        public override void LoadStaticAssets()
        {
            string path = Path.Combine(MelonEnvironment.ModsDirectory + "/Fulda1989", "m60_full");

            AssetBundle bundle = AssetBundle.LoadFromFile(path);

            if (bundle == null)
            {
                MelonLogger.Error("FAILED TO LOAD M60 ERA ASSETBUNDLE");
                return;
            }

            m60_full = bundle.LoadAsset<GameObject>("m60_full");


            if (m60_full == null)
            {
                MelonLogger.Error("FAILED TO FIND m60_full");
                return;
            }

            woodlandM60Era = bundle.LoadAsset<Texture2D>("Woodland M60 ERA");

            if (woodlandM60Era == null)
                MelonLogger.Error("[M60 ERA CAMO] Could not find 'Woodland M60 ERA' in the AssetBundle.");
            else
                MelonLogger.Msg("[M60 ERA CAMO] Woodland M60 ERA texture loaded.");
        }
    }
}