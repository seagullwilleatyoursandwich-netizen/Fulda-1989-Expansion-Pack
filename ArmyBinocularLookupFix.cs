
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
        private const string NamePrefix = "FULDA1989_BINOCULAR_";

        public static void EnsureSovietEntries(params string[] friendlyNames)
        {
            UnitPrefabLookupScriptable lookup = Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>().FirstOrDefault();

            ArmyBasicInfoScriptable ussr = Resources.FindObjectsOfTypeAll<ArmyBasicInfoScriptable>().FirstOrDefault(o => o != null && o.name == "USSR");

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

            List<UnitPrefabLookupScriptable.UnitPrefabMetadata> units = lookup.AllUnits != null ? lookup.AllUnits.ToList() : new List<UnitPrefabLookupScriptable.UnitPrefabMetadata>();

            AssetReference dummyRef = new AssetReference();
            bool changed = false;

            foreach (string friendlyName in friendlyNames.Distinct())
            {
                if (string.IsNullOrWhiteSpace(friendlyName))
                    continue;

                int index = units.FindIndex(o => o != null && string.Equals(o.FriendlyName, friendlyName, StringComparison.Ordinal));

                if (index >= 0)
                {
                    var existing = units[index];
                    bool entryChanged = false;

                    if (existing.Army != ussr)
                    {
                        existing.Army = ussr;
                        entryChanged = true;

                        MelonLogger.Msg($"[BINOCULAR LOOKUP] Corrected army for {friendlyName} -> USSR.");
                    }

                    // Repair any existing placeholder with a blank internal name.
                    if (string.IsNullOrWhiteSpace(existing.Name))
                    {
                        existing.Name = BuildUniqueName(units, friendlyName, index);
                        entryChanged = true;

                        MelonLogger.Msg($"[BINOCULAR LOOKUP] Repaired internal name for {friendlyName}: {existing.Name}");
                    }

                    if (entryChanged)
                    {
                        units[index] = existing;
                        changed = true;
                    }
                    else
                    {
                        MelonLogger.Msg(
                            $"[BINOCULAR LOOKUP] Existing USSR entry is valid: {friendlyName}.");
                    }

                    continue;
                }

                string uniqueName = BuildUniqueName(units, friendlyName, -1);

                units.Add(new UnitPrefabLookupScriptable.UnitPrefabMetadata()
                {
                    AllowInCustomizer = false,
                    Name = uniqueName,
                    PrefabReference = dummyRef,
                    BaseAmmoClipsReference = dummyRef,
                    Army = ussr,
                    FriendlyName = friendlyName
                });

                changed = true;

                MelonLogger.Msg($"[BINOCULAR LOOKUP] Added USSR lookup entry: {friendlyName} ({uniqueName}).");
            }

            if (changed)
            {
                lookup.AllUnits = units.ToArray();

                MelonLogger.Msg("[BINOCULAR LOOKUP] Army lookup updated successfully.");
            }
        }

        private static string BuildUniqueName(List<UnitPrefabLookupScriptable.UnitPrefabMetadata> units, string friendlyName, int ignoredIndex)
        {
            string baseName = NamePrefix + friendlyName;
            string candidate = baseName;
            int suffix = 2;

            while (true)
            {
                bool collision = false;

                for (int i = 0; i < units.Count; i++)
                {
                    if (i == ignoredIndex)
                        continue;

                    var entry = units[i];

                    if (entry != null &&
                        string.Equals(entry.Name, candidate, StringComparison.Ordinal))
                    {
                        collision = true;
                        break;
                    }
                }

                if (!collision)
                    return candidate;

                candidate = baseName + "_" + suffix++;
            }
        }
    }
}
