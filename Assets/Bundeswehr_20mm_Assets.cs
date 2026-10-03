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
    internal class Bundeswehr_20mm_Assets : Module
    {
        public static AmmoType ammo_HEI;

        public override void LoadStaticAssets()
        {
            Resources.LoadAll<AmmoCodexScriptable>("");

            var ammoCodexes = Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>();

            var dm43 = ammoCodexes.FirstOrDefault(o => o != null && o.name == "ammo_20mm_DM43");

            if (dm43 == null)
            {
                MelonLogger.Error("[RH202] 20mm DM43 ammo asset is missing!");
                return;
            }

            ammo_HEI = dm43.AmmoType;

            MelonLogger.Msg($"[RH202] 20mm HEI donor: {dm43.name} | " + $"AmmoType={ammo_HEI?.Name ?? "NULL"}");
        }
    }
}
