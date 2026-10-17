using System.Collections.Generic;
using System.Linq;
using GHPC.Weapons;
using UnityEngine;
using GHPC.Weaponry;

namespace Fulda1989
{
    public class Ammo_120mm_Bundeswehr : ModUtil.Module
    {
        public static Dictionary<string, AmmoClipCodexScriptable> ap;
        public static Dictionary<string, AmmoClipCodexScriptable> heat;
        public static AmmoClipCodexScriptable clip_codex_dm12a1;
        public static AmmoType.AmmoClip clip_dm12a1;
        public static AmmoCodexScriptable ammo_codex_dm12a1;
        public static AmmoType ammo_dm12a1;

        public static AmmoClipCodexScriptable clip_codex_dm12a1k;
        public static AmmoType.AmmoClip clip_dm12a1k;
        public static AmmoCodexScriptable ammo_codex_dm12a1k;
        public static AmmoType ammo_dm12a1k;


        public static AmmoClipCodexScriptable clip_codex_dm12;
        public static AmmoType.AmmoClip clip_dm12;
        public static AmmoCodexScriptable ammo_codex_dm12;
        public static AmmoType ammo_dm12;

        public static AmmoClipCodexScriptable clip_codex_dm13;
        public static AmmoType.AmmoClip clip_dm13;
        public static AmmoCodexScriptable ammo_codex_dm13;
        public static AmmoType ammo_dm13;

        public static AmmoClipCodexScriptable clip_codex_dm23;
        public static AmmoType.AmmoClip clip_dm23;
        public static AmmoCodexScriptable ammo_codex_dm23;
        public static AmmoType ammo_dm23;

        public static AmmoClipCodexScriptable clip_codex_dm33;
        public static AmmoType.AmmoClip clip_dm33;
        public static AmmoCodexScriptable ammo_codex_dm33;
        public static AmmoType ammo_dm33;
        public static AmmoClipCodexScriptable clip_codex_dm43k;
        public static AmmoType.AmmoClip clip_dm43k;
        public static AmmoCodexScriptable ammo_codex_dm43k;
        public static AmmoType ammo_dm43k;

        public static GameObject ammo_dm13_vis = null;
        public static GameObject ammo_dm23_vis = null;
        public static GameObject ammo_dm33_vis = null;
        public static GameObject ammo_dm43k_vis = null;
        public static GameObject ammo_dm12a1k_vis = null;
        public static GameObject ammo_dm12a1_vis = null;
        public static GameObject ammo_dm12_vis = null;

        public static void CreateCompositeOptimizations()
        {
            var composite_optimizations_dm33 = new List<AmmoType.ArmorOptimization>() { };
            var composite_optimizations_dm43k = new List<AmmoType.ArmorOptimization>() { };

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

                AmmoType.ArmorOptimization optimization_dm33 = new AmmoType.ArmorOptimization();
                optimization_dm33.Armor = armour;
                optimization_dm33.RhaRatio = 0.90f;
                composite_optimizations_dm33.Add(optimization_dm33);

                AmmoType.ArmorOptimization optimization_dm43k = new AmmoType.ArmorOptimization();
                optimization_dm43k.Armor = armour;
                optimization_dm43k.RhaRatio = 0.80f;
                composite_optimizations_dm43k.Add(optimization_dm43k);
            }

            ammo_dm43k.ArmorOptimizations = composite_optimizations_dm43k.ToArray<AmmoType.ArmorOptimization>();
            ammo_dm33.ArmorOptimizations = composite_optimizations_dm33.ToArray<AmmoType.ArmorOptimization>();
        }


        public override void UnloadDynamicAssets()
        {
            GameObject.DestroyImmediate(ammo_dm13_vis);
            GameObject.DestroyImmediate(ammo_dm23_vis);
            GameObject.DestroyImmediate(ammo_dm33_vis);
            GameObject.DestroyImmediate(ammo_dm43k_vis);
            GameObject.DestroyImmediate(ammo_dm12a1k_vis);
            GameObject.DestroyImmediate(ammo_dm12a1_vis);
            GameObject.DestroyImmediate(ammo_dm12_vis);
        }

        public override void LoadDynamicAssets()
        {
            // DM13 APFSDS
            ammo_dm13 = new AmmoType();
            Util.ShallowCopy(ammo_dm13, Bundeswehr_120mm_Assets.ammo_APFSDS);
            ammo_dm13.Name = "DM13 APFSDS-T";
            ammo_dm13.Caliber = 120;
            ammo_dm13.RhaPenetration = 470;
            ammo_dm13.MuzzleVelocity = 1650f;
            ammo_dm13.Mass = 4.6f;
            ammo_dm13.SectionalArea = 0.0006f;
            ammo_dm13.SpallMultiplier = 1f;
            ammo_dm13.CachedIndex = -1;
            ammo_dm13.CertainRicochetAngle = 10f;
            ammo_dm13.Coeff = 0.3f;
            ammo_dm13.MaxSpallRha = 12f;
            ammo_dm13.MinSpallRha = 1f;
            ammo_dm13.ShatterOnRicochet = true;

            Util.Coalesce(ref ammo_codex_dm13);
            ammo_codex_dm13.AmmoType = ammo_dm13;
            ammo_codex_dm13.name = "ammo_dm13";

            clip_dm13 = new AmmoType.AmmoClip();
            clip_dm13.Capacity = 1;
            clip_dm13.Name = "DM13 APFSDS-T";
            clip_dm13.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm13.MinimalPattern[0] = ammo_codex_dm13;

            Util.Coalesce(ref clip_codex_dm13);
            clip_codex_dm13.name = "clip_dm13";
            clip_codex_dm13.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_dm13.ClipType = clip_dm13;

            // DM23 APFSDS
            ammo_dm23 = new AmmoType();
            Util.ShallowCopy(ammo_dm23, Bundeswehr_120mm_Assets.ammo_APFSDS);
            ammo_dm23.Name = "DM23 APFSDS-T";
            ammo_dm23.Caliber = 120;
            ammo_dm23.RhaPenetration = 520;
            ammo_dm23.MuzzleVelocity = 1640f;
            ammo_dm23.Mass = 4.45f;
            ammo_dm23.SectionalArea = 0.0006f;
            ammo_dm23.SpallMultiplier = 1f;
            ammo_dm23.CachedIndex = -1;
            ammo_dm23.CertainRicochetAngle = 10f;
            ammo_dm23.Coeff = 0.3f;
            ammo_dm23.MaxSpallRha = 12f;
            ammo_dm23.MinSpallRha = 1f;
            ammo_dm23.ShatterOnRicochet = true;

            Util.Coalesce(ref ammo_codex_dm23);
            ammo_codex_dm23.AmmoType = ammo_dm23;
            ammo_codex_dm23.name = "ammo_dm23";

            clip_dm23 = new AmmoType.AmmoClip();
            clip_dm23.Capacity = 1;
            clip_dm23.Name = "DM23 APFSDS-T";
            clip_dm23.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm23.MinimalPattern[0] = ammo_codex_dm23;

            Util.Coalesce(ref clip_codex_dm23);
            clip_codex_dm23.name = "clip_dm23";
            clip_codex_dm23.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_dm23.ClipType = clip_dm23;

            // DM33 APFSDS
            ammo_dm33 = new AmmoType();
            Util.ShallowCopy(ammo_dm33, Bundeswehr_120mm_Assets.ammo_APFSDS);
            ammo_dm33.Name = "DM33 APFSDS-T";
            ammo_dm33.Caliber = 120;
            ammo_dm33.RhaPenetration = 600;
            ammo_dm33.SpallMultiplier = 1.15f;
            ammo_dm33.MuzzleVelocity = 1650;
            ammo_dm33.Mass = 4.6f;
            ammo_dm33.SectionalArea = 0.0008f;
            ammo_dm33.Coeff = 0.3f;
            ammo_dm33.CachedIndex = -1;
            ammo_dm33.SpallMultiplier = 1f;
            ammo_dm33.CachedIndex = -1;
            ammo_dm33.CertainRicochetAngle = 8f;
            ammo_dm33.Coeff = 0.3f;
            ammo_dm33.MaxSpallRha = 14f;
            ammo_dm33.MinSpallRha = 1f;
            ammo_dm33.ShatterOnRicochet = true;

            Util.Coalesce(ref ammo_codex_dm33);
            ammo_codex_dm33.AmmoType = ammo_dm33;
            ammo_codex_dm33.name = "ammo_dm33";

            clip_dm33 = new AmmoType.AmmoClip();
            clip_dm33.Capacity = 1;
            clip_dm33.Name = "DM33 APFSDS-T";
            clip_dm33.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm33.MinimalPattern[0] = ammo_codex_dm33;

            Util.Coalesce(ref clip_codex_dm33);
            clip_codex_dm33.name = "clip_dm33";
            clip_codex_dm33.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_dm33.ClipType = clip_dm33;

            // DM43K Experimental APFSDS
            ammo_dm43k = new AmmoType();
            Util.ShallowCopy(ammo_dm43k, Bundeswehr_120mm_Assets.ammo_APFSDS);
            ammo_dm43k.Name = "DM43K APFSDS-T";
            ammo_dm43k.Caliber = 120;
            ammo_dm43k.RhaPenetration = 640;
            ammo_dm43k.SpallMultiplier = 0.95f;
            ammo_dm43k.MuzzleVelocity = 1600;
            ammo_dm43k.Mass = 4.9f;
            ammo_dm43k.SectionalArea = 0.0009f;
            ammo_dm43k.Coeff = 0.3f;
            ammo_dm43k.CachedIndex = -1;
            ammo_dm43k.SpallMultiplier = 1f;
            ammo_dm43k.CachedIndex = -1;
            ammo_dm43k.CertainRicochetAngle = 8f;
            ammo_dm43k.Coeff = 0.3f;
            ammo_dm43k.MaxSpallRha = 14f;
            ammo_dm43k.MinSpallRha = 1f;
            ammo_dm43k.ShatterOnRicochet = true;

            Util.Coalesce(ref ammo_codex_dm43k);
            ammo_codex_dm43k.AmmoType = ammo_dm43k;
            ammo_codex_dm43k.name = "ammo_dm43k";

            clip_dm43k = new AmmoType.AmmoClip();
            clip_dm43k.Capacity = 1;
            clip_dm43k.Name = "DM43K APFSDS-T";
            clip_dm43k.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm43k.MinimalPattern[0] = ammo_codex_dm43k;

            Util.Coalesce(ref clip_codex_dm43k);
            clip_codex_dm43k.name = "ammo_DM43K(120)";
            clip_codex_dm43k.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_dm43k.ClipType = clip_dm43k;

            // DM12 HEAT-FS-T
            ammo_dm12 = new AmmoType();
            Util.ShallowCopy(ammo_dm12, Bundeswehr_120mm_Assets.ammo_HEAT);
            ammo_dm12.Name = "DM12 HEAT-FS-T";
            ammo_dm12.Caliber = 120;
            ammo_dm12.RhaPenetration = 480;
            ammo_dm12.TntEquivalentKg = 1.66f;
            ammo_dm12.MuzzleVelocity = 1140f;
            ammo_dm12.Mass = 13.5f;
            ammo_dm12.MaxSpallRha = 12;
            ammo_dm12.MinSpallRha = 1;
            ammo_dm12.CertainRicochetAngle = 3f;
            ammo_dm12.ShatterOnRicochet = false;
            ammo_dm12.DetonateSpallCount = 50;
            ammo_dm12.SectionalArea = 0.0087f;
            ammo_dm12.Coeff = 0.19f;
            ammo_dm12.ForcedSpallAngle = 60;
            ammo_dm12.IgnoreSlat = true;
            ammo_dm12.CachedIndex = -1;
            ammo_dm12.AlwaysProduceBlast = true;
            ammo_dm12.Category = AmmoType.AmmoCategory.ShapedCharge;
            ammo_dm12.CertainRicochetAngle = 3;
            ammo_dm12.SpallMultiplier = 1.2f;
            ammo_dm12.MicroFragScaling = 0.6f;
            //ammo_dm12.VisualModel = GameObject.Find("120mm DM12A1 visual");

            Util.Coalesce(ref ammo_codex_dm12);
            ammo_codex_dm12.AmmoType = ammo_dm12;
            ammo_codex_dm12.name = "ammo_dm12";

            clip_dm12 = new AmmoType.AmmoClip();
            clip_dm12.Capacity = 1;
            clip_dm12.Name = "DM12 HEAT-FS-T";
            clip_dm12.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm12.MinimalPattern[0] = ammo_codex_dm12;

            Util.Coalesce(ref clip_codex_dm12);
            clip_codex_dm12.name = "ammo_DM12(120)";
            clip_codex_dm12.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_dm12.ClipType = clip_dm12;

            // DM12A1 HEAT-FS-T
            ammo_dm12a1 = new AmmoType();
            Util.ShallowCopy(ammo_dm12a1, Bundeswehr_120mm_Assets.ammo_HEAT);
            ammo_dm12a1.Name = "DM12A1 HEAT-FS-T";
            ammo_dm12a1.Caliber = 120;
            ammo_dm12a1._radius = 0.0525f;
            ammo_dm12a1.RhaPenetration = 560;
            ammo_dm12a1.TntEquivalentKg = 2.21f;
            ammo_dm12a1.MuzzleVelocity = 1140f;
            ammo_dm12a1.Mass = 10.2f;
            ammo_dm12a1.MaxSpallRha = 12;
            ammo_dm12a1.MinSpallRha = 1;
            ammo_dm12a1.CertainRicochetAngle = 3f;
            ammo_dm12a1.ShatterOnRicochet = false;
            ammo_dm12a1.DetonateSpallCount = 50;
            ammo_dm12a1.SectionalArea = 0.0087f;
            ammo_dm12a1.Coeff = 0.19f;
            ammo_dm12a1.ForcedSpallAngle = 60;
            ammo_dm12a1.IgnoreSlat = true;
            ammo_dm12a1.CachedIndex = -1;
            ammo_dm12a1.AlwaysProduceBlast = true;
            ammo_dm12a1.Category = AmmoType.AmmoCategory.ShapedCharge;
            ammo_dm12a1.CertainRicochetAngle = 3;
            ammo_dm12a1.SpallMultiplier = 1.25f;
            ammo_dm12a1.MicroFragScaling = 0.7f;
            //ammo_dm12a1.VisualModel = GameObject.Find("120mm DM12A1 visual");

            Util.Coalesce(ref ammo_codex_dm12a1);
            ammo_codex_dm12a1.AmmoType = ammo_dm12a1;
            ammo_codex_dm12a1.name = "ammo_dm12a1";

            clip_dm12a1 = new AmmoType.AmmoClip();
            clip_dm12a1.Capacity = 1;
            clip_dm12a1.Name = "DM12A1 HEAT-FS-T";
            clip_dm12a1.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm12a1.MinimalPattern[0] = ammo_codex_dm12a1;

            Util.Coalesce(ref clip_codex_dm12a1);
            clip_codex_dm12a1.name = "clip_dm12a1";
            clip_codex_dm12a1.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_dm12a1.ClipType = clip_dm12a1;

            // DM12A1K Experimental HEAT-FS-T
            ammo_dm12a1k = new AmmoType();
            Util.ShallowCopy(ammo_dm12a1k, Bundeswehr_120mm_Assets.ammo_HEAT);
            ammo_dm12a1k.Name = "DM12A1K HEAT-FS-T";
            ammo_dm12a1k.Caliber = 120;
            ammo_dm12a1k._radius = 0.0525f;
            ammo_dm12a1k.RhaPenetration = 600;
            ammo_dm12a1k.TntEquivalentKg = 2.62f;
            ammo_dm12a1k.MuzzleVelocity = 1140f;
            ammo_dm12a1k.Mass = 10.2f;
            ammo_dm12a1k.MaxSpallRha = 12;
            ammo_dm12a1k.MinSpallRha = 1;
            ammo_dm12a1k.CertainRicochetAngle = 3f;
            ammo_dm12a1k.ShatterOnRicochet = false;
            ammo_dm12a1k.DetonateSpallCount = 50;
            ammo_dm12a1k.SectionalArea = 0.0087f;
            ammo_dm12a1k.Coeff = 0.19f;
            ammo_dm12a1k.ForcedSpallAngle = 60;
            ammo_dm12a1k.IgnoreSlat = true;
            ammo_dm12a1k.CachedIndex = -1;
            ammo_dm12a1k.AlwaysProduceBlast = true;
            ammo_dm12a1k.Category = AmmoType.AmmoCategory.ShapedCharge;
            ammo_dm12a1k.CertainRicochetAngle = 3;
            ammo_dm12a1k.SpallMultiplier = 1.10f;
            ammo_dm12a1k.MicroFragScaling = 0.8f;
            //ammo_dm12a1k.VisualModel = GameObject.Find("120mm DM12A1 visual");

            Util.Coalesce(ref ammo_codex_dm12a1k);
            ammo_codex_dm12a1k.AmmoType = ammo_dm12a1k;
            ammo_codex_dm12a1k.name = "ammo_dm12a1k";

            clip_dm12a1k = new AmmoType.AmmoClip();
            clip_dm12a1k.Capacity = 1;
            clip_dm12a1k.Name = "DM12A1K HEAT-FS-T";
            clip_dm12a1k.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm12a1k.MinimalPattern[0] = ammo_codex_dm12a1k;

            Util.Coalesce(ref clip_codex_dm12a1k);
            clip_codex_dm12a1k.name = "ammo_DM12A1K(120)";
            clip_codex_dm12a1k.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[1];
            clip_codex_dm12a1k.ClipType = clip_dm12a1k;

            ammo_dm13_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_APFSDS.VisualModel);
            ammo_dm13_vis.name = "DM13 visual";
            ammo_dm13.VisualModel = ammo_dm13_vis;
            ammo_dm13.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_dm13;
            ammo_dm13.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_dm13;

            ammo_dm23_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_APFSDS.VisualModel);
            ammo_dm23_vis.name = "DM23 visual";
            ammo_dm23.VisualModel = ammo_dm23_vis;
            ammo_dm23.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_dm23;
            ammo_dm23.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_dm23;

            ammo_dm33_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_APFSDS.VisualModel);
            ammo_dm33_vis.name = "DM33 visual";
            ammo_dm33.VisualModel = ammo_dm33_vis;
            ammo_dm33.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_dm33;
            ammo_dm33.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_dm33;

            ammo_dm43k_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_APFSDS.VisualModel);
            ammo_dm43k_vis.name = "DM43K visual";
            ammo_dm43k.VisualModel = ammo_dm43k_vis;
            ammo_dm43k.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_dm43k;
            ammo_dm43k.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_dm43k;

            ammo_dm12a1k_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_HEAT.VisualModel);
            ammo_dm12a1k_vis.name = "DM12A1K visual";
            ammo_dm12a1k.VisualModel = ammo_dm12a1k_vis;
            ammo_dm12a1k.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_dm12a1k;
            ammo_dm12a1k.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_dm12a1k;

            ammo_dm12a1_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_HEAT.VisualModel);
            ammo_dm12a1_vis.name = "DM12A1 visual";
            ammo_dm12a1.VisualModel = ammo_dm12a1_vis;
            ammo_dm12a1.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_dm12a1;
            ammo_dm12a1.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_dm12a1;

            ammo_dm12_vis = GameObject.Instantiate(Bundeswehr_120mm_Assets.ammo_HEAT.VisualModel);
            ammo_dm12_vis.name = "DM12 visual";
            ammo_dm12.VisualModel = ammo_dm12_vis;
            ammo_dm12.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_dm12;
            ammo_dm12.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_dm12;

            Util.CacheAmmo(ammo_dm13);
            Util.CacheAmmo(ammo_dm23);
            Util.CacheAmmo(ammo_dm33);
            Util.CacheAmmo(ammo_dm43k);
            Util.CacheAmmo(ammo_dm12);
            Util.CacheAmmo(ammo_dm12a1);         
            Util.CacheAmmo(ammo_dm12a1k);

            ap = new Dictionary<string, AmmoClipCodexScriptable>()
            {
                ["DM13"] = clip_codex_dm13,
                ["DM23"] = clip_codex_dm23,
                ["DM33"] = clip_codex_dm33,
                ["DM43K"] = clip_codex_dm43k,
            };

            heat = new Dictionary<string, AmmoClipCodexScriptable>()
            {
                ["DM12A1K"] = clip_codex_dm12a1k,
                ["DM12A1"] = clip_codex_dm12a1,
                ["DM12"] = clip_codex_dm12,
            };
        }
    }
}
