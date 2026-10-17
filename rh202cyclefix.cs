using GHPC.Camera;
using GHPC.Player;
using GHPC.Weapons;
using HarmonyLib;
using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;

namespace Fulda1989
{
    [HarmonyPatch]
    public static class RH202CameraCyclePatch
    {
        static MethodBase TargetMethod()
        {
            foreach (MethodInfo method in typeof(CameraManager).GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (method.Name == "AttachCamToSlot" &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(CameraSlot))
                {
                    return method;
                }
            }

            return null;
        }

        static bool Prefix(CameraManager __instance, ref CameraSlot __0)
        {
            if (RH202.RH202Slot == null || RH202.AuxSightSlot == null || __0 == null)
                return true;

            CameraSlot auxSlot = RH202.AuxSightSlot;
            CameraSlot auxNightSlot = auxSlot.LinkedNightSight;

            if (__0 == auxSlot || __0 == auxNightSlot)
            {
                __0 = RH202.RH202Slot;
                MelonLogger.Msg("[RH202] Blocked GAS camera selection and redirected to RH202 optic.");
            }

            return true;
        }
    }
}