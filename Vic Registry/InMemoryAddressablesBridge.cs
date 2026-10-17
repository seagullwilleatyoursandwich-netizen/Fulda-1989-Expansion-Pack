using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Fulda1989
{
    internal sealed class InMemoryAddressablesBridge : IDisposable
    {
        private readonly string _key;
        private readonly GameObject _prefab;
        private readonly PrefabLocator _locator;
        private readonly PrefabProvider _provider;
        private bool _installed;

        public InMemoryAddressablesBridge(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException("prefab");
            _prefab = prefab;
            _key = Guid.NewGuid().ToString("N");
            _provider = new PrefabProvider(_key, prefab);
            _locator = new PrefabLocator(_key, _provider.ProviderId);
        }

        public AssetReference CreateAssetReference()
        {
            Install();
            return new AssetReference(_key);
        }

        private void Install()
        {
            if (_installed) return;
            Addressables.ResourceManager.ResourceProviders.Add(_provider);
            Addressables.AddResourceLocator(_locator);
            _installed = true;
        }

        public void Dispose()
        {
            if (!_installed) return;
            try { Addressables.RemoveResourceLocator(_locator); } catch { }
            try { Addressables.ResourceManager.ResourceProviders.Remove(_provider); } catch { }
            _installed = false;
        }

        private sealed class PrefabLocator : IResourceLocator
        {
            private readonly string _key;
            private readonly IResourceLocation _location;
            private readonly IList<object> _keys;

            public PrefabLocator(string key, string providerId)
            {
                _key = key;
                _location = new PrefabLocation(key, providerId);
                _keys = new List<object> { key };
            }

            public string LocatorId { get { return "RTR.Leopard2K.InMemoryLocator"; } }
            public IEnumerable<object> Keys { get { return _keys; } }

            public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
            {
                locations = null;
                if (key == null || !string.Equals(key.ToString(), _key, StringComparison.Ordinal)) return false;
                if (type != null && type != typeof(object) && !type.IsAssignableFrom(typeof(GameObject))) return false;
                locations = new List<IResourceLocation> { _location };
                return true;
            }
        }

        private sealed class PrefabLocation : IResourceLocation
        {
            private static readonly IList<IResourceLocation> NoDependencies = new List<IResourceLocation>();
            public PrefabLocation(string key, string providerId)
            {
                PrimaryKey = key;
                InternalId = key;
                ProviderId = providerId;
            }

            public string InternalId { get; private set; }
            public string ProviderId { get; private set; }
            public IList<IResourceLocation> Dependencies { get { return NoDependencies; } }
            public int DependencyHashCode { get { return 0; } }
            public object Data { get { return null; } }
            public string PrimaryKey { get; private set; }
            public Type ResourceType { get { return typeof(GameObject); } }
            public bool HasDependencies { get { return false; } }
            public int Hash(Type resultType) { return PrimaryKey.GetHashCode(); }
        }

        private sealed class PrefabProvider : IResourceProvider
        {
            private readonly string _key;
            private readonly GameObject _prefab;
            private readonly string _providerId;

            public PrefabProvider(string key, GameObject prefab)
            {
                _key = key;
                _prefab = prefab;
                _providerId = "RTR.Leopard2K.InMemoryProvider." + Guid.NewGuid().ToString("N");
            }

            public string ProviderId { get { return _providerId; } }
            public ProviderBehaviourFlags BehaviourFlags { get { return ProviderBehaviourFlags.None; } }
            public Type GetDefaultType(IResourceLocation location) { return typeof(GameObject); }

            public bool CanProvide(Type t, IResourceLocation location)
            {
                return location != null && string.Equals(location.PrimaryKey, _key, StringComparison.Ordinal) && (t == null || t == typeof(object) || t.IsAssignableFrom(typeof(GameObject)));
            }

            public void Provide(ProvideHandle provideHandle)
            {
                Type wanted = provideHandle.Type;
                if (_prefab == null || (wanted != null && wanted != typeof(object) && !wanted.IsAssignableFrom(typeof(GameObject))))
                {
                    provideHandle.Complete<object>(null, false, new InvalidOperationException("RTR in-memory prefab provider received incompatible request."));
                    return;
                }
                provideHandle.Complete<GameObject>(_prefab, true, null);
            }

            public void Release(IResourceLocation location, object asset)
            {
                // The AssetBundle owns the template. GHPC instances are separate Unity clones.
            }
        }
    }
}