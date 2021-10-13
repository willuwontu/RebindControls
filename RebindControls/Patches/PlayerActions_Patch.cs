using HarmonyLib;

namespace RebindControls.Patches
{
    [HarmonyPatch(typeof(PlayerActions))]
    class PlayerActions_Patch
    {
        //[HarmonyPriority(Priority.Last)]
        [HarmonyPrefix]
        [HarmonyPatch("CreateWithKeyboardBindings")]
        static bool KeyboardLayout(ref PlayerActions __result)
        {
            if (RebindControls.instance.setupDefault)
            {
                UnityEngine.Debug.Log("[RebindControls] Default Keyboard layout has been setup already, attempting to substitute the player's layout instead.");
                PlayerActions temp = new PlayerActions();
                temp.Load(RebindControls.instance.keyboardPlayer.Save());
                __result = temp;
                return false;
            }
            return true;
        }

        //[HarmonyPriority(Priority.Last)]
        [HarmonyPrefix]
        [HarmonyPatch("CreateWithControllerBindings")]
        static bool ControllerLayout(ref PlayerActions __result)
        {
            if (RebindControls.instance.setupDefault)
            {
                UnityEngine.Debug.Log("[RebindControls] Default Controller layout has been setup already, attempting to substitute the player's layout instead.");
                PlayerActions temp = new PlayerActions();
                temp.Load(RebindControls.instance.controllerPlayer.Save());
                __result = temp;
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
