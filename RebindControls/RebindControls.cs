using BepInEx;
using BepInEx.Configuration;
using UnboundLib;
using UnboundLib.Utils.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Jotunn.Utils;
using UnityEngine.UI;
using InControl;
using System;
using TMPro;

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

        public static RebindControls instance { get; private set; }
        private PlayerActions keyboardPlayer;
        private PlayerActions controllerPlayer;
        private ConfigEntry<string> keyboardBindingsConfig;
        private ConfigEntry<string> controllerBindingsConfig;
        internal AssetBundle UIAssets;
        private GameObject containerFrameAsset;
        private GameObject keyGroupAsset;
        private GameObject bindingButtonAsset;
        public bool defaultIsSetup = false;

        void Awake()
        {
            // Use this to call any harmony patch files your mod may have
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
        void Start()
        {
            instance = this;

            UnityEngine.Debug.Log($"[RebindControls] Registering Credits");
            Unbound.RegisterCredits(ModName, new string[] { "willuwontu" }, new string[] { "github", "Ko-Fi" }, new string[] { "https://github.com/willuwontu/RebindControls", "https://ko-fi.com/willuwontu" });

            UnityEngine.Debug.Log($"[RebindControls] Initializing Default Controls");
            InitializeDefaultControls();

            UnityEngine.Debug.Log($"[RebindControls] Setting up config binds.");
            //Setup the BepInEx config binds
            {
                keyboardBindingsConfig = Config.Bind("Controls", "Keyboard", keyboardPlayer.Save(), "The control layout for a keyboard and mouse user.");
                controllerBindingsConfig = Config.Bind("Controls", "Controller", controllerPlayer.Save(), "The control layout for a controller user.");
                keyboardPlayer.Load(keyboardBindingsConfig.Value);
                controllerPlayer.Load(controllerBindingsConfig.Value);
            }

            UnityEngine.Debug.Log($"[RebindControls] Initializing loading of assets.");
            LoadAssets();

            UnityEngine.Debug.Log($"[RebindControls] Registering Menu");
            Unbound.RegisterMenu(ModName, () => { }, this.MainGui, null, true);

            UnityEngine.Debug.Log($"[RebindControls] Adding network events.");
            var networkEvents = gameObject.AddComponent<NetworkEventCallbacks>();
            networkEvents.OnJoinedRoomEvent += OnJoinedRoomAction;
            networkEvents.OnLeftRoomEvent += OnLeftRoomAction;
        }

        private void InitializeDefaultControls()
        {
            keyboardPlayer = PlayerActions.CreateWithKeyboardBindings();
            controllerPlayer = PlayerActions.CreateWithControllerBindings();
            // Setup the default profile for a keyboard player
            {
                keyboardPlayer.ListenOptions.IncludeControllers = false;
                keyboardPlayer.ListenOptions.IncludeNonStandardControls = false;
                keyboardPlayer.ListenOptions.IncludeUnknownControllers = false;
                keyboardPlayer.ListenOptions.MaxAllowedBindings = 1U;
                keyboardPlayer.ListenOptions.UnsetDuplicateBindingsOnSet = true;
                keyboardPlayer.ListenOptions.IncludeMouseButtons = true;
                keyboardPlayer.ListenOptions.OnBindingFound = null;
                keyboardPlayer.ListenOptions.OnBindingRejected = null;
                keyboardPlayer.ListenOptions.OnBindingAdded = null;
                keyboardPlayer.ListenOptions.OnBindingFound += OnBindingFound;
                keyboardPlayer.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
                keyboardPlayer.ListenOptions.OnBindingAdded += OnBindingAdded;
            }

            // Setup the default profile for a controller player
            {
                controllerPlayer.ListenOptions.IncludeUnknownControllers = true;
                controllerPlayer.ListenOptions.MaxAllowedBindings = 3U;
                controllerPlayer.ListenOptions.UnsetDuplicateBindingsOnSet = true;
                controllerPlayer.ListenOptions.OnBindingFound = null;
                controllerPlayer.ListenOptions.OnBindingRejected = null;
                controllerPlayer.ListenOptions.OnBindingAdded = null;
                controllerPlayer.ListenOptions.OnBindingFound += OnBindingFoundController;
                controllerPlayer.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
                controllerPlayer.ListenOptions.OnBindingAdded += OnBindingAdded;
            }
            defaultIsSetup = true;
        }

        private void LoadAssets()
        {
            UnityEngine.Debug.Log($"[RebindControls] Attempting to load assetbundle.");
            UIAssets = AssetUtils.LoadAssetBundleFromResources("rebindcontrolsui", typeof(RebindControls).Assembly);

            foreach (var asset in UIAssets.GetAllAssetNames())
            {
                UnityEngine.Debug.Log($"[RebindControls][UI Asset] {asset}");
            }

            UnityEngine.Debug.Log($"[RebindControls] Attempting to load assets.");
            containerFrameAsset = UIAssets.LoadAsset<GameObject>("BindingGridFrame");
            keyGroupAsset = UIAssets.LoadAsset<GameObject>("KeyGroup");
            bindingButtonAsset = UIAssets.LoadAsset<GameObject>("Binding");
        }

        private void OnJoinedRoomAction()
        {

        }

        private void OnLeftRoomAction()
        {

        }

        private void OnBindingAdded(PlayerAction action, BindingSource binding)
        {
            Debug.Log("Binding added... " + binding.DeviceName + ": " + binding.Name);
        }

        private bool OnBindingFound(PlayerAction action, BindingSource binding)
        {
            if (binding == new KeyBindingSource(Key.Escape))
            {
                action.StopListeningForBinding();
                return false;
            }
            return true;
        }

        private bool OnBindingFoundController(PlayerAction action, BindingSource binding)
        {
            if (binding.BindingSourceType != BindingSourceType.DeviceBindingSource)
            {
                action.StopListeningForBinding();
                return false;
            }
            return true;
        }

        private void OnBindingRejectedMenu(PlayerAction action, BindingSource binding, BindingSourceRejectionType reason)
        {
            Debug.Log("Binding rejected... " + reason);
        }

        private void MainGui(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);

            CreateKeyBindingLayout(menu, keyboardPlayer);

        }

        private void KeyboardGUI(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);

            foreach (var action in keyboardPlayer.Actions)
            {
                //MenuHandler.CreateText
                MenuHandler.CreateButton("", menu);
            }
        }

        private GameObject CreateKeyBindingLayout(GameObject parent, PlayerActions bindings)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }

            var container = GameObject.Instantiate(containerFrameAsset, parent.transform);

            return container;
        }
    }

    public class RightClick : MonoBehaviour, IPointerClickHandler
    {

        public UnityEvent leftClick;
        public UnityEvent middleClick;
        public UnityEvent rightClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                leftClick.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Middle)
                middleClick.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Right)
                rightClick.Invoke();
        }
    }
}