using BepInEx;
using UnboundLib;
using UnboundLib.Utils;
using HarmonyLib;
using UnityEngine;
using InControl;

namespace RebindControls
{
    // These are the mods required for our mod to work
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch", BepInDependency.DependencyFlags.HardDependency)]
    // Declares our mod to Bepin
    [BepInPlugin(ModId, ModName, Version)]
    // The game our mod is associated with
    [BepInProcess("Rounds.exe")]
    public class RebindControls : BaseUnityPlugin
    {
        private const string ModId = "com.willuwontu.rounds.rebindcontrols";
        private const string ModName = "Rebind Controls";
        public const string Version = "1.0.0"; // What version are we on (major.minor.patch)?

        private PlayerActions keyboardPlayer = new PlayerActions();

        private

        void Awake()
        {
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
        void Start()
        {
            Unbound.RegisterCredits(ModName, new string[] { "willuwontu" }, new string[] { "github", "Ko-Fi" }, new string[] { "https://github.com/willuwontu/wills-wacky-cards", "https://ko-fi.com/willuwontu" });
        }
    }
}