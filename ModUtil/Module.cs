using System.Collections;
using GHPC.State;
using MelonLoader;

namespace ModUtil
{
    public class Module
    {
        private bool static_assets_loaded = false;
        private bool dynamic_assets_loaded = false;
        private bool dynamic_assets_load_pending = false;
        internal string mod_id;
        public string Id { get; set; }

        public bool TryLoadStaticAssets()
        {
            if (static_assets_loaded) return false;

            LoadStaticAssets();

            static_assets_loaded = true;

            return true;
        }

        public bool TryLoadDynamicAssets()
        {
            if (dynamic_assets_loaded || dynamic_assets_load_pending) return false;

            dynamic_assets_load_pending = true;

            try
            {
                StateController.RunOrDefer(LoadState, new GameStateEventHandler(LoadDynamicAssetsDeferred));

                return true;
            }
            catch
            {
                dynamic_assets_load_pending = false;
                throw;
            }
        }

        public bool TryUnloadDynamicAssets()
        {
            if (!dynamic_assets_loaded) return false;

            dynamic_assets_loaded = false;

            UnloadDynamicAssets();

            return true;
        }

        private IEnumerator LoadDynamicAssetsDeferred(GameState _)
        {
            if (dynamic_assets_loaded)
            {
                dynamic_assets_load_pending = false;
                yield break;
            }

            try
            {
                LoadDynamicAssets();
                dynamic_assets_loaded = true;

                MelonLogger.Msg(mod_id + " finished dynamic assets loading from module: " + Id);
            }
            finally
            {
                dynamic_assets_load_pending = false;
            }

            yield break;
        }

        public virtual GameState LoadState => GameState.PlayerReady;
        public virtual void LoadStaticAssets() { }
        public virtual void LoadDynamicAssets() { }

        // ASSETS THAT ARE INSTANTIATED DYNAMICALLY 
        // I.E. USING Object.Instantiate
        // MUST BE DESTROYED!
        public virtual void UnloadDynamicAssets() { }
    }
}
