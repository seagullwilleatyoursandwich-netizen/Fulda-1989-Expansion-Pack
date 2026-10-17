using FuldaExpansion1989.Sights;
using GHPC;
using GHPC.Camera;
using GHPC.Crew;
using GHPC.Equipment.Optics;
using GHPC.Player;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weaponry;
using GHPC.Weapons;
using HarmonyLib;
using MelonLoader;
using ModUtil;
using System;
using System.Reflection;
using UnityEngine;

namespace Fulda1989
{
    public class PeriRh202Follower : MonoBehaviour
    {
        public AimablePlatform rh202Traverse;
        public AimablePlatform rh202Elevation;
        public Transform periTransform;
        public Transform periR12Optic;

        void Update()
        {
            if (rh202Traverse == null || rh202Elevation == null || periTransform == null || periR12Optic == null)
                return;

            float targetY = rh202Traverse.Transform.localEulerAngles.y;
            float currentY = periTransform.localEulerAngles.y;
            float nextY = Mathf.MoveTowardsAngle(currentY, targetY, rh202Traverse.CurrentMaxSpeed * Time.deltaTime);

            float targetX = rh202Elevation.Transform.localEulerAngles.x;
            float currentX = periR12Optic.localEulerAngles.x;
            float nextX = Mathf.MoveTowardsAngle(currentX, targetX, rh202Elevation.CurrentMaxSpeed * Time.deltaTime);

            Vector3 periEuler = periTransform.localEulerAngles;
            periEuler.y = nextY;
            periTransform.localEulerAngles = periEuler;

            Vector3 opticEuler = periR12Optic.localEulerAngles;
            opticEuler.x = nextX;
            periR12Optic.localEulerAngles = opticEuler;
        }
    }

    public class RH202LaserPointCorrection : MonoBehaviour
    {
        public Transform laser;
        public UsableOptic dayOptic;
        public UsableOptic nightOptic;

        public Vector3 preservedSpotDay;
        public Quaternion preservedRotationDay;

        public Vector3 preservedSpotNight;
        public Quaternion preservedRotationNight;

        void Start()
        {
            laser.SetParent(dayOptic.transform, true);
            preservedSpotDay = laser.transform.localPosition;
            preservedRotationDay = laser.transform.localRotation;

            laser.SetParent(nightOptic.transform, true);
            preservedSpotNight = laser.transform.localPosition;
            preservedRotationNight = laser.transform.localRotation;

            laser.SetParent(dayOptic.transform, true);
        }

        void Update()
        {
            if (dayOptic.isActiveAndEnabled)
            {
                laser.SetParent(dayOptic.transform, true);
                laser.transform.SetLocalPositionAndRotation(preservedSpotDay, preservedRotationDay);
            }

            if (nightOptic.isActiveAndEnabled)
            {
                laser.SetParent(nightOptic.transform, true);
                laser.transform.SetLocalPositionAndRotation(preservedSpotNight, preservedRotationNight);
            }
        }
    }

    public class StaticWhenNotInUse : MonoBehaviour
    {
        public FireControlSystem fcs;

        void Update()
        {
            if (fcs == null || PlayerInput.Instance == null || PlayerInput.Instance.CurrentPlayerWeapon == null || PlayerInput.Instance.CurrentPlayerWeapon.FCS == null)
                return;

            fcs.SetStabsActive(fcs.GetInstanceID() == PlayerInput.Instance.CurrentPlayerWeapon.FCS.GetInstanceID());
        }
    }

    public class RH202ReticleBothActive : MonoBehaviour
    {
        public Reticle.ReticleMesh primaryReticle;
        public Reticle.ReticleMesh wfovReticle;

        public void ForceActive()
        {
            if (primaryReticle != null)
            {
                primaryReticle.gameObject.SetActive(true);
                primaryReticle.enabled = true;
            }

            if (wfovReticle != null)
            {
                wfovReticle.gameObject.SetActive(true);
                wfovReticle.enabled = true;
            }
        }

        void LateUpdate()
        {
            ForceActive();
        }
    }

    public static class RH202
    {
        public static GHPC.Weapons.AmmoRack AmmoRack;
        public static AmmoType.AmmoClip APClip;
        public static AmmoType.AmmoClip HEClip;
        public static AmmoType.AmmoClip[] AvailableClips;
        public static CameraSlot AuxSightSlot;
        public static CameraSlot RH202Slot;

        public enum Leopard2KVariant
        {
            K,
            KA1,
            KA2,
            KA3
        }

        public static void Setup(Vehicle vic, Transform rh202Gun, Leopard2KVariant variant)
        {
            if (rh202Gun == null) { MelonLogger.Error("[RH202] RH202 gun is null."); return; }
            if (vic == null) { MelonLogger.Error("[RH202] Vehicle is null."); return; }
            if (rh202Gun.Find("RH202_MarderWeapon") != null) { MelonLogger.Warning("[RH202] RH202_MarderWeapon already exists."); return; }
            if (LEO2K_Assets.marder == null) { MelonLogger.Error("[RH202] Marder donor was not loaded."); return; }

            GameObject marderGun = null;
            foreach (Transform t in LEO2K_Assets.marder.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "20mm Gun MK20") { marderGun = t.gameObject; break; }
            }

            if (marderGun == null) { MelonLogger.Error("[RH202] Could not find vanilla Marder 20mm Gun MK20."); return; }

            FireControlSystem donorFcs = null;
            foreach (FireControlSystem fcs in LEO2K_Assets.marder.GetComponentsInChildren<FireControlSystem>(true))
            {
                if (fcs.name == "FCS and sights") { donorFcs = fcs; break; }
            }

            if (donorFcs == null) { MelonLogger.Error("[RH202] Could not find Marder FCS."); return; }

            Transform leo2kVisualTurret = rh202Gun;
            while (leo2kVisualTurret != null && leo2kVisualTurret.name != "turret")
                leo2kVisualTurret = leo2kVisualTurret.parent;

            if (leo2kVisualTurret == null) { MelonLogger.Error("[RH202] Could not find runtime Leopard 2K turret."); return; }

            Transform gunScripts = leo2kVisualTurret.Find("20MM RIG/RH202_TRAVERSE_PIVOT/20mm_MOUNT/Gun Script");
            if (gunScripts == null) { MelonLogger.Error("[RH202] Could not find runtime RH202 Gun Script."); return; }

            Transform vehicleTurret = vic.transform.Find("LEO1A1A1_rig/HULL/TURRET");
            if (vehicleTurret == null) { MelonLogger.Error("[RH202] Could not find runtime vehicle turret."); return; }

            GameObject fcsClone = UnityEngine.Object.Instantiate(donorFcs.gameObject, gunScripts, false);
            fcsClone.name = "RH202_FCS";
            fcsClone.transform.localPosition = Vector3.zero;
            fcsClone.transform.localRotation = Quaternion.identity;
            fcsClone.transform.localScale = Vector3.one;

            FireControlSystem rh202Fcs = fcsClone.GetComponent<FireControlSystem>();
            if (rh202Fcs == null) { MelonLogger.Error("[RH202] Cloned FCS has no FireControlSystem."); return; }

            GameObject clone = UnityEngine.Object.Instantiate(marderGun, gunScripts, false);
            Transform muzzleIdentity = rh202Gun.Find("gun model/barrel/muzzleID");
            if (muzzleIdentity == null) { MelonLogger.Error("[RH202] Could not find RH202 muzzleID."); return; }

            clone.transform.SetParent(muzzleIdentity, false);

            Reparent reparent = clone.GetComponent<Reparent>();
            if (reparent != null) UnityEngine.Object.Destroy(reparent);

            clone.name = "RH202_MarderWeapon";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            WeaponSystem weapon = clone.GetComponent<WeaponSystem>();
            GHPC.Weapons.AmmoFeed feed = clone.GetComponent<GHPC.Weapons.AmmoFeed>();

            if (weapon == null) { MelonLogger.Error("[RH202] Cloned weapon has no WeaponSystem."); return; }
            if (feed == null) { MelonLogger.Error("[RH202] Cloned weapon has no AmmoFeed."); return; }

            Transform audioTransform = clone.transform.Find("FMODWeaponAudio");
            if (audioTransform != null)
            {
                WeaponAudio weaponAudio = audioTransform.GetComponent<WeaponAudio>();
                if (weaponAudio != null)
                {
                    FieldInfo audioUnitField = typeof(WeaponAudio).GetField("_unit", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (audioUnitField != null) audioUnitField.SetValue(weaponAudio, vic);
                }
            }

            FieldInfo unitField = typeof(WeaponSystem).GetField("_unit", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo impulseRigidBodyField = typeof(WeaponSystem).GetField("_impulseRigidBody", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo muzzleIdentityField = typeof(WeaponSystem).GetField("_muzzleIdentity", BindingFlags.Instance | BindingFlags.NonPublic);

            if (unitField != null) unitField.SetValue(weapon, vic);
            if (impulseRigidBodyField != null) impulseRigidBodyField.SetValue(weapon, vic);
            if (muzzleIdentityField != null) muzzleIdentityField.SetValue(weapon, muzzleIdentity);

            FieldInfo crewManagerField = typeof(GHPC.Weapons.AmmoFeed).GetField("_crewManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo loadoutManagerField = typeof(GHPC.Weapons.AmmoFeed).GetField("_loadoutManager", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo rootRigidbodyField = typeof(GHPC.Weapons.AmmoFeed).GetField("_rootRigidbody", BindingFlags.Instance | BindingFlags.NonPublic);

            if (crewManagerField != null) crewManagerField.SetValue(feed, vic.InfoBroker._crewManager);
            if (loadoutManagerField != null) loadoutManagerField.SetValue(feed, vic.GetComponent<LoadoutManager>());

            if (rootRigidbodyField != null)
            {
                Component rootRigidbody = vic.GetComponent(rootRigidbodyField.FieldType);
                if (rootRigidbody == null) rootRigidbody = vic.GetComponentInChildren(rootRigidbodyField.FieldType, true);
                rootRigidbodyField.SetValue(feed, rootRigidbody);
            }

            AmmoCodexScriptable dm43Codex = null;
            AmmoCodexScriptable dm51a2Codex = null;
            AmmoCodexScriptable dm63Codex = null;
            AmmoCodexScriptable dm81Codex = Ammo_20mm_Bundeswehr.ammo_codex_dm81;

            foreach (AmmoCodexScriptable codex in Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>())
            {
                if (codex == null || codex.AmmoType == null) continue;
                if (codex.name == "ammo_20mm_DM43" && dm43Codex == null) dm43Codex = codex;
                if (codex.name == "ammo_20mm_DM51A2" && dm51a2Codex == null) dm51a2Codex = codex;
                if (codex.name == "ammo_20mm_DM63" && dm63Codex == null) dm63Codex = codex;
            }

            if (dm43Codex == null || dm51a2Codex == null || ((variant == Leopard2KVariant.KA1 || variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3) && dm81Codex == null) || (variant == Leopard2KVariant.KA3 && dm63Codex == null))
            {
                MelonLogger.Error("[RH202] Required 20mm ammo codex entries are missing.");
                return;
            }

            AmmoType.AmmoClip rh202Dm43 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM43 API-T",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm43Codex }
            };

            AmmoType.AmmoClip rh202Dm51a2 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM51A2 HE",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm51a2Codex }
            };

            AmmoType.AmmoClip rh202Dm63 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM63 APDS-T",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm63Codex }
            };

            AmmoType.AmmoClip rh202Dm81 = new AmmoType.AmmoClip
            {
                Name = "RH202 DM81 HEI-T",
                Capacity = 375,
                MinimalPattern = new AmmoCodexScriptable[] { dm81Codex }
            };

            APClip = rh202Dm43;
            HEClip = rh202Dm51a2;

            if (variant == Leopard2KVariant.KA1 || variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3)
                HEClip = rh202Dm81;

            if (variant == Leopard2KVariant.KA3)
                APClip = rh202Dm63;

            AvailableClips = new AmmoType.AmmoClip[] { APClip, HEClip };

            GameObject rackObject = new GameObject("RH202_AmmoRack");
            rackObject.transform.SetParent(clone.transform, false);
            rackObject.SetActive(false);

            GHPC.Weapons.AmmoRack rh202Rack = rackObject.AddComponent<GHPC.Weapons.AmmoRack>();
            rh202Rack.Name = "RH202 20mm Ammo Rack";
            rh202Rack.UseFixedAmmoLoad = true;
            rh202Rack.UseVisibleRounds = false;
            rh202Rack.ExposedRack = false;
            rh202Rack.ClipTypes = AvailableClips;
            rh202Rack._initialClipCounts = new int[] { 1, 1 };

            rackObject.SetActive(true);

            feed.ReadyRack = rh202Rack;
            AmmoRack = rh202Rack;
            feed.AmmoTypeInBreech = null;
            feed.LoadedClipType = null;

            Transform elevationTransform = rh202Gun.parent;
            Transform mountTransform = elevationTransform != null ? elevationTransform.parent : null;
            Transform traverseTransform = mountTransform != null ? mountTransform.parent : null;

            if (traverseTransform == null || elevationTransform == null)
            {
                MelonLogger.Error("[RH202] RH202 traverse/elevation pivot not found.");
                return;
            }

            AimablePlatform traversePlatform = traverseTransform.GetComponent<AimablePlatform>();
            AimablePlatform elevationPlatform = elevationTransform.GetComponent<AimablePlatform>();

            Transform periR12Transform = leo2kVisualTurret.Find("Peri R12 Transform");
            if (periR12Transform == null) { MelonLogger.Error("[RH202] Could not find Peri R12 Transform."); return; }

            Transform periR12Optic = periR12Transform.Find("Peri R12 Optic");
            if (periR12Optic == null) { MelonLogger.Error("[RH202] Could not find Peri R12 Optic."); return; }

            bool traverseWasActive = traverseTransform.gameObject.activeSelf;
            bool elevationWasActive = elevationTransform.gameObject.activeSelf;
            traverseTransform.gameObject.SetActive(false);
            elevationTransform.gameObject.SetActive(false);

            try
            {
                if (traversePlatform == null) traversePlatform = traverseTransform.gameObject.AddComponent<AimablePlatform>();
                traversePlatform.Axis = EnumHelpers.Axis3.Y;
                traversePlatform.Transform = traverseTransform;
                traversePlatform.HitZones = new GHPC.Equipment.DestructibleComponent[0];
                traversePlatform.StabilizerActive = true;
                traversePlatform.ProtectFromBadProjection = true;
                traversePlatform.ReverseLocalEuler = false;
                traversePlatform.SpeedPowered = 80f;
                traversePlatform.SpeedUnpowered = 8f;
                traversePlatform.LockTransformToRotation = true;
                traversePlatform._unit = vic;
                traversePlatform._equipmentManager = vic._equipmentManager;

                if (elevationPlatform == null) elevationPlatform = elevationTransform.gameObject.AddComponent<AimablePlatform>();
                elevationPlatform.Axis = EnumHelpers.Axis3.X;
                elevationPlatform.Transform = elevationTransform;
                elevationPlatform.HitZones = new GHPC.Equipment.DestructibleComponent[0];
                elevationPlatform.StabilizerActive = true;
                elevationPlatform.ProtectFromBadProjection = true;
                elevationPlatform.ReverseLocalEuler = true;
                elevationPlatform.SpeedPowered = 48f;
                elevationPlatform.SpeedUnpowered = 6f;
                elevationPlatform.LocalEulerLimits = new Vector2(-9f, 45f);
                elevationPlatform.LockTransformToRotation = true;
                elevationPlatform._unit = vic;
                elevationPlatform._equipmentManager = vic._equipmentManager;
            }
            finally
            {
                traverseTransform.gameObject.SetActive(traverseWasActive);
                elevationTransform.gameObject.SetActive(elevationWasActive);
            }

            PeriRh202Follower periFollower = periR12Transform.GetComponent<PeriRh202Follower>();
            if (periFollower == null) periFollower = periR12Transform.gameObject.AddComponent<PeriRh202Follower>();

            periFollower.rh202Traverse = traversePlatform;
            periFollower.rh202Elevation = elevationPlatform;
            periFollower.periTransform = periR12Transform;
            periFollower.periR12Optic = periR12Optic;

            Transform marderPeriZ11 = LEO2K_Assets.marder.transform.Find("Marder1A1_rig/hull/turret/PERI Z11");

            if (marderPeriZ11 == null)
            {
                foreach (Transform t in LEO2K_Assets.marder.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "PERI Z11")
                    {
                        marderPeriZ11 = t;
                        break;
                    }
                }
            }

            if (marderPeriZ11 == null)
            {
                MelonLogger.Error("[RH202] Could not find Marder PERI Z11.");
                return;
            }

            UsableOptic donorOptic = marderPeriZ11.GetComponent<UsableOptic>();
            CameraSlot donorSlot = marderPeriZ11.GetComponent<CameraSlot>();

            if (donorOptic == null || donorSlot == null)
            {
                MelonLogger.Error("[RH202] Marder PERI Z11 is missing UsableOptic or CameraSlot.");
                return;
            }

            bool donorRegisterAtStart = donorOptic.RegisterAtStart;
            Transform donorReparentTarget = donorSlot._reparentTarget;
            CameraSlot donorLinkedNightSight = donorSlot.LinkedNightSight;
            bool donorOpticWasActive = marderPeriZ11.gameObject.activeSelf;

            donorOptic.RegisterAtStart = false;
            donorSlot._reparentTarget = vehicleTurret;
            donorSlot.LinkedNightSight = null;

            GameObject opticClone = UnityEngine.Object.Instantiate(marderPeriZ11.gameObject, vehicleTurret, false);

            marderPeriZ11.gameObject.SetActive(donorOpticWasActive);
            donorOptic.RegisterAtStart = donorRegisterAtStart;
            donorSlot._reparentTarget = donorReparentTarget;
            donorSlot.LinkedNightSight = donorLinkedNightSight;

            Reparent opticReparent = opticClone.GetComponent<Reparent>();
            if (opticReparent != null) UnityEngine.Object.Destroy(opticReparent);

            opticClone.name = "RH202_Optic";
            opticClone.transform.SetPositionAndRotation(periR12Optic.position, periR12Optic.rotation);
            opticClone.transform.localScale = Vector3.one;

            UsableOptic optic = opticClone.GetComponent<UsableOptic>();
            CameraSlot rh202Slot = opticClone.GetComponent<CameraSlot>();
            optic.RegisterAtStart = true;

            //MelonLogger.Msg("[RH202 OPTIC] Transform=" + (optic.Transform != null ? optic.Transform.name : "NULL") + " | Parent=" + (optic.Transform != null && optic.Transform.parent != null ? optic.Transform.parent.name : "NULL") + " | RotateAzimuth=" + optic.RotateAzimuth + " | RotateElevation=" + optic.RotateElevation);

            if (optic == null || rh202Slot == null)
            {
                MelonLogger.Error("[RH202] RH202 optic clone is missing UsableOptic or CameraSlot.");
                UnityEngine.Object.Destroy(opticClone);
                return;
            }

            RH202Slot = rh202Slot;

            Transform auxSightTransform = vehicleTurret.Find("1A3 mantlet/--Gun Scripts--/Aux sight TZF1A");

            if (auxSightTransform == null)
            {
                MelonLogger.Error("[RH202 CAM] Could not find Aux sight TZF1A.");
            }
            else
            {
                AuxSightSlot = auxSightTransform.GetComponent<CameraSlot>();

                if (AuxSightSlot == null)
                    MelonLogger.Error("[RH202 CAM] Aux sight TZF1A has no CameraSlot component.");
            }

            rh202Slot.LinkedNightSight = null;
            rh202Slot.ExclusiveWeapons = new WeaponSystem[] { weapon };
            rh202Slot._pairedOptic = optic;
            rh202Slot._reparentTarget = vehicleTurret;

            LateFollow rh202LateFollow = opticClone.GetComponent<LateFollow>();
            if (rh202LateFollow == null) rh202LateFollow = opticClone.AddComponent<LateFollow>();

            rh202LateFollow.FollowTarget = periR12Optic;
            rh202LateFollow.ForceToRoot = false;
            rh202LateFollow.SuppressWarnings = true;

            rh202Fcs.Mounts = new AimablePlatform[] { traversePlatform, elevationPlatform };
            rh202Fcs.MainOptic = optic;
            rh202Fcs.ReferenceTransform = elevationTransform;
            optic.FCS = rh202Fcs;
            optic.Alignment = OpticAlignment.ElevationCompensated;
            optic.RotateAzimuth = true;
            optic.RotateElevation = true;
            optic.LocalElevationLimits = elevationPlatform.LocalEulerLimits;
            if (optic.reticleMesh != null)
                optic.reticleMesh.Load();

            if (variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3)
            {
                optic.Alignment = OpticAlignment.BoresightStabilized;
            }

            rh202Fcs.LinkedWeaponSystems = new WeaponSystem[] { weapon };
            rh202Fcs.StabsActive = true;
            rh202Fcs.CurrentStabMode = StabilizationMode.Vector;
            weapon.FCS = rh202Fcs;

            StaticWhenNotInUse fix = rh202Fcs.gameObject.GetComponent<StaticWhenNotInUse>();
            if (fix == null) fix = rh202Fcs.gameObject.AddComponent<StaticWhenNotInUse>();
            fix.fcs = rh202Fcs;

            bool nightDonorRegisterAtStart = donorOptic.RegisterAtStart;
            Transform nightDonorReparentTarget = donorSlot._reparentTarget;
            CameraSlot nightDonorLinkedNightSight = donorSlot.LinkedNightSight;

            donorOptic.RegisterAtStart = false;
            donorSlot._reparentTarget = vehicleTurret;
            donorSlot.LinkedNightSight = null;

            GameObject nightOpticClone = UnityEngine.Object.Instantiate(marderPeriZ11.gameObject, vehicleTurret, false);

            donorOptic.RegisterAtStart = nightDonorRegisterAtStart;
            donorSlot._reparentTarget = nightDonorReparentTarget;
            donorSlot.LinkedNightSight = nightDonorLinkedNightSight;

            Reparent nightReparent = nightOpticClone.GetComponent<Reparent>();
            if (nightReparent != null)
                UnityEngine.Object.Destroy(nightReparent);

            nightOpticClone.name = "RH202_NightOptic";
            nightOpticClone.transform.SetPositionAndRotation(periR12Optic.position, periR12Optic.rotation);
            nightOpticClone.transform.localScale = Vector3.one;

            UsableOptic nightOptic = nightOpticClone.GetComponent<UsableOptic>();
            CameraSlot nightSlot = nightOpticClone.GetComponent<CameraSlot>();

            if (nightOptic == null || nightSlot == null)
            {
                MelonLogger.Error("[RH202 NIGHT] Marder PERI Z11 clone is missing UsableOptic or CameraSlot.");
                UnityEngine.Object.Destroy(nightOpticClone);
                return;
            }

            nightOptic.RegisterAtStart = true;

            nightSlot.name = "RH202_NightSlot";
            nightSlot.IsLinkedNightSight = true;
            nightSlot.LinkedDaySight = rh202Slot;
            nightSlot.LinkedNightSight = null;
            nightSlot._pairedOptic = nightOptic;
            nightSlot.ExclusiveWeapons = new WeaponSystem[] { weapon };
            nightSlot._reparentTarget = vehicleTurret;

            nightOptic.FCS = rh202Fcs;
            nightOptic.Alignment = optic.Alignment;
            nightOptic.RotateAzimuth = true;
            nightOptic.RotateElevation = true;
            nightOptic.LocalElevationLimits = elevationPlatform.LocalEulerLimits;

            LateFollow nightFollow = nightOpticClone.GetComponent<LateFollow>();
            if (nightFollow == null)
                nightFollow = nightOpticClone.AddComponent<LateFollow>();

            nightFollow.FollowTarget = periR12Optic;
            nightFollow.ForceToRoot = false;
            nightFollow.SuppressWarnings = true;

            rh202Slot.LinkedNightSight = nightSlot;
            rh202Fcs.NightOptic = nightOptic;

            nightSlot.VisionType = NightVisionType.Intensifier;

            if (variant == Leopard2KVariant.KA2 || variant == Leopard2KVariant.KA3)
            {
                GameObject lase = new GameObject("RH202_LaserOrigin");
                lase.transform.SetParent(optic.transform, false);

                rh202Fcs.LaserOrigin = lase.transform;
                rh202Fcs.LaserAim = LaserAimMode.Fixed;
                rh202Fcs.MaxLaserRange = 4000f;
                rh202Fcs.OpticalRangefinder = null;

                RH202LaserPointCorrection laserFix = rh202Fcs.gameObject.GetComponent<RH202LaserPointCorrection>();
                if (laserFix == null)
                    laserFix = rh202Fcs.gameObject.AddComponent<RH202LaserPointCorrection>();

                laserFix.laser = lase.transform;
                laserFix.dayOptic = optic;
                laserFix.nightOptic = nightOptic;
            }

            if (variant != Leopard2KVariant.KA3)
            {
                Transform periR12PostObject = vehicleTurret.Find("1A3 mantlet/--Gun Scripts--/PERI-R12");

                if (periR12PostObject == null)
                {
                    MelonLogger.Error("[RH202 NIGHT] Could not find runtime PERI-R12 GameObject.");
                }
                else
                {
                    UnityEngine.Rendering.PostProcessing.PostProcessVolume periPost =
                        periR12PostObject.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();

                    UnityEngine.Rendering.PostProcessing.PostProcessVolume nightPost =
                        nightOpticClone.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();

                    if (periPost == null)
                    {
                        MelonLogger.Error("[RH202 NIGHT] PERI-R12 GameObject has no PostProcessVolume.");
                    }
                    else if (nightPost == null)
                    {
                        MelonLogger.Error("[RH202 NIGHT] RH202_NightOptic has no PostProcessVolume.");
                    }
                    else
                    {
                        nightPost.sharedProfile = periPost.sharedProfile;
                        nightPost.profile = periPost.profile;
                        nightPost.isGlobal = periPost.isGlobal;
                        nightPost.blendDistance = periPost.blendDistance;
                        nightPost.weight = periPost.weight;
                        nightPost.priority = periPost.priority;
                        nightPost.enabled = true;
                    }
                }
            }

            if (variant != Leopard2KVariant.KA3)
            {
                Transform pzbReticle = vehicleTurret.Find("1A3 mantlet/--Gun Scripts--/PERI-R12/pzb reticle");
                Transform newNfov = vehicleTurret.Find("LEO1A1A1_rig/HULL/TURRET/RH202_NightSlot");
                UnityEngine.GameObject.Destroy(newNfov);

                if (pzbReticle == null)
                {
                    MelonLogger.Error("[RH202 NIGHT] Could not find native PERI-R12 pzb reticle.");
                }
                else
                {
                    Transform oldWfov = nightOpticClone.transform.Find("WFOV reticle");
                    Transform oldNfov = nightOpticClone.transform.Find("NFOV reticle");

                    if (oldWfov != null)
                    {
                        Reticle.PostMeshComp wfovPost = oldWfov.GetComponent<Reticle.PostMeshComp>();

                        if (wfovPost != null)
                            UnityEngine.Object.Destroy(wfovPost);

                        GameObject wfovClone = UnityEngine.Object.Instantiate(pzbReticle.gameObject, oldWfov, false);
                        wfovClone.name = "RH202_PZB_WFOV";
                        wfovClone.transform.localPosition = pzbReticle.localPosition;
                        wfovClone.transform.localRotation = pzbReticle.localRotation;
                        wfovClone.transform.localScale = pzbReticle.localScale;
                        wfovClone.SetActive(true);

                        Transform wfovBoresight = wfovClone.transform.Find("FFP/Boresight");

                        if (wfovBoresight != null)
                        {
                            wfovBoresight.localScale = new Vector3(4f, 4f, 4f);
                        }
                        else
                        {
                            MelonLogger.Warning("[RH202 NIGHT] Could not find WFOV PZB Boresight.");
                        }
                    }
                    else
                    {
                        MelonLogger.Error("[RH202 NIGHT] Could not find original WFOV reticle.");
                    }

                    if (oldNfov != null)
                    {
                        Reticle.PostMeshComp nfovPost = oldNfov.GetComponent<Reticle.PostMeshComp>();

                        if (nfovPost != null)
                            UnityEngine.Object.Destroy(nfovPost);

                        GameObject nfovClone = UnityEngine.Object.Instantiate(pzbReticle.gameObject, oldNfov, false);
                        nfovClone.name = "RH202_PZB_NFOV";
                        nfovClone.transform.localPosition = pzbReticle.localPosition;
                        nfovClone.transform.localRotation = pzbReticle.localRotation;
                        nfovClone.transform.localScale = pzbReticle.localScale;
                        nfovClone.SetActive(true);

                        Transform nfovBoresight = nfovClone.transform.Find("FFP/Boresight");

                        if (nfovBoresight != null)
                        {
                            nfovBoresight.localScale = new Vector3(1.45f, 1.45f, 1.45f);
                        }
                        else
                        {
                            MelonLogger.Warning("[RH202 NIGHT] Could not find NFOV PZB Boresight.");
                        }
                    }
                    else
                    {
                        MelonLogger.Error("[RH202 NIGHT] Could not find original NFOV reticle.");
                    }
                }
            }

            if (variant == Leopard2KVariant.KA3)
            {
                LEO2KThermals.Add(nightOptic, LEO2KThermals.ThermalVariant.Leopard2K2);

                Reticle.ReticleMesh primaryReticle = nightOptic.reticleMesh;
                Reticle.ReticleMesh wfovReticle = null;

                Transform wfovTransform = nightOptic.transform.Find("__LEO2K_THERMAL_WFOV__");
                if (wfovTransform != null)
                    wfovReticle = wfovTransform.GetComponent<Reticle.ReticleMesh>();

                if (primaryReticle != null && wfovReticle != null)
                {
                    RH202ReticleBothActive bothActive = nightOptic.GetComponent<RH202ReticleBothActive>();
                    if (bothActive == null)
                        bothActive = nightOptic.gameObject.AddComponent<RH202ReticleBothActive>();

                    bothActive.primaryReticle = primaryReticle;
                    bothActive.wfovReticle = wfovReticle;
                    bothActive.ForceActive();
                }
                else
                {
                    MelonLogger.Warning("[RH202] Could not find both thermal reticles to force active.");
                }
            }

            WeaponSystemInfo rh202Info = new WeaponSystemInfo
            {
                Name = "20mm RCWS Rh202",
                FCS = rh202Fcs,
                Weapon = weapon,
                Role = WeaponSystemRole.RoofGun,
                PreAimWeapon = WeaponSystemRole.RoofGun
            };

            bool replacedRoofGun = false;
            WeaponSystemInfo[] weapons = vic._weaponsManager.Weapons;

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null || weapons[i].Role != WeaponSystemRole.RoofGun) continue;
                weapons[i] = rh202Info;
                replacedRoofGun = true;
                break;
            }

            if (!replacedRoofGun)
            {
                Array.Resize(ref weapons, weapons.Length + 1);
                weapons[weapons.Length - 1] = rh202Info;
            }

            vic._weaponsManager.Weapons = weapons;

            ICrewBrain gunnerBrain = vic.InfoBroker._crewManager.GetCrewBrain(CrewPosition.Gunner);

            if (gunnerBrain != null && gunnerBrain.WeaponsModule != null)
            {
                if (!gunnerBrain.WeaponsModule.Weapons.Contains(rh202Info))
                    gunnerBrain.WeaponsModule.Weapons.Add(rh202Info);
            }
            else
            {
                MelonLogger.Error("[RH202] Gunner WeaponsModule not found.");
            }

            AimablePlatform[] vehiclePlatforms = vic.AimablePlatforms;
            Array.Resize(ref vehiclePlatforms, vehiclePlatforms.Length + 2);
            vehiclePlatforms[vehiclePlatforms.Length - 2] = traversePlatform;
            vehiclePlatforms[vehiclePlatforms.Length - 1] = elevationPlatform;
            vic.AimablePlatforms = vehiclePlatforms;

            CameraSlot[] cameraSlots = vic.DesignatedCameraSlots;
            Array.Resize(ref cameraSlots, cameraSlots.Length + 1);
            cameraSlots[cameraSlots.Length - 1] = rh202Slot;
            vic._designatedCameraSlots = cameraSlots;

            CameraManager cameraManager = CameraManager.Instance;
            if (cameraManager != null && PlayerInput.Instance != null && PlayerInput.Instance.CurrentPlayerUnit == vic)
                cameraManager.RescanCamSlots(vic.DesignatedCameraSlots);
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.QueueAmmoIndex))]
    public static class RH202AmmoSwitchPatch
    {
        public static bool Prefix(PlayerInput __instance, int index)
        {
            if (__instance.CurrentPlayerCrewBrain == null || __instance.CurrentPlayerCrewBrain.WeaponsModule == null || __instance.CurrentPlayerCrewBrain.WeaponsModule.ActiveWeapon == null)
                return true;

            WeaponSystem activeWeapon = __instance.CurrentPlayerCrewBrain.WeaponsModule.ActiveWeapon.Weapon;
            if (activeWeapon == null || activeWeapon.name != "RH202_MarderWeapon")
                return true;

            GHPC.Weapons.AmmoFeed feed = activeWeapon.Feed;
            if (feed == null || feed.ReadyRack == null)
                return false;

            AmmoType.AmmoClip[] clipTypes = feed.ReadyRack.ClipTypes;
            if (clipTypes == null || index < 0 || index >= clipTypes.Length)
                return false;

            __instance.CurrentPlayerCrewBrain.WeaponsModule.QueueClipType(clipTypes[index], firstWeaponOnly: false);
            return false;
        }
    }
}