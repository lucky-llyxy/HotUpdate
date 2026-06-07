using HotUpdateDemo.Core;
using HotUpdateDemo.UI;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HotUpdateDemo.Editor
{
    public static class HotUpdateDemoSetup
    {
        private const string DemoRoot = "Assets/HotUpdateDemo";
        private const string BootstrapScenePath = DemoRoot + "/Scenes/Bootstrap.unity";
        private const string UpdatePanelPrefabPath = DemoRoot + "/Prefabs/UI/UpdatePanel.prefab";
        private const string LoginPanelPrefabPath = DemoRoot + "/Prefabs/UI/LoginPanel.prefab";
        private const string LocalUiGroupName = "Local UI";
        private const string LoginPanelAddress = "ui/login_panel";
        private static AddRequest addAddressablesRequest;

        [MenuItem("HotUpdate Demo/02 Install Addressables Package")]
        public static void InstallAddressablesPackage()
        {
            if (addAddressablesRequest != null && !addAddressablesRequest.IsCompleted)
            {
                Debug.Log("Addressables installation is already running.");
                return;
            }

            addAddressablesRequest = Client.Add("com.unity.addressables");
            EditorApplication.update += CheckAddressablesInstallResult;
            Debug.Log("Installing Addressables package...");
        }

        private static void CheckAddressablesInstallResult()
        {
            if (addAddressablesRequest == null || !addAddressablesRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= CheckAddressablesInstallResult;

            if (addAddressablesRequest.Status == StatusCode.Success)
            {
                Debug.Log($"Addressables installed: {addAddressablesRequest.Result.packageId}");
            }
            else
            {
                Debug.LogError($"Addressables installation failed: {addAddressablesRequest.Error.message}");
            }

            addAddressablesRequest = null;
        }

        [MenuItem("HotUpdate Demo/01 Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Bootstrap";

            CreateCamera();
            CreateEventSystem();

            var canvas = CreateCanvas();
            var updatePanel = CreateUpdatePanel(canvas.transform);
            CreateAppEntry(updatePanel);

            PrefabUtility.SaveAsPrefabAsset(updatePanel.gameObject, UpdatePanelPrefabPath);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Bootstrap scene created: {BootstrapScenePath}");
        }

        [MenuItem("HotUpdate Demo/03 Create Local Addressables UI")]
        public static void CreateLocalAddressablesUi()
        {
            EnsureFolders();

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("Failed to create Addressables settings.");
                return;
            }

            CreateLoginPanelPrefab();
            MarkLoginPanelAddressable(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created Addressable prefab: {LoginPanelAddress} -> {LoginPanelPrefabPath}");
        }

        [MenuItem("HotUpdate Demo/04 Build Addressables Content")]
        public static void BuildAddressablesContent()
        {
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("Addressables content build finished.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "HotUpdateDemo");
            EnsureFolder(DemoRoot, "Scenes");
            EnsureFolder(DemoRoot, "Prefabs");
            EnsureFolder(DemoRoot + "/Prefabs", "UI");
            EnsureFolder(DemoRoot, "Scripts");
            EnsureFolder(DemoRoot + "/Scripts", "Core");
            EnsureFolder(DemoRoot + "/Scripts", "UI");
            EnsureFolder(DemoRoot, "Art");
            EnsureFolder(DemoRoot, "Configs");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();

            var inputSystemModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                eventSystemObject.AddComponent(inputSystemModuleType);
                return;
            }

            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static UpdatePanel CreateUpdatePanel(Transform parent)
        {
            var panelObject = CreateRect("UpdatePanel", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var background = panelObject.AddComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.09f, 1f);

            var content = CreateRect("Content", panelObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(720f, 560f));
            var contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.12f, 0.14f, 0.18f, 1f);

            var title = CreateText("Title", content.transform, "HotUpdate Demo", 44, FontStyle.Bold, new Color(0.96f, 0.97f, 1f), TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(620f, 80f));

            var subtitle = CreateText("Subtitle", content.transform, "Bootstrap -> Check Update -> Enter Game", 24, FontStyle.Normal, new Color(0.63f, 0.7f, 0.82f), TextAnchor.MiddleCenter);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(620f, 50f));

            var status = CreateText("StatusText", content.transform, "等待启动...", 28, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter);
            SetRect(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(620f, 60f));

            var slider = CreateSlider(content.transform);
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(560f, 36f));

            var percent = CreateText("PercentText", content.transform, "0%", 24, FontStyle.Bold, new Color(0.8f, 0.88f, 1f), TextAnchor.MiddleCenter);
            SetRect(percent.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(200f, 44f));

            var button = CreateButton(content.transform, "EnterButton", "进入游戏");
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 85f), new Vector2(260f, 72f));

            var updatePanel = panelObject.AddComponent<UpdatePanel>();
            var serializedObject = new SerializedObject(updatePanel);
            serializedObject.FindProperty("statusText").objectReferenceValue = status;
            serializedObject.FindProperty("percentText").objectReferenceValue = percent;
            serializedObject.FindProperty("progressSlider").objectReferenceValue = slider;
            serializedObject.FindProperty("enterButton").objectReferenceValue = button;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return updatePanel;
        }

        private static void CreateAppEntry(UpdatePanel updatePanel)
        {
            var appEntryObject = new GameObject("AppEntry");
            var appEntry = appEntryObject.AddComponent<AppEntry>();

            var serializedObject = new SerializedObject(appEntry);
            serializedObject.FindProperty("updatePanel").objectReferenceValue = updatePanel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLoginPanelPrefab()
        {
            var panelObject = CreateRect("LoginPanel", null, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero);
            var background = panelObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.11f, 0.16f, 1f);

            var content = CreateRect("Content", panelObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(680f, 520f));
            var contentImage = content.AddComponent<Image>();
            contentImage.color = new Color(0.14f, 0.18f, 0.25f, 1f);

            var title = CreateText("Title", content.transform, "Login Panel", 48, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(560f, 80f));

            var info = CreateText("Info", content.transform, "Loaded by Addressables\nAddress: ui/login_panel\nLabel: ui", 28, FontStyle.Normal, new Color(0.72f, 0.8f, 0.92f), TextAnchor.MiddleCenter);
            SetRect(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(560f, 180f));

            var button = CreateButton(content.transform, "StartButton", "开始游戏");
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(260f, 72f));

            PrefabUtility.SaveAsPrefabAsset(panelObject, LoginPanelPrefabPath);
            Object.DestroyImmediate(panelObject);
        }

        private static void MarkLoginPanelAddressable(AddressableAssetSettings settings)
        {
            var group = settings.FindGroup(LocalUiGroupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    LocalUiGroupName,
                    false,
                    false,
                    true,
                    null,
                    typeof(ContentUpdateGroupSchema),
                    typeof(BundledAssetGroupSchema));
            }

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema != null)
            {
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            }

            var guid = AssetDatabase.AssetPathToGUID(LoginPanelPrefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, false, true);
            entry.SetAddress(LoginPanelAddress);
            entry.SetLabel("ui", true, true, true);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true, true);
        }

        private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            SetRect(gameObject.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, Vector2.zero, size);
            return gameObject;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = Vector3.one;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment)
        {
            var gameObject = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var text = gameObject.AddComponent<Text>();
            text.text = content;
            text.font = GetBuiltinFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Slider CreateSlider(Transform parent)
        {
            var root = CreateRect("ProgressSlider", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var background = root.AddComponent<Image>();
            background.color = new Color(0.22f, 0.25f, 0.31f, 1f);

            var fillArea = CreateRect("Fill Area", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-18f, -8f));
            var fill = CreateRect("Fill", fillArea.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.24f, 0.55f, 1f, 1f);

            var slider = root.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = background;
            return slider;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var buttonObject = CreateRect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.24f, 0.55f, 1f, 1f);

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.34f, 0.64f, 1f, 1f);
            colors.pressedColor = new Color(0.16f, 0.42f, 0.82f, 1f);
            button.colors = colors;

            var text = CreateText("Text", buttonObject.transform, label, 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            return button;
        }

        private static Font GetBuiltinFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
