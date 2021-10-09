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
        public bool defaultIsSetup = false;

        internal AssetBundle UIAssets;
        private GameObject containerFrameAsset;
        private GameObject keyGroupAsset;
        private GameObject bindingButtonAsset;
        public AudioClip click;
        public AudioClip hover;

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

            LoadAssets();

            UnityEngine.Debug.Log($"[RebindControls] Setting up config binds.");
            //Setup the BepInEx config binds
            {
                keyboardBindingsConfig = Config.Bind("Controls", "Keyboard", keyboardPlayer.Save(), "The control layout for a keyboard and mouse user.");
                controllerBindingsConfig = Config.Bind("Controls", "Controller", controllerPlayer.Save(), "The control layout for a controller user.");
                keyboardPlayer.Load(keyboardBindingsConfig.Value);
                controllerPlayer.Load(controllerBindingsConfig.Value);
            }

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
                keyboardPlayer.ListenOptions.MaxAllowedBindings = 3U;
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
            bindingButtonAsset = UIAssets.LoadAsset<GameObject>("KeyBindingGroup");
            click = UIAssets.LoadAsset<AudioClip>("ui_button_click_01");
            hover = UIAssets.LoadAsset<AudioClip>("ui_button_hover_01");
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

        private void ResetKeyboardLayout()
        {

        }

        private void ResetControllerLayout()
        {

        }

        private void MainGui(GameObject menu)
        {
            MenuHandler.CreateText("Keyboard Layout", menu, out TextMeshProUGUI _, 60);
            var blah = CreateKeyBindingLayout(menu, keyboardPlayer);



            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);

            MenuHandler.CreateButton("Reset keyboard layout to default".ToUpper(), menu, ResetKeyboardLayout, 30);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);

            MenuHandler.CreateText("Controller Layout", menu, out TextMeshProUGUI _, 60);
            var bleh = CreateKeyBindingLayout(menu, controllerPlayer);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);

            MenuHandler.CreateButton("Reset controller layout to default".ToUpper(), menu, ResetControllerLayout, 30);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);
        }

        private void UpdateDisplayedBindings()
        {

        }

        private void OnClearButtonPressed()
        {

        }

        private void OnBindingButtonPressed()
        {

        }

        private void OnResetButtonPressed()
        {

        }

        private GameObject CreateKeyBindingLayout(GameObject parent, PlayerActions actions)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }

            var container = GameObject.Instantiate(containerFrameAsset, parent.transform);

            var grid = container.GetOrAddComponent<GridLayoutGroup>();

            //grid.cellSize = new Vector2(100, 100);

            foreach (var action in actions.Actions)
            {
                CreateBindingFrame(container, action);
            }

            return container;
        }

        private GameObject CreateBindingFrame(GameObject parent, PlayerAction action)
        {
            var keyGroup = GameObject.Instantiate(keyGroupAsset, parent.transform);
            var text = keyGroup.transform.Find("KeyTitle/Title").gameObject.GetComponent<TextMeshProUGUI>();
            text.text = action.Name;

            var reset = keyGroup.transform.Find("KeyTitle/Reset").gameObject;
            var interact = reset.GetOrAddComponent<ButtonInteraction>();
            interact.mouseClick.AddListener(OnResetButtonPressed);

            var bindingGroup = keyGroup.transform.Find("Bindings").gameObject;

            for (int i = 0; i < 3; i++)
            {
                CreateKeyBindButton(bindingGroup, (i < action.UnfilteredBindings.Count) ? action.UnfilteredBindings[i] : null);
            }


            return keyGroup;
        }

        private GameObject CreateKeyBindButton(GameObject parent, BindingSource binding)
        {
            var keyBindingGroup = GameObject.Instantiate(bindingButtonAsset, parent.transform);

            var bindingButton = keyBindingGroup.transform.Find("Binding").gameObject;

            var bindingText = bindingButton.transform.Find("Name").gameObject.GetComponent<TextMeshProUGUI>();

            var clear = keyBindingGroup.transform.Find("Clear").gameObject;

            var interact = bindingButton.GetOrAddComponent<ButtonInteraction>();
            interact.mouseClick.AddListener(OnBindingButtonPressed);

            interact = clear.GetOrAddComponent<ButtonInteraction>();
            interact.mouseClick.AddListener(OnClearButtonPressed);

            if (binding != null)
            {
                bindingText.text = binding.Name;
            }
            else
            {
                bindingText.text = "Not Set.";

            }

            //var button = bindButton.GetComponent<Button>();

            //bindButton.AddComponent<ButtonInteraction>();

            return keyBindingGroup;
        }
    }

    public class BindingInfo : MonoBehaviour
    {
        public BindingSource binding = null;
        public PlayerAction action = null;
        public int slot = 0;
        public TextMeshProUGUI text;

        public void Update()
        {
            if (action)
            {
                binding = slot < action.Bindings.Count ? action.Bindings[slot] : null;
            }


            if (text)
            {
                if (binding != null)
                {
                    text.text = binding.Name;
                }
                else if (action.IsListeningForBinding)
                {
                    text.text = "Not Set.";
                }
                else
                {
                    text.text = "Not Set.";
                }
            }
        }
    }

    public class ButtonInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEvent mouseClick = new UnityEvent();
        public UnityEvent leftClick = new UnityEvent();
        public UnityEvent middleClick = new UnityEvent();
        public UnityEvent rightClick = new UnityEvent();
        public UnityEvent mouseEnter = new UnityEvent();
        public UnityEvent mouseExit = new UnityEvent();
        public AudioSource source;
        public static ButtonInteraction instance;

        void Start()
        {
            instance = this;
            gameObject.GetOrAddComponent<AudioSource>();
            mouseEnter.AddListener(OnEnter);
            mouseExit.AddListener(OnExit);
            leftClick.AddListener(OnClick);
        }

        public void OnEnter()
        {
            UnityEngine.Debug.Log($"Button Enter");
            source.PlayOneShot(RebindControls.instance.hover);
        }

        public void OnExit()
        {
            UnityEngine.Debug.Log($"Button Exit");
            source.PlayOneShot(RebindControls.instance.hover);
        }

        public void OnClick()
        {
            UnityEngine.Debug.Log($"Button Clicked");
            source.PlayOneShot(RebindControls.instance.click);
            //Destroy(gameObject);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            mouseEnter.Invoke();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            mouseExit.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                leftClick.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Middle)
                middleClick.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Right)
                rightClick.Invoke();
            mouseClick.Invoke();
        }
    }
}