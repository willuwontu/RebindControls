using HarmonyLib;
using InControl;
using UnityEngine;

namespace RebindControls.Patches
{
    [HarmonyPatch(typeof(GoBack))]
    class GoBack_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        static bool KeyboardLayout()
        {
            bool bPressed = false;
            foreach (var device in InputManager.ActiveDevices)
            {
                if (device.Action2.WasPressed)
                {
                    bPressed = true;
                }
            }
            if ((Input.GetKeyDown(KeyCode.Escape) || bPressed) && (RebindControls.instance.keyboardPlayer.IsListeningForBinding || RebindControls.instance.controllerPlayer.IsListeningForBinding) && !GameManager.instance.isPlaying)
            {
                if (RebindControls.instance.keyboardPlayer.IsListeningForBinding)
                {
                    UnityEngine.Debug.Log($"[RebindControls] Keyboard layout is listening for input. Skipping go back.");
                }
                else
                {
                    UnityEngine.Debug.Log($"[RebindControls] Controller layout is listening for input. Skipping go back.");
                }
                return false;
            }
            return true;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}

        //[HarmonyPostfix]
        //[HarmonyPatch("SomeMethod")]
        //static void MyMethodName()
        //{

        //}
    }
}
