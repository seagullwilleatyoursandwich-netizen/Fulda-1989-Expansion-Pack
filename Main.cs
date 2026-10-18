using FMOD;
using Fulda1989;
using GHPC;
using GHPC.Audio;
using GHPC.Camera;
using GHPC.Mission;
using GHPC.Player;
using GHPC.State;
using GHPC.Vehicle;
using MelonLoader;
using ModUtil;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[assembly: MelonInfo(typeof(Mod), "Fulda 1989 | Expansion Pack [Leopard 2A4 + Remodel Update]", "1.4.2A", "ZironTheDerg")]
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

        private int valid_scene_count = 0;
        internal static FMOD.ChannelGroup audio_channel_group;


        public IEnumerator OnPlayerReady(GameState _) 
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
            new HarmonyLib.Harmony("Fulda1989.RH202Ammo").PatchAll();
            //LEO2KNreTracePatch.Install();

            module_manager = new ModuleManager("Fulda1989");
            cfg = MelonPreferences.CreateCategory("Fulda 1989 | Expansion Pack");
            T64AV.Config(cfg);
            T64BV.Config(cfg);
            M60ERA.Config(cfg);
            BMP2D.Config(cfg);
            LEO2A4.Config(cfg);
            LEO2K.Config(cfg);

            var cor_system = FMODUnity.RuntimeManager.CoreSystem;

            cor_system.createChannelGroup("master", out audio_channel_group);
            audio_channel_group.setVolumeRamp(true);
            audio_channel_group.setMode(MODE._3D_WORLDRELATIVE);

            module_manager.Add("SharedAssets", new SharedAssets());
            module_manager.Add("LEO2K_Assets", new LEO2K_Assets());
            module_manager.Add("Ammo_120mm_assets_Leo2k", new Bundeswehr_120mm_Assets());
            module_manager.Add("Ammo_20mm_assets_Leo2k", new Bundeswehr_20mm_Assets());
            module_manager.Add("AMMO_125MM", new Ammo_125mm());
            module_manager.Add("Ammo_105mm_usa", new Ammo_120mm_USA());
            module_manager.Add("Ammo_120mm_Leo2k", new Ammo_120mm_Bundeswehr());
            module_manager.Add("Ammo_20mm_Leo2k", new Ammo_20mm_Bundeswehr());
            module_manager.Add("M60_Assets", new M60_Assets());
            module_manager.Add("BMP2D_Assets", new BMP2D_Assets());
            module_manager.Add("T64Assets", new T64Assets());
            module_manager.Add("T64AV", new T64AV());
            module_manager.Add("T64BV", new T64BV());
            module_manager.Add("M60ERA", new M60ERA());
            module_manager.Add("LEO2A4", new LEO2A4());
            module_manager.Add("LEO2K", new LEO2K());
            module_manager.Add("BMP-2D", new BMP2D());
            module_manager.Add("SuperFCS", new SuperFCS());
            module_manager.Add("PactThermal", new PactThermal());
            module_manager.Add("1A40", new FireControlSystem1A40());
            module_manager.Add("BOM", new BOM());

        }

        public IEnumerator MissionUnitsLoaded(GameState _)
        {
            UnitSpawner.Instance.SpawnUnit("LEO2K", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "LEO2K",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1231.95f, 23.5001f, 1496.691f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            UnitSpawner.Instance.SpawnUnit("LEO2A4", new GHPC.Mission.Data.UnitMetaData()
                {
                    Name = "LEO2A4",
                    Allegiance = Faction.Neutral,
                    Position = new Vector3(1233.509f, 23.631f, 1488.909f),
                    Rotation = Quaternion.Euler(359.3389f, 263.3187f, 359.9356f),
                    UnitType = UnitType.GroundVehicle,
                }
            );
            yield break;
        }
        private static IEnumerator AddVicsToCampaignRoster()
        {
            TheaterMetaScriptable theaterMeta = null;

            while (theaterMeta == null)
            {
                theaterMeta = Resources.FindObjectsOfTypeAll<TheaterMetaScriptable>().FirstOrDefault();

                if (theaterMeta == null)
                    yield return null;
            }

            if (theaterMeta.AllTheaters == null || theaterMeta.AllTheaters.Length == 0)
            {
                MelonLogger.Error("[VIC CAMPAIGN] No theater data exists.");
                yield break;
            }

            TheaterFactionMetadata usa = theaterMeta.AllTheaters[0].Factions[0];

            if (usa == null || usa.AllUnits == null)
            {
                MelonLogger.Error("[VIC CAMPAIGN] USA faction or AllUnits is null.");
                yield break;
            }

            bool enableLeo2K = LEO2K.PatchEnabled;
            bool enableLeo2A4 = LEO2A4.PatchEnabled;

            var units = usa.AllUnits.ToList();

            // Remove custom campaign entries when their patches are disabled.
            units.RemoveAll(x => x != null && ((!enableLeo2K && x.Key == "LEO2K") || (!enableLeo2A4 && x.Key == "LEO2A4")));

            bool addLeo2K = enableLeo2K && !units.Any(x => x != null && x.Key == "LEO2K");

            bool addLeo2A4 = enableLeo2A4 && !units.Any(x => x != null && x.Key == "LEO2A4");

            if (addLeo2K || addLeo2A4)
            {
                TheaterUnitMetadata m1 = units.FirstOrDefault(x => x != null && x.Key == "M1");

                if (m1 == null)
                {
                    usa.AllUnits = units.ToArray();
                    MelonLogger.Error("[VIC CAMPAIGN] Could not find M1 roster entry for logistics curve.");
                    yield break;
                }

                if (addLeo2K)
                {
                    units.Add(new TheaterUnitMetadata
                    {
                        Key = "LEO2K",
                        FriendlyName = "Leopard 2K",
                        Type = UnitType.GroundVehicle,
                        Class = UnitClass.Tank,
                        Tier = EffectivenessTier.FirstLine,
                        SpawnRatio = 1.25f,
                        BlockUntilMissionsCompleted = 0,
                        Prevalence = 1.1f,
                        LogisticsModifierCurve = null
                    });
                }

                if (addLeo2A4)
                {
                    units.Add(new TheaterUnitMetadata
                    {
                        Key = "LEO2A4",
                        FriendlyName = "Leopard 2A4 (1988)",
                        Type = UnitType.GroundVehicle,
                        Class = UnitClass.Tank,
                        Tier = EffectivenessTier.FirstLine,
                        SpawnRatio = 0.9f,
                        BlockUntilMissionsCompleted = 3,
                        Prevalence = 1.1f,
                        LogisticsModifierCurve = null
                    });
                }
            }

            usa.AllUnits = units.ToArray();
            yield break;
        }
        public override void OnApplicationQuit()
        {
            LEO2KStandaloneRegistration.Dispose();
            LEO2A4StandaloneRegistration.Dispose();
            //T90StandaloneRegistration.Dispose();
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (LEO2K.PatchEnabled)
                LEO2KStandaloneRegistration.TryRegister();
            else
                LEO2KStandaloneRegistration.Dispose();

            if (LEO2A4.PatchEnabled)
                LEO2A4StandaloneRegistration.TryRegister();
            else
                LEO2A4StandaloneRegistration.Dispose();

            //T90StandaloneRegistration.TryRegister();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonCoroutines.Start(AddVicsToCampaignRoster());

            if (Util.MenuScreens.Contains(sceneName))
            {
                module_manager.LoadAllStaticAssets();
            }

            if (sceneName == "TR01_showcase")
            {
                StateController.RunOrDefer(GameState.MissionUnitsLoaded, new GameStateEventHandler(MissionUnitsLoaded), GameStatePriority.Medium);
            }

            if (Util.MenuScreensWithLoader.Contains(sceneName)) return;
            valid_scene_count++;

            if (valid_scene_count == 1)
            {
                module_manager.UnloadAllDynamicAssets();
            }

            if (valid_scene_count == 2)
            {
                module_manager.LoadAllDynamicAssets();
                StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(OnPlayerReady), GameStatePriority.Highest);
                StateController.RunOrDefer(GameState.GameReady, new GameStateEventHandler(Ammo_125mm.CreateCompositeOptimizationsDeferred), GameStatePriority.Low);

                FuldaEra.Init();
                M60ERA.Init();
                T64AV.Init();
                T64BV.Init();
                BMP2D.Init();
                LEO2A4.Init();
                LEO2K.Init();

                valid_scene_count = 0;
            }
        }
    }
}
