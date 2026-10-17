using GHPC.Mission;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Fulda1989
{
    internal static class ArmyBinocularLookupFix
    {
        public static void EnsureSovietEntries(params string[] friendlyNames)
        {
            UnitPrefabLookupScriptable lookup = Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>().FirstOrDefault();
            ArmyBasicInfoScriptable ussr = Resources.FindObjectsOfTypeAll<ArmyBasicInfoScriptable>().FirstOrDefault(o => o.name == "USSR");

            if (lookup == null)
            {
                MelonLogger.Error("[BINOCULAR LOOKUP] UnitPrefabLookupScriptable not found.");
                return;
            }

            if (ussr == null)
            {
                MelonLogger.Error("[BINOCULAR LOOKUP] USSR ArmyBasicInfoScriptable not found.");
                return;
            }

            List<UnitPrefabLookupScriptable.UnitPrefabMetadata> units = lookup.AllUnits.ToList();

            AssetReference dummyRef = new AssetReference();
            bool changed = false;

            foreach (string friendlyName in friendlyNames.Distinct())
            {
                if (string.IsNullOrWhiteSpace(friendlyName))
                    continue;

                int index = units.FindIndex(o => string.Equals(o.FriendlyName, friendlyName, StringComparison.Ordinal));

                if (index >= 0)
                {
                    var existing = units[index];

                    if (existing.Army != ussr)
                    {
                        existing.Army = ussr;
                        units[index] = existing;
                        changed = true;

                        MelonLogger.Msg($"[BINOCULAR LOOKUP] Corrected army for " + $"{friendlyName} -> USSR.");
                    }
                    else
                    {
                        MelonLogger.Msg($"[BINOCULAR LOOKUP] Existing USSR entry is valid: " + $"{friendlyName}.");
                    }

                    continue;
                }

                units.Add(new UnitPrefabLookupScriptable.UnitPrefabMetadata()
                    {
                        AllowInCustomizer = false,
                        Name = "",
                        PrefabReference = dummyRef,
                        BaseAmmoClipsReference = dummyRef,
                        Army = ussr,
                        FriendlyName = friendlyName
                    });

                changed = true;

                MelonLogger.Msg($"[BINOCULAR LOOKUP] Added USSR lookup entry: " + $"{friendlyName}.");
            }

            if (changed)
            {
                lookup.AllUnits = units.ToArray();

                MelonLogger.Msg("[BINOCULAR LOOKUP] Army lookup updated successfully.");
            }
        }
    }
}