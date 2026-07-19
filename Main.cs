using System.Linq;
using MelonLoader;
using UnityEngine;
using GHPC.State;
using System.Collections;
using MelonLoader.Utils;
using System.IO;
using GHPC.Audio;
using GHPC.Player;
using GHPC.Camera;
using FMOD;
using GHPC.Vehicle;
using Fulda1989;
using ModUtil;
using FMODUnity;

[assembly: MelonInfo(typeof(Mod), "Fulda 1989 | Expansion Pack", "1.0.0", "ZironTheDerg")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace Fulda1989
{
    public class Mod : MelonMod
    {
        private static ModuleManager module_manager;
        internal static Vehicle[] vics;
        internal static MelonPreferences_Category cfg;

        private GameObject game_manager;
        internal static AudioSettingsManager audio_settings_manager;
        internal static PlayerInput player_manager;
        internal static CameraManager camera_manager;

        internal static FMOD.ChannelGroup audio_channel_group;

        public IEnumerator OnGameReady(GameState _) 
        {
            game_manager = GameObject.Find("_APP_GHPC_");
            audio_settings_manager = game_manager.GetComponent<AudioSettingsManager>();
            player_manager = game_manager.GetComponent<PlayerInput>();
            camera_manager = game_manager.GetComponent<CameraManager>();
            vics = GameObject.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);

            module_manager.LoadAllDynamicAssets();
            Ammo_125mm.CreateCompositeOptimizations();

            yield break;
        }

        public override void OnInitializeMelon()
        {
            module_manager = new ModuleManager("Fulda1989");
            cfg = MelonPreferences.CreateCategory("Fulda 1989 | Expansion Pack");
            T64AV.Config(cfg);
            T64BV.Config(cfg);

            var cor_system = FMODUnity.RuntimeManager.CoreSystem;

            cor_system.createChannelGroup("master", out audio_channel_group);
            audio_channel_group.setVolumeRamp(true);
            audio_channel_group.setMode(MODE._3D_WORLDRELATIVE);

            module_manager.Add("SharedAssets", new SharedAssets());
            module_manager.Add("AMMO_125MM", new Ammo_125mm());
            module_manager.Add("T64Assets", new T64Assets());
            module_manager.Add("T64AV", new T64AV());
            module_manager.Add("T64BV", new T64BV());
            module_manager.Add("SuperFCS", new SuperFCS());
            module_manager.Add("PactThermal", new PactThermal());
            module_manager.Add("1A40", new FireControlSystem1A40());
            module_manager.Add("BOM", new BOM());

        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            module_manager.UnloadAllDynamicAssets();

            if (sceneName == "MainMenu2_Scene" || sceneName == "MainMenu2-1_Scene" || sceneName == "t64_menu")
            {
                module_manager.LoadAllStaticAssets();
                AssetUtil.ReleaseVanillaAssets();
            }

            //TODO why is this needed?        
            if (sceneName == "GT01_Beginers_Luck") 
            {
                AssetUtil.LoadVanillaVehicle("T72M");
            }

            if (Util.menu_screens.Contains(sceneName)) return;

            StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(OnGameReady), GameStatePriority.Medium);

            PactEra.Init();
            T64AV.Init();
            T64BV.Init();
        }
    }
}