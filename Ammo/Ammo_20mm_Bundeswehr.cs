using GHPC.Effects;
using GHPC.Weaponry;
using GHPC.Weapons;
using MelonLoader;
using ModUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fulda1989
{
    public class Ammo_20mm_Bundeswehr : ModUtil.Module
    {
        public static Dictionary<string, AmmoClipCodexScriptable> he;

        public static AmmoClipCodexScriptable clip_codex_dm81;
        public static AmmoType.AmmoClip clip_dm81;
        public static AmmoCodexScriptable ammo_codex_dm81;
        public static AmmoType ammo_dm81;
        public static AmmoClipCodexScriptable clip_codex_dm51a2;
        public static AmmoType.AmmoClip clip_dm51a2;
        public static AmmoCodexScriptable ammo_codex_dm51a2;
        public static AmmoType ammo_dm51a2;
        public static GameObject ammo_dm51a2_vis = null;
        public static GameObject ammo_dm81_vis = null;

        public override void UnloadDynamicAssets()
        {
            GameObject.DestroyImmediate(ammo_dm51a2_vis);
            GameObject.DestroyImmediate(ammo_dm81_vis);
        }
        public override void LoadDynamicAssets()
        {
            //// DM51A2 (RH202) HEI-T
            //ammo_dm51a2 = new AmmoType();
            //Util.ShallowCopy(ammo_dm51a2, Bundeswehr_20mm_Assets.ammo_HEI);
            //ammo_dm51a2.Name = "(20MM) DM51A2 HE-T";
            //ammo_dm51a2.Caliber = 20;
            //ammo_dm51a2.RhaPenetration = 12;
            //ammo_dm51a2.ImpactAudio = GHPC.Audio.ImpactAudioType.AutocannonExplosive;
            //ammo_dm51a2.TntEquivalentKg = 0.0213f;
            //ammo_dm51a2.MuzzleVelocity = 1055;
            //ammo_dm51a2.Mass = 0.1f;
            //ammo_dm51a2.SpallMultiplier = 0.1f;
            //ammo_dm51a2.CertainRicochetAngle = 10;
            //ammo_dm51a2.ShatterOnRicochet = false;
            //ammo_dm51a2.AlwaysProduceBlast = false;
            //ammo_dm51a2.DetonateSpallCount = 10;
            //ammo_dm51a2.SectionalArea = 0.0003f;
            //ammo_dm51a2.CachedIndex = -1;
            //ammo_dm51a2.VisualType = LiveRoundMarshaller.LiveRoundVisualType.APDS;
            //ammo_dm51a2.ShortName = AmmoType.AmmoShortName.He;
            //ammo_dm51a2.ImpactEffectDescriptor.HasImpactEffect = true;
            //ammo_dm51a2.ImpactEffectDescriptor.ImpactCategory = ParticleEffectsManager.Category.HighExplosive;
            //ammo_dm51a2.ImpactDecalDescriptor.HasImpactDecal = true;
            //ammo_dm51a2.ImpactDecalDescriptor.DecalCategory = ImpactDecalsManager.DecalCategory.Explosion;

            //Util.Coalesce(ref ammo_codex_dm51a2);
            //ammo_codex_dm51a2.AmmoType = ammo_dm51a2;
            //ammo_codex_dm51a2.name = "ammo_dm51a2";

            //clip_dm51a2 = new AmmoType.AmmoClip();
            //clip_dm51a2.Capacity = 1;
            //clip_dm51a2.Name = "DM51A2 HE-T";
            //clip_dm51a2.MinimalPattern = new AmmoCodexScriptable[1];
            //clip_dm51a2.MinimalPattern[0] = ammo_codex_dm51a2;

            //Util.Coalesce(ref clip_codex_dm51a2);
            //clip_codex_dm51a2.name = "clip_dm51a2";
            //clip_codex_dm51a2.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[2];
            //clip_codex_dm51a2.ClipType = clip_dm51a2;

            // DM81 HEI-T
            ammo_dm81 = new AmmoType();
            Util.ShallowCopy(ammo_dm81, Bundeswehr_20mm_Assets.ammo_HEI);
            ammo_dm81.Name = "(20MM) DM81 HEI-T";
            ammo_dm81.Caliber = 20;
            ammo_dm81.RhaPenetration = 16;
            ammo_dm81.ImpactAudio = GHPC.Audio.ImpactAudioType.AutocannonExplosive;
            ammo_dm81.TntEquivalentKg = 0.03f;
            ammo_dm81.MuzzleVelocity = 1055;
            ammo_dm81.Mass = 0.12f;
            ammo_dm81.SpallMultiplier = 0.15f;
            ammo_dm81.CertainRicochetAngle = 10;
            ammo_dm81.ShatterOnRicochet = false;
            ammo_dm81.AlwaysProduceBlast = false;
            ammo_dm81.DetonateSpallCount = 15;
            ammo_dm81.SectionalArea = 0.0003f;
            ammo_dm81.CachedIndex = -1;
            ammo_dm81.VisualType = LiveRoundMarshaller.LiveRoundVisualType.APDS;
            ammo_dm81.ShortName = AmmoType.AmmoShortName.He;
            ammo_dm81.Category = AmmoType.AmmoCategory.Explosive;
            ammo_dm81.ImpactEffectDescriptor.HasImpactEffect = true;
            ammo_dm81.ImpactEffectDescriptor.ImpactCategory = ParticleEffectsManager.Category.HighExplosive;
            ammo_dm81.ImpactDecalDescriptor.HasImpactDecal = true;
            ammo_dm81.ImpactDecalDescriptor.DecalCategory = ImpactDecalsManager.DecalCategory.Explosion;

            Util.Coalesce(ref ammo_codex_dm81);
            ammo_codex_dm81.AmmoType = ammo_dm81;
            ammo_codex_dm81.name = "ammo_dm81";

            clip_dm81 = new AmmoType.AmmoClip();
            clip_dm81.Capacity = 1;
            clip_dm81.Name = "DM81 HEI-T";
            clip_dm81.MinimalPattern = new AmmoCodexScriptable[1];
            clip_dm81.MinimalPattern[0] = ammo_codex_dm81;

            Util.Coalesce(ref clip_codex_dm81);
            clip_codex_dm81.name = "clip_dm81";
            clip_codex_dm81.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[2];
            clip_codex_dm81.ClipType = clip_dm81;

            Util.CacheAmmo(ammo_dm81);

            he = new Dictionary<string, AmmoClipCodexScriptable>()
            {
                ["DM51A2"] = clip_codex_dm51a2,
                ["DM81"] = clip_codex_dm81,
            };
        }
    }
}
