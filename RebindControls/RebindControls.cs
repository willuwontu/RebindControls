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
using System.Linq;
using System.Collections.Generic;
using Photon.Pun;
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
        public PlayerActions keyboardPlayer;
        public PlayerActions controllerPlayer;
        private ConfigEntry<string> keyboardBindingsConfig;
        private ConfigEntry<string> controllerBindingsConfig;
        public bool setupDefault = false;

        internal AssetBundle UIAssets;
        private GameObject containerFrameAsset;
        private GameObject keyGroupAsset;
        private GameObject bindingButtonAsset;
        public List<AudioClip> click;
        public List<AudioClip> hover;

        private Dictionary<GameObject, Dictionary<GameObject, List<GameObject>>> layouts = new Dictionary<GameObject, Dictionary<GameObject, List<GameObject>>>();

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
            keyboardPlayer = DefaultKeyboardLayout();
            controllerPlayer = DefaultControllersLayout();

            LoadAssets();

            UnityEngine.Debug.Log($"[RebindControls] Setting up config binds.");
            //Setup the BepInEx config binds
            {
                keyboardBindingsConfig = Config.Bind("Controls", "Keyboard", keyboardPlayer.Save(), "The control layout for a keyboard and mouse user.");
                controllerBindingsConfig = Config.Bind("Controls", "Controller", controllerPlayer.Save(), "The control layout for a controller user.");
                keyboardPlayer.Load(keyboardBindingsConfig.Value);
                controllerPlayer.Load(controllerBindingsConfig.Value);
                setupDefault = true;
            }


            UnityEngine.Debug.Log($"[RebindControls] Registering Menu");
            Unbound.RegisterMenu(ModName, () => { }, this.MainGui, null, true);
        }

        private PlayerActions DefaultKeyboardLayout()
        {
            PlayerActions playerActions = PlayerActions.CreateWithKeyboardBindings();
            playerActions.ListenOptions.IncludeControllers = false;
            playerActions.ListenOptions.IncludeNonStandardControls = false;
            playerActions.ListenOptions.IncludeUnknownControllers = false;
            playerActions.ListenOptions.MaxAllowedBindings = 3U;
            playerActions.ListenOptions.UnsetDuplicateBindingsOnSet = true;
            playerActions.ListenOptions.IncludeMouseButtons = true;
            playerActions.ListenOptions.OnBindingFound = null;
            playerActions.ListenOptions.OnBindingRejected = null;
            playerActions.ListenOptions.OnBindingAdded = null;
            playerActions.ListenOptions.OnBindingFound += OnBindingFound;
            playerActions.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
            playerActions.ListenOptions.OnBindingAdded += OnBindingAdded;

            return playerActions;
        }

        private PlayerActions DefaultControllersLayout()
        {
            PlayerActions playerActions = PlayerActions.CreateWithControllerBindings();
            playerActions.ListenOptions.IncludeUnknownControllers = true;
            playerActions.ListenOptions.MaxAllowedBindings = 3U;
            playerActions.ListenOptions.UnsetDuplicateBindingsOnSet = true;
            playerActions.ListenOptions.OnBindingFound = null;
            playerActions.ListenOptions.OnBindingRejected = null;
            playerActions.ListenOptions.OnBindingAdded = null;
            playerActions.ListenOptions.OnBindingFound += OnBindingFoundController;
            playerActions.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
            playerActions.ListenOptions.OnBindingAdded += OnBindingAdded;

            return playerActions;
        }

        private void SaveLayouts()
        {
            keyboardPlayer.ListenOptions.OnBindingFound -= OnBindingFound;
            keyboardPlayer.ListenOptions.OnBindingRejected -= OnBindingRejectedMenu;
            keyboardPlayer.ListenOptions.OnBindingAdded -= OnBindingAdded;
            controllerPlayer.ListenOptions.OnBindingFound -= OnBindingFoundController;
            controllerPlayer.ListenOptions.OnBindingRejected -= OnBindingRejectedMenu;
            controllerPlayer.ListenOptions.OnBindingAdded -= OnBindingAdded;

            keyboardBindingsConfig.Value = keyboardPlayer.Save();
            controllerBindingsConfig.Value = keyboardPlayer.Save();

            keyboardPlayer.ListenOptions.OnBindingFound += OnBindingFound;
            keyboardPlayer.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
            keyboardPlayer.ListenOptions.OnBindingAdded += OnBindingAdded;
            controllerPlayer.ListenOptions.OnBindingFound += OnBindingFoundController;
            controllerPlayer.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
            controllerPlayer.ListenOptions.OnBindingAdded += OnBindingAdded;
        }

        private void LoadAssets()
        {
            UnityEngine.Debug.Log($"[RebindControls] Attempting to load assetbundle.");
            UIAssets = AssetUtils.LoadAssetBundleFromResources("rebindcontrolsui", typeof(RebindControls).Assembly);

            UnityEngine.Debug.Log($"[RebindControls] Attempting to load assets.");
            containerFrameAsset = UIAssets.LoadAsset<GameObject>("BindingGridFrame");
            keyGroupAsset = UIAssets.LoadAsset<GameObject>("KeyGroup");
            bindingButtonAsset = UIAssets.LoadAsset<GameObject>("KeyBindingGroup");
            click = UIAssets.LoadAllAssets<AudioClip>().ToList().Where(clip => { /*UnityEngine.Debug.Log($"Checking the name of {clip}");*/ return clip.name.Contains("UI_Button_Click"); }).ToList();
            hover = UIAssets.LoadAllAssets<AudioClip>().ToList().Where(clip => { /*UnityEngine.Debug.Log($"Checking the name of {clip}");*/ return clip.name.Contains("UI_Button_Hover"); }).ToList();
        }

        private void OnBindingAdded(PlayerAction action, BindingSource binding)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            Debug.Log("Binding added... " + binding.DeviceName + ": " + binding.Name);
            UnityEngine.Debug.Log("Binding added... " + binding.DeviceName + ": " + binding.Name);
            RebindControls.instance.ExecuteAfterFrames(1, SaveLayouts);
        }

        private bool OnBindingFound(PlayerAction action, BindingSource binding)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            if (binding == new KeyBindingSource(Key.Escape))
            {
                action.StopListeningForBinding();
                return false;
            }
            return true;
        }

        private bool OnBindingFoundController(PlayerAction action, BindingSource binding)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            if (binding.BindingSourceType != BindingSourceType.DeviceBindingSource)
            {
                action.StopListeningForBinding();
                return false;
            }
            return true;
        }

        private void OnBindingRejectedMenu(PlayerAction action, BindingSource binding, BindingSourceRejectionType reason)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            Debug.Log("Binding rejected... " + reason);
            UnityEngine.Debug.Log("Binding rejected... " + reason);
        }

        private void MainGui(GameObject menu)
        {
            MenuHandler.CreateText("Keyboard Layout", menu, out TextMeshProUGUI _, 60);
            var blah = CreateKeyBindingLayout(menu, keyboardPlayer);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);

            MenuHandler.CreateButton("Reset keyboard layout to default".ToUpper(), menu, OnResetKeyboardLayoutPressed, 70);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);

            MenuHandler.CreateText("Controller Layout", menu, out TextMeshProUGUI _, 60);
            var bleh = CreateKeyBindingLayout(menu, controllerPlayer);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);

            MenuHandler.CreateButton("Reset controller layout to default".ToUpper(), menu, OnResetControllerLayoutPressed, 70);

            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 70);

            MenuHandler.CreateButton(" ".ToUpper(), menu, null, 30);

            UpdateDisplayedBindings();
        }

        public void UpdateDisplayedBindings()
        {
            foreach (var layout in layouts)
            {
                foreach (var actionFrame in layout.Value)
                {
                    var info = actionFrame.Key.GetComponent<BindingInfo>();

                    var action = info.action;

                    var i = 0;

                    foreach (var bindingFrame in actionFrame.Value)
                    {
                        var text = bindingFrame.transform.Find("Binding/Name").gameObject.GetComponent<TextMeshProUGUI>();

                        if (i < action.Bindings.Count())
                        {
                            text.text = action.Bindings[i].Name;
                        }
                        else if (action.IsListeningForBinding)
                        {
                            text.text = "Waiting for input...";
                        }
                        else
                        {
                            text.text = "";
                        }

                        i++;
                    }
                }
            }
        }

        private void OnResetKeyboardLayoutPressed()
        {
            UnityEngine.Debug.Log($"[RebindControls] Reset Keyboard Layout pressed.");
            keyboardPlayer.Reset();
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            RebindControls.instance.ExecuteAfterFrames(1, SaveLayouts);
        }

        private void OnResetControllerLayoutPressed()
        {
            UnityEngine.Debug.Log($"[RebindControls] Reset Controller Layout pressed.");
            controllerPlayer.Reset();
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            RebindControls.instance.ExecuteAfterFrames(1, SaveLayouts);
        }

        private void OnClearButtonPressed(PlayerAction action, int slot)
        {
            UnityEngine.Debug.Log($"[RebindControls] Clear Button Pressed");
            if (slot < action.Bindings.Count)
            {
                action.RemoveBindingAt(slot);
            }
            
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            RebindControls.instance.ExecuteAfterFrames(1, SaveLayouts);
        }

        private void OnBindingButtonPressed(PlayerAction action, int slot)
        {
            UnityEngine.Debug.Log($"[RebindControls] Binding Button Pressed");

            if (slot < action.UnfilteredBindings.Count)
            {
                UnityEngine.Debug.Log($"[RebindControls] Slot {slot} has found {action.UnfilteredBindings.Count} bindings on {action.Name}. Replacing existing binding now.");
                action.ListenForBindingReplacing(action.UnfilteredBindings[slot]);
            }
            else
            {
                UnityEngine.Debug.Log($"[RebindControls] Slot {slot} has found {action.UnfilteredBindings.Count} bindings on {action.Name}. Listening for a binding.");
                action.ListenForBinding();
            }
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
        }

        private void OnResetButtonPressed(PlayerAction action)
        {
            action.ResetBindings();
            UnityEngine.Debug.Log($"[RebindControls] Reset Button Pressed");
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            RebindControls.instance.ExecuteAfterFrames(1, SaveLayouts);
        }

        private GameObject CreateKeyBindingLayout(GameObject parent, PlayerActions actions)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }

            var container = GameObject.Instantiate(containerFrameAsset, parent.transform);

            var grid = container.GetOrAddComponent<GridLayoutGroup>();

            layouts.Add(container, new Dictionary<GameObject, List<GameObject>>());

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
            interact.Setup();
            interact.mouseClick.AddListener(() => OnResetButtonPressed(action));

            var bindingGroup = keyGroup.transform.Find("Bindings").gameObject;

            var info = keyGroup.AddComponent<BindingInfo>();

            layouts[parent].Add(keyGroup, new List<GameObject>());

            for (int i = 0; i < 3; i++)
            {
                var bindingKey = CreateKeyBindButton(bindingGroup, action, i);
                info.bindingKeys.Add(bindingKey.transform.Find("Binding/Name").gameObject.GetComponent<TextMeshProUGUI>());
                layouts[parent][keyGroup].Add(bindingKey);
            }
            info.action = action;

            return keyGroup;
        }

        private GameObject CreateKeyBindButton(GameObject parent, PlayerAction action, int slot)
        {
            var keyBindingGroup = GameObject.Instantiate(bindingButtonAsset, parent.transform);

            var bindingButton = keyBindingGroup.transform.Find("Binding").gameObject;

            var bindingText = bindingButton.transform.Find("Name").gameObject.GetComponent<TextMeshProUGUI>();

            var clear = keyBindingGroup.transform.Find("Clear").gameObject;

            var interact = bindingButton.GetOrAddComponent<ButtonInteraction>();
            interact.Setup();
            interact.mouseClick.AddListener(() => OnBindingButtonPressed(action, slot));

            interact = clear.GetOrAddComponent<ButtonInteraction>();
            interact.Setup();
            interact.mouseClick.AddListener(() => OnClearButtonPressed(action, slot));

            return keyBindingGroup;
        }
    }

    public class BindingInfo : MonoBehaviour
    {
        public PlayerAction action;
        public List<TextMeshProUGUI> bindingKeys = new List<TextMeshProUGUI>();
    }

    public class ButtonInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEvent mouseClick = new UnityEvent();
        public UnityEvent leftClick = new UnityEvent();
        public UnityEvent middleClick = new UnityEvent();
        public UnityEvent rightClick = new UnityEvent();
        public UnityEvent mouseEnter = new UnityEvent();
        public UnityEvent mouseExit = new UnityEvent();
        public Button button;
        public AudioSource source;
        public static ButtonInteraction instance;

        private System.Random random = new System.Random();

        private void Start()
        {
            instance = this;
            button = gameObject.GetComponent<Button>();
            source = gameObject.GetOrAddComponent<AudioSource>();
            //mouseClick = new UnityEvent();
            //leftClick = new UnityEvent();
            //middleClick = new UnityEvent();
            //rightClick = new UnityEvent();
            //mouseEnter = new UnityEvent();
            //mouseExit = new UnityEvent();

            mouseEnter.AddListener(OnEnter);
            mouseExit.AddListener(OnExit);
            mouseClick.AddListener(OnClick);
            mouseClick.AddListener(RebindControls.instance.UpdateDisplayedBindings);
        }

        public void Setup()
        {
            //mouseEnter = new UnityEvent();
            //mouseClick = new UnityEvent();
            //mouseEnter.AddListener(OnEnter);
            //mouseClick.AddListener(OnClick);
        }

        public void OnEnter()
        {
            //UnityEngine.Debug.Log($"Button Enter");
            source.PlayOneShot(RebindControls.instance.hover[random.Next(RebindControls.instance.hover.Count)]);
        }

        public void OnExit()
        {
            //UnityEngine.Debug.Log($"Button Exit");
            source.PlayOneShot(RebindControls.instance.hover[random.Next(RebindControls.instance.hover.Count)]);
        }

        public void OnClick()
        {
            //UnityEngine.Debug.Log($"Button Clicked");
            source.PlayOneShot(RebindControls.instance.click[random.Next(RebindControls.instance.click.Count)]);
            EventSystem.current.SetSelectedGameObject(null);
            //Destroy(gameObject);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            mouseEnter?.Invoke();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            mouseExit?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                leftClick?.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Middle)
                middleClick?.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Right)
                rightClick?.Invoke();
            mouseClick?.Invoke();
        }
    }
}