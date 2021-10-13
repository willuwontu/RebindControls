using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using InControl;
using Jotunn.Utils;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnboundLib;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        public const string Version = "1.1.0"; // What version are we on (major.minor.patch)?

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

        private Dictionary<GameObject, Dictionary<BindingInfo, List<TextMeshProUGUI>>> layouts = new Dictionary<GameObject, Dictionary<BindingInfo, List<TextMeshProUGUI>>>();

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

        /***********************************
         ****** Layout Setup Functions *****
         ***********************************/
        private PlayerActions DefaultKeyboardLayout()
        {
            PlayerActions playerActions = PlayerActions.CreateWithKeyboardBindings();
            playerActions.ListenOptions.IncludeUnknownControllers = true;
            playerActions.ListenOptions.IncludeMouseButtons = true;
            playerActions.ListenOptions.MaxAllowedBindings = 3U;
            playerActions.ListenOptions.UnsetDuplicateBindingsOnSet = true;
            playerActions.ListenOptions.OnBindingFound = null;
            playerActions.ListenOptions.OnBindingRejected = null;
            playerActions.ListenOptions.OnBindingAdded = null;
            playerActions.ListenOptions.OnBindingFound += OnBindingFoundKeyboard;
            playerActions.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
            playerActions.ListenOptions.OnBindingAdded += OnBindingAdded;

            return playerActions;
        }

        private PlayerActions DefaultControllersLayout()
        {
            PlayerActions playerActions = PlayerActions.CreateWithControllerBindings();
            playerActions.ListenOptions.IncludeUnknownControllers = true;
            playerActions.ListenOptions.IncludeMouseButtons = true;
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
            keyboardPlayer.ListenOptions.OnBindingFound -= OnBindingFoundKeyboard;
            keyboardPlayer.ListenOptions.OnBindingRejected -= OnBindingRejectedMenu;
            keyboardPlayer.ListenOptions.OnBindingAdded -= OnBindingAdded;
            controllerPlayer.ListenOptions.OnBindingFound -= OnBindingFoundController;
            controllerPlayer.ListenOptions.OnBindingRejected -= OnBindingRejectedMenu;
            controllerPlayer.ListenOptions.OnBindingAdded -= OnBindingAdded;

            keyboardBindingsConfig.Value = keyboardPlayer.Save();
            controllerBindingsConfig.Value = controllerPlayer.Save();

            keyboardPlayer.ListenOptions.OnBindingFound += OnBindingFoundKeyboard;
            keyboardPlayer.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
            keyboardPlayer.ListenOptions.OnBindingAdded += OnBindingAdded;
            controllerPlayer.ListenOptions.OnBindingFound += OnBindingFoundController;
            controllerPlayer.ListenOptions.OnBindingRejected += OnBindingRejectedMenu;
            controllerPlayer.ListenOptions.OnBindingAdded += OnBindingAdded;

            foreach (var player in PlayerManager.instance.players)
            {
                PlayerActions temp = new PlayerActions();
                if (player.data.playerActions.Device != null)
                {
                    temp.Load(controllerPlayer.Save());
                    player.data.playerActions = temp;
                }
                else
                {
                    temp.Load(keyboardPlayer.Save());
                    player.data.playerActions = temp;
                }
            }
        }

        /// <summary>
        /// Loads the assets used into their proper variables for usage.
        /// </summary>
        private void LoadAssets()
        {
            UnityEngine.Debug.Log($"[RebindControls] Attempting to load assetbundle.");
            UIAssets = AssetUtils.LoadAssetBundleFromResources("rebindcontrolsui", typeof(RebindControls).Assembly);

            UnityEngine.Debug.Log($"[RebindControls] Attempting to load assets.");
            containerFrameAsset = UIAssets.LoadAsset<GameObject>("BindingGridFrame");
            keyGroupAsset = UIAssets.LoadAsset<GameObject>("KeyGroup");
            bindingButtonAsset = UIAssets.LoadAsset<GameObject>("KeyBindingGroup");
            click = UIAssets.LoadAllAssets<AudioClip>().ToList().Where(clip => clip.name.Contains("UI_Button_Click")).ToList();
            hover = UIAssets.LoadAllAssets<AudioClip>().ToList().Where(clip => clip.name.Contains("UI_Button_Hover")).ToList();
        }

        /// <summary>
        /// The callback that is run when a player successfully adds a new keybinding.
        /// </summary>
        /// <param name="action">The action for which the keybinding was added.</param>
        /// <param name="binding">The new binding itself.</param>
        private void OnBindingAdded(PlayerAction action, BindingSource binding)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            Debug.Log("[RebindControls] Binding added... " + binding.DeviceName + ": " + binding.Name);
            UnityEngine.Debug.Log($"[RebindControls] Binding added for {binding.DeviceName}: {binding.Name}");
            RebindControls.instance.ExecuteAfterFrames(1, SaveLayouts);
        }

        /// <summary>
        /// The callback run whenever a listening action finds a new binding, but before it is added.
        /// </summary>
        /// <param name="action">The action for which the keybinding was found.</param>
        /// <param name="binding">The potential new binding.</param>
        /// <returns>True if the binding is valid and will be added to the action, otherwise it returns false.</returns>
        private bool OnBindingFoundKeyboard(PlayerAction action, BindingSource binding)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            if (binding == new KeyBindingSource(Key.Escape))
            {
                action.StopListeningForBinding();
                return false;
            }
            if (binding.BindingSourceType != BindingSourceType.KeyBindingSource && binding.BindingSourceType != BindingSourceType.MouseBindingSource)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// The callback run whenever a listening action finds a new binding, but before it is added.
        /// </summary>
        /// <param name="action">The action for which the keybinding was found.</param>
        /// <param name="binding">The potential new binding.</param>
        /// <returns>True if the binding is valid and will be added to the action, otherwise it will continue listening unless esc is pressed.</returns>
        private bool OnBindingFoundController(PlayerAction action, BindingSource binding)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            if (binding == new KeyBindingSource(Key.Escape))
            {
                action.StopListeningForBinding();
                return false;
            }
            if (binding == new DeviceBindingSource(InputControlType.Back))
            {
                action.StopListeningForBinding();
                return false;
            }
            if (binding.BindingSourceType != BindingSourceType.DeviceBindingSource)
            {
                return false;
            }
            return true;
        }

        private void OnBindingRejectedMenu(PlayerAction action, BindingSource binding, BindingSourceRejectionType reason)
        {
            RebindControls.instance.ExecuteAfterFrames(1, UpdateDisplayedBindings);
            Debug.Log("[RebindControls] Binding rejected... " + reason);
            UnityEngine.Debug.Log("[RebindControls] Binding rejected... " + reason);
        }

        /***********************************
         ******* GUI Setup Functions *******
         ***********************************/
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

            this.ExecuteAfterFrames(5, UpdateDisplayedBindings);
        }

        private GameObject CreateKeyBindingLayout(GameObject parent, PlayerActions actions)
        {
            if (parent.transform.Find("Group/Grid/Scroll View/Viewport/Content"))
            {
                parent = parent.transform.Find("Group/Grid/Scroll View/Viewport/Content").gameObject;
            }

            var container = GameObject.Instantiate(containerFrameAsset, parent.transform);

            var grid = container.GetOrAddComponent<GridLayoutGroup>();

            layouts.Add(container, new Dictionary<BindingInfo, List<TextMeshProUGUI>>());

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
            interact.mouseClick.AddListener(() => OnResetButtonPressed(action));

            var bindingGroup = keyGroup.transform.Find("Bindings").gameObject;

            var info = keyGroup.AddComponent<BindingInfo>();

            layouts[parent].Add(info, new List<TextMeshProUGUI>());

            for (int i = 0; i < 3; i++)
            {
                var bindingKey = CreateKeyBindButton(bindingGroup, action, i);
                layouts[parent][info].Add(bindingKey);
            }
            info.action = action;

            return keyGroup;
        }

        private TextMeshProUGUI CreateKeyBindButton(GameObject parent, PlayerAction action, int slot)
        {
            var keyBindingGroup = GameObject.Instantiate(bindingButtonAsset, parent.transform);

            var bindingButton = keyBindingGroup.transform.Find("Binding").gameObject;

            var bindingText = bindingButton.transform.Find("Name").gameObject.GetComponent<TextMeshProUGUI>();

            var clear = keyBindingGroup.transform.Find("Clear").gameObject;

            var interact = bindingButton.GetOrAddComponent<ButtonInteraction>();
            interact.mouseClick.AddListener(() => OnBindingButtonPressed(action, slot));

            interact = clear.GetOrAddComponent<ButtonInteraction>();
            interact.mouseClick.AddListener(() => OnClearButtonPressed(action, slot));

            return bindingText;
        }

        /***********************************
         **** GUI Interaction Functions ****
         ***********************************/
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
                UnityEngine.Debug.Log($"[RebindControls] Slot {slot+1} has found {action.UnfilteredBindings.Count} bindings on {action.Name}. Replacing existing binding now.");
                action.ListenForBindingReplacing(action.UnfilteredBindings[slot]);
            }
            else
            {
                UnityEngine.Debug.Log($"[RebindControls] Slot {slot+1} has found {action.UnfilteredBindings.Count} bindings on {action.Name}. Listening for a binding.");
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

        /// <summary>
        /// Forces each binding frame to update its current status.
        /// </summary>
        public void UpdateDisplayedBindings()
        {
            foreach (var layout in layouts)
            {
                foreach (var info in layout.Value)
                {
                    var action = info.Key.action;

                    var i = 0;

                    foreach (var text in info.Value)
                    {
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

                        #if DEBUG
                            //UnityEngine.Debug.Log($"[RebindControls] {action.Name} slot {i}: {text.text}"); 
                        #endif

                        i++;
                    }
                }
            }
        }

        /***********************************
         ** Monobehaviors used for buttons *
         ***********************************/
        private class BindingInfo : MonoBehaviour
        {
            public PlayerAction action;
        }

        private class ButtonInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
        {
            public UnityEvent mouseClick = new UnityEvent();
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

                mouseEnter.AddListener(OnEnter);
                mouseExit.AddListener(OnExit);
                mouseClick.AddListener(OnClick);
                mouseClick.AddListener(RebindControls.instance.UpdateDisplayedBindings);
            }

            public void OnEnter()
            {
                source.PlayOneShot(RebindControls.instance.hover[random.Next(RebindControls.instance.hover.Count)]);
            }

            public void OnExit()
            {
                source.PlayOneShot(RebindControls.instance.hover[random.Next(RebindControls.instance.hover.Count)]);
            }

            public void OnClick()
            {
                source.PlayOneShot(RebindControls.instance.click[random.Next(RebindControls.instance.click.Count)]);
                EventSystem.current.SetSelectedGameObject(null);
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
                mouseClick?.Invoke();
            }
        }
    }
}