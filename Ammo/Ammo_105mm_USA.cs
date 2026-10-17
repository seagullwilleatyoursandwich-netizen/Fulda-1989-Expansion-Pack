using System;
using System.Linq;
using System.Collections.Generic;
using GHPC.Weapons;
using UnityEngine;
using GHPC.Weaponry;

namespace Fulda1989
{
    public class Ammo_120mm_USA : ModUtil.Module
    {
        public static Dictionary<string, AmmoClipCodexScriptable> ap;
        public static Dictionary<string, AmmoClipCodexScriptable> heat;
        public static AmmoClipCodexScriptable clip_codex_m900;
        public static AmmoType.AmmoClip clip_m900;
        public static AmmoCodexScriptable ammo_codex_m900;
        public static AmmoType ammo_m900;
        public static GameObject ammo_m900_vis = null;

        public static void CreateCompositeOptimizations()
        {
            var composite_optimizations_m900 = new List<AmmoType.ArmorOptimization>() { };

            string[] composite_names = new string[] {
                "Vorschlaghammer composite",
                "Eber composite",
                "Abrams special armor gen 1 hull front",
                "Abrams special armor gen 1 mantlet",
                "Abrams special armor gen 1 turret cheeks",
                "Abrams special armor gen 1 turret sides",
                "Abrams special armor gen 0 turret cheeks",
                "Corundum ball armor",
                "Kvartz"
            };

            ArmorCodexScriptable[] armours = Resources.FindObjectsOfTypeAll<ArmorCodexScriptable>();

            foreach (string name in composite_names)
            {
                IEnumerable<ArmorCodexScriptable> possible_armours = armours.Where(o => o.name == name);

                if (possible_armours.Count() == 0) continue;

                ArmorCodexScriptable armour = possible_armours.First();

                AmmoType.ArmorOptimization optimization_m900 = new AmmoType.ArmorOptimization();
                optimization_m900.Armor = armour;
                optimization_m900.RhaRatio = 0.95f;
                composite_optimizations_m900.Add(optimization_m900);

            }

            ammo_m900.ArmorOptimizations = composite_optimizations_m900.ToArray<AmmoType.ArmorOptimization>();
        }

        public override void UnloadDynamicAssets()
        {
            GameObject.DestroyImmediate(ammo_m900_vis);
        }

        public override void LoadDynamicAssets()
        {

            AmmoClipCodexScriptable m833BaseClip = Resources.FindObjectsOfTypeAll<AmmoClipCodexScriptable>().FirstOrDefault(c => c != null && c.name.Equals("clip_M833", StringComparison.OrdinalIgnoreCase));
            AmmoCodexScriptable m833BaseRound = m833BaseClip?.ClipType?.MinimalPattern?.FirstOrDefault(a => a != null && a.AmmoType != null);

            AmmoType baseAmmo = m833BaseRound?.AmmoType ?? Bundeswehr_120mm_Assets.ammo_APFSDS;

            // M900 APFSDS
            ammo_m900 = new AmmoType();
            Util.ShallowCopy(ammo_m900, baseAmmo);
            ammo_m900.Name = "M900 APFSDS-T";
            ammo_m900.Caliber = 105;
            ammo_m900.RhaPenetration = 650;
            ammo_m900.MuzzleVelocity = 1505;
            ammo_m900.Mass = 3.83f;
            ammo_m900.SectionalArea = 0.0005f;
            ammo_m900.SpallMultiplier = 1f;
            ammo_m900.CachedIndex = -1;
            ammo_m900.CertainRicochetAngle = 10f;
            ammo_m900.Coeff = 0.3f;
            ammo_m900.MaxSpallRha = 12f;
            ammo_m900.MinSpallRha = 1f;
            ammo_m900.ShatterOnRicochet = true;

            Util.Coalesce(ref ammo_codex_m900);
            ammo_codex_m900.AmmoType = ammo_m900;
            ammo_codex_m900.name = "ammo_m900";

            clip_m900 = new AmmoType.AmmoClip();
            clip_m900.Capacity = 1;
            clip_m900.Name = "M900 APFSDS-T";
            clip_m900.MinimalPattern = new AmmoCodexScriptable[1];
            clip_m900.MinimalPattern[0] = ammo_codex_m900;

            Util.Coalesce(ref clip_codex_m900);
            clip_codex_m900.name = "clip_m900";
            clip_codex_m900.CompatibleWeaponSystems = m833BaseClip?.CompatibleWeaponSystems != null ? m833BaseClip.CompatibleWeaponSystems.ToArray() : new WeaponSystemCodexScriptable[0];
            clip_codex_m900.ClipType = clip_m900;

            ammo_m900_vis = GameObject.Instantiate(baseAmmo.VisualModel);
            ammo_m900_vis.name = "M900 visual";
            ammo_m900.VisualModel = ammo_m900_vis;
            ammo_m900.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_m900;
            ammo_m900.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_m900;

            Util.CacheAmmo(ammo_m900);

            ap = new Dictionary<string, AmmoClipCodexScriptable>()
            {
                ["M900"] = clip_codex_m900,
            };

            heat = new Dictionary<string, AmmoClipCodexScriptable>()
            {
            };
        }
    }
}