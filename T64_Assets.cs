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

            t64bv_full.hideFlags = HideFlags.DontUnloadUnusedAsset;

            Transform hull_k1 = t64bv_full.transform.Find("HULL K1/ARMOUR");
            Transform turret_k1 = t64bv_full.transform.Find("TURRET K1/TURRET K1 ARMOUR");

            Kontakt1.Setup(hull_k1, hull_k1.parent);
            Kontakt1.Setup(turret_k1, turret_k1.parent);

            Util.SetupFLIRShaders(t64bv_full);

            MelonLogger.Msg("Loaded T64 shared assets");
        }
    }
}