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
        public static GameObject t64bv_turret;
        public static GameObject t64av_turret;
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

            t64bv_turret = bundle.LoadAsset<GameObject>("bvturret");
            t64av_turret = bundle.LoadAsset<GameObject>("avturret");

            if (t64bv_turret == null || t64av_turret == null)
                MelonLogger.Error("[T64] Either t64av_turret or t64bv_turret is missing in the AssetBundle search!");
            else
                MelonLogger.Msg("[T64] Loaded both turret prefabs successfully.");

            Util.SetupFLIRShaders(T64Assets.t64bv_full);

            if (T64Assets.t64bv_turret != null)
                Util.SetupFLIRShaders(T64Assets.t64bv_turret);
            if (T64Assets.t64av_turret != null)
                Util.SetupFLIRShaders(T64Assets.t64av_turret);
        }
    }
}