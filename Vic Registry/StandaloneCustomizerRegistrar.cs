using System;
using System.Collections.Generic;
using System.Reflection;
using GHPC;
using GHPC.Mission;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Fulda1989
{
    /// <summary>
    /// Generic GHPC standalone vehicle customizer registration helper.
    ///
    /// This ONLY registers an already-valid standalone vehicle prefab in GHPC's
    /// UnitPrefabLookupScriptable/customizer catalog. It does not build the vehicle,
    /// its weapons, crew, physics, armour, cameras, etc.
    ///
    /// Targeted at the same current GHPC managed contract used by RTR:
    /// Unity 2022.3.62f2 / MelonLoader 0.6.1 net35.
    /// </summary>
    public sealed class StandaloneCustomizerRegistrar : IDisposable
    {
        public sealed class VehicleIdentity
        {
            public string UniqueName;
            public string FriendlyName;
            public string VehicleTag;
            public string ArmyUniqueName;
            public string ArmyFriendlyName;
            public UnitClass VehicleClass = UnitClass.Tank;

            public void Validate()
            {
                RequireText(UniqueName, "UniqueName");
                RequireText(FriendlyName, "FriendlyName");
                RequireText(VehicleTag, "VehicleTag");
                RequireText(ArmyUniqueName, "ArmyUniqueName");
                RequireText(ArmyFriendlyName, "ArmyFriendlyName");
            }

            private static void RequireText(string value, string name)
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(name + " cannot be null or empty.", name);
            }
        }

        private sealed class LookupRegistration
        {
            public UnitPrefabLookupScriptable Lookup;
            public UnitPrefabLookupScriptable.UnitPrefabMetadata[] Original;
            public UnitPrefabLookupScriptable.UnitPrefabMetadata Metadata;
        }

        private readonly VehicleIdentity _identity;
        private readonly List<LookupRegistration> _registrations = new List<LookupRegistration>();

        private ArmyBasicInfoScriptable _army;
        private FactionBasicInfoScriptable _nativeBlueFaction;
        private AssetReference _prefabReference;
        private AssetReference _baseAmmoReference;
        private bool _committed;

        public StandaloneCustomizerRegistrar(VehicleIdentity identity)
        {
            if (identity == null) throw new ArgumentNullException("identity");
            identity.Validate();
            _identity = identity;
        }

        /// Registers the vehicle in every currently loaded native GHPC unit lookup.
        /// prefabReference must resolve to your OWN standalone vehicle prefab.
        /// baseAmmoReference must resolve to your OWN valid VehicleAmmoListScriptable.
       
        public void Register(AssetReference prefabReference, AssetReference baseAmmoReference)
        {
            if (_committed)
                throw new InvalidOperationException("Vehicle is already registered: " + _identity.UniqueName);
            if (prefabReference == null)
                throw new ArgumentNullException("prefabReference");
            if (baseAmmoReference == null)
                throw new ArgumentNullException("baseAmmoReference");

            UnitPrefabLookupScriptable[] lookups = Resources.FindObjectsOfTypeAll<UnitPrefabLookupScriptable>();
            if (lookups == null || lookups.Length == 0)
                throw new InvalidOperationException("GHPC UnitPrefabLookupScriptable is not loaded yet.");

            List<UnitPrefabLookupScriptable> usable = new List<UnitPrefabLookupScriptable>();

            for (int i = 0; i < lookups.Length; i++)
            {
                UnitPrefabLookupScriptable lookup = lookups[i];
                if (lookup == null || lookup.AllUnits == null) continue;

                if (ContainsUniqueName(lookup.AllUnits, _identity.UniqueName))
                    throw new InvalidOperationException(
                        "A vehicle with this GHPC unique name is already registered: " + _identity.UniqueName);

                usable.Add(lookup);
            }

            if (usable.Count == 0)
                throw new InvalidOperationException("No usable GHPC unit lookup is currently loaded.");

            _nativeBlueFaction = ResolveNativeBlueFaction(usable);
            if (_nativeBlueFaction == null)
                throw new InvalidOperationException("Could not safely resolve GHPC's native Blue faction metadata.");

            _army = BuildArmyInfo(_nativeBlueFaction);
            _prefabReference = prefabReference;
            _baseAmmoReference = baseAmmoReference;

            try
            {
                for (int i = 0; i < usable.Count; i++)
                    RegisterIntoLookup(usable[i]);

                _committed = true;
            }
            catch
            {
                RollbackTransaction();
                DestroyOwnedArmy();
                _prefabReference = null;
                _baseAmmoReference = null;
                throw;
            }
        }

        public void Dispose()
        {
            Unregister();
        }

        public void Unregister()
        {
            if (_committed)
                RemoveRegisteredMetadata();
            else
                RollbackTransaction();

            _committed = false;
            _registrations.Clear();
            _prefabReference = null;
            _baseAmmoReference = null;

            DestroyOwnedArmy();

            // GHPC owns this object. Never destroy or modify it.
            _nativeBlueFaction = null;
        }

        private void RegisterIntoLookup(UnitPrefabLookupScriptable lookup)
        {
            UnitPrefabLookupScriptable.UnitPrefabMetadata[] original = lookup.AllUnits;
            UnitPrefabLookupScriptable.UnitPrefabMetadata metadata = BuildMetadata();

            UnitPrefabLookupScriptable.UnitPrefabMetadata[] next =
                new UnitPrefabLookupScriptable.UnitPrefabMetadata[original.Length + 1];

            Array.Copy(original, next, original.Length);
            next[original.Length] = metadata;

            LookupRegistration state = new LookupRegistration();
            state.Lookup = lookup;
            state.Original = original;
            state.Metadata = metadata;
            _registrations.Add(state);

            lookup.AllUnits = next;

            ValidateCatalogAppend(state);
            ValidateNativeLookupApi(state);
        }

        private UnitPrefabLookupScriptable.UnitPrefabMetadata BuildMetadata()
        {
            UnitPrefabLookupScriptable.UnitPrefabMetadata metadata =
                new UnitPrefabLookupScriptable.UnitPrefabMetadata();

            // Internal key used by GHPC.
            metadata.Name = _identity.UniqueName;

            // Human-readable vehicle name shown by GHPC.
            metadata.FriendlyName = _identity.FriendlyName;

            metadata.PrefabReference = _prefabReference;
            metadata.BaseAmmoClipsReference = _baseAmmoReference;
            metadata.UseDecalLayout = false;
            metadata.Army = _army;
            metadata.Class = _identity.VehicleClass;
            metadata.AlternativeClasses = new UnitClass[] { _identity.VehicleClass };

            // Required for normal GHPC customizer discovery.
            metadata.AllowInCustomizer = true;

            return metadata;
        }

        private ArmyBasicInfoScriptable BuildArmyInfo(FactionBasicInfoScriptable nativeBlueFaction)
        {
            ArmyBasicInfoScriptable army = ScriptableObject.CreateInstance<ArmyBasicInfoScriptable>();
            army.name = _identity.ArmyUniqueName;
            army.hideFlags = HideFlags.HideAndDontSave;
            army.Info = new ArmyBasicInfo();
            army.Info.UniqueName = _identity.ArmyUniqueName;
            army.Info.FriendlyName = _identity.ArmyFriendlyName;

            // This is the short tag shown by GHPC for this vehicle/army identity.
            // Examples: CH1, L2A7, T80U, etc.
            army.Info.Abbreviation = _identity.VehicleTag;

            // Reuse GHPC's native Blue faction object rather than creating a fake faction taxonomy.
            army.Info.Faction = nativeBlueFaction;
            return army;
        }

        private static FactionBasicInfoScriptable ResolveNativeBlueFaction(
            List<UnitPrefabLookupScriptable> lookups)
        {
            for (int l = 0; l < lookups.Count; l++)
            {
                UnitPrefabLookupScriptable.UnitPrefabMetadata[] all = lookups[l].AllUnits;

                for (int i = 0; i < all.Length; i++)
                {
                    UnitPrefabLookupScriptable.UnitPrefabMetadata entry = all[i];
                    if (entry == null || entry.Army == null || entry.Army.Info == null) continue;

                    FactionBasicInfoScriptable faction = entry.Army.Info.Faction;
                    if (faction == null || faction.Info == null) continue;

                    if (faction.Info.Allegiance == Faction.Blue)
                        return faction;
                }
            }

            return null;
        }

        private static void ValidateCatalogAppend(LookupRegistration state)
        {
            UnitPrefabLookupScriptable.UnitPrefabMetadata[] current = state.Lookup.AllUnits;

            if (current == null || current.Length != state.Original.Length + 1)
                throw new InvalidOperationException("Standalone vehicle registration changed the GHPC catalog length unexpectedly.");

            for (int i = 0; i < state.Original.Length; i++)
            {
                if (!object.ReferenceEquals(current[i], state.Original[i]))
                    throw new InvalidOperationException("Standalone vehicle registration modified or reordered a native GHPC entry.");
            }

            if (!object.ReferenceEquals(current[current.Length - 1], state.Metadata))
                throw new InvalidOperationException("Standalone vehicle metadata was not appended to the GHPC catalog.");
        }

        private void ValidateNativeLookupApi(LookupRegistration state)
        {
            UnitPrefabLookupScriptable lookup = state.Lookup;

            object data = Call(lookup, "GetUnitDataByKey", _identity.UniqueName);
            if (data != null && !object.ReferenceEquals(data, state.Metadata))
                throw new InvalidOperationException("GHPC GetUnitDataByKey returned the wrong vehicle metadata.");

            object friendlyValue = Call(lookup, "GetFriendlyName", _identity.UniqueName);
            if (friendlyValue != null)
            {
                string friendly = friendlyValue.ToString();
                if (!string.Equals(friendly, _identity.FriendlyName, StringComparison.Ordinal))
                    throw new InvalidOperationException("GHPC GetFriendlyName returned the wrong standalone vehicle name.");
            }

            object abbreviationValue = Call(lookup, "GetFactionAbbreviation", _identity.UniqueName);
            if (abbreviationValue != null)
            {
                string abbreviation = abbreviationValue.ToString();
                if (!string.Equals(abbreviation, _identity.VehicleTag, StringComparison.Ordinal))
                    throw new InvalidOperationException("GHPC GetFactionAbbreviation returned the wrong standalone vehicle tag.");
            }

            if (!state.Metadata.AllowInCustomizer)
                throw new InvalidOperationException("Standalone vehicle metadata is not customizer-enabled.");

            if (state.Metadata.Class != _identity.VehicleClass)
                throw new InvalidOperationException("Standalone vehicle metadata has the wrong GHPC unit class.");

            if (state.Metadata.Army == null || state.Metadata.Army.Info == null || state.Metadata.Army.Info.Faction == null || state.Metadata.Army.Info.Faction.Info == null || state.Metadata.Army.Info.Faction.Info.Allegiance != Faction.Blue)
            {
                throw new InvalidOperationException("Standalone vehicle army/faction metadata is incomplete.");
            }
        }

        private void RollbackTransaction()
        {
            for (int i = _registrations.Count - 1; i >= 0; i--)
            {
                LookupRegistration state = _registrations[i];
                if (state.Lookup != null && state.Original != null)
                    state.Lookup.AllUnits = state.Original;
            }

            _registrations.Clear();
        }

        private void RemoveRegisteredMetadata()
        {
            for (int r = 0; r < _registrations.Count; r++)
            {
                LookupRegistration state = _registrations[r];
                if (state.Lookup == null || state.Lookup.AllUnits == null || state.Metadata == null)
                    continue;

                UnitPrefabLookupScriptable.UnitPrefabMetadata[] current = state.Lookup.AllUnits;
                int index = -1;

                for (int i = 0; i < current.Length; i++)
                {
                    if (object.ReferenceEquals(current[i], state.Metadata))
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0) continue;

                UnitPrefabLookupScriptable.UnitPrefabMetadata[] next = new UnitPrefabLookupScriptable.UnitPrefabMetadata[current.Length - 1];

                int target = 0;
                for (int i = 0; i < current.Length; i++)
                {
                    if (i == index) continue;
                    next[target++] = current[i];
                }

                state.Lookup.AllUnits = next;
            }
        }

        private void DestroyOwnedArmy()
        {
            if (_army != null)
            {
                UnityEngine.Object.Destroy(_army);
                _army = null;
            }
        }

        private static bool ContainsUniqueName(UnitPrefabLookupScriptable.UnitPrefabMetadata[] list, string uniqueName)
        {
            if (list == null) return false;

            for (int i = 0; i < list.Length; i++)
            {
                UnitPrefabLookupScriptable.UnitPrefabMetadata item = list[i];
                if (item != null &&
                    string.Equals(item.Name, uniqueName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// Small self-contained reflection helper so that this class does not depend on RTR.ReflectionUtil.
        /// Returns null when a native helper method is absent/inaccessible; structural validation still runs.

        private static object Call(object target, string methodName, object argument)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return null;

            Type scan = target.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            while (scan != null)
            {
                MethodInfo[] methods = scan.GetMethods(flags);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (!string.Equals(method.Name, methodName, StringComparison.Ordinal)) continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 1) continue;

                    Type parameterType = parameters[0].ParameterType;
                    if (argument != null && !parameterType.IsInstanceOfType(argument) && parameterType != typeof(string))
                        continue;

                    try
                    {
                        return method.Invoke(target, new object[] { argument });
                    }
                    catch
                    {
                        return null;
                    }
                }

                scan = scan.BaseType;
            }

            return null;
        }
    }
}