using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace ButlerWindow
{
    public class ButlerWindow2 : EditorWindow
    {
        public const string TITLE = "Upload to itch.io";
        public readonly string ShareUXML = "Packages/com.wispfire.butlerwindow/UI/ButlerWindow_Share.uxml";
        public readonly string DownloadUXML = "Packages/com.wispfire.butlerwindow/UI/ButlerWindow_Download.uxml";
        public readonly string MainStyleSheet = "Packages/com.wispfire.butlerwindow/UI/ButlerWindow.uss";

        private ButlerWin64 _butler;
        private ButlerSettings _settings => ButlerSettings.instance;
        private Toggle _devBuildToggle;
        private VisualElement _downloadPage;
        private VisualElement _sharePage;
        private TextField _console;

        [MenuItem("Window/Upload to itch.io")]
        public static void Open()
        {
            ButlerWindow2 wnd = GetWindow<ButlerWindow2>();
            wnd.titleContent = new GUIContent(TITLE);
            wnd.minSize = new Vector2(400, 500);
        }

        void OnEnable()
        {
            _butler = CreateInstance<ButlerWin64>();
            _butler.SetConsoleMessage = SetConsoleContents;
            _butler.AppendConsoleMessage = AppendConsoleMessage;
        }

        private void OnDisable()
        {
            DestroyImmediate(_butler);
        }


        public void CreateGUI()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(MainStyleSheet);
            // Create Download Page
            _downloadPage = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DownloadUXML).CloneTree();
            _downloadPage.styleSheets.Add(styleSheet);

            var platformNotSupported = _downloadPage.Q<Label>("platformNotSupported");
            var downloadButton = _downloadPage.Q<Button>("downloadButton");
            var downloadProgress = _downloadPage.Q<ProgressBar>("downloadProgress");

            if (IsEditorSupported()) platformNotSupported.RemoveFromHierarchy();
            else downloadButton.RemoveFromHierarchy();

            downloadProgress.visible = false;
            downloadButton.clicked += () =>
            {
                downloadProgress.visible = true;
                _butler.DownloadButler(
                    progress => downloadProgress.value = progress,
                    onComplete: () => ShowPage(_sharePage));
            };

            // Create Share Page
            var settings = new SerializedObject(ButlerSettings.instance);
            // Import Share UXML
            _sharePage = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShareUXML).CloneTree();
            _sharePage.styleSheets.Add(styleSheet);

            // Authenticate Butler
            _sharePage.Q<Button>("auth").clicked += _butler.Login;
            _sharePage.Q<Button>("deAuth").clicked += () => SetConsoleContents(_butler.Logout());
            _sharePage.Q<Button>("update").clicked += () => SetConsoleContents(_butler.CheckForUpdates());

            _sharePage.Q<EnumField>("buildTarget").BindProperty(settings.FindProperty("BuildTarget"));
            // Account, Project & URL
            var acct = _sharePage.Q<TextField>("account");
            acct.BindProperty(settings.FindProperty("Account"));
            var prjct = _sharePage.Q<TextField>("project");
            prjct.BindProperty(settings.FindProperty("Project"));
            var urlDisplay = _sharePage.Q<Label>("projectUrl");
            urlDisplay.RegisterCallback<MouseUpEvent>((cb) => Application.OpenURL(_settings.GetURL()));
            urlDisplay.text = _settings.GetURL();
            acct.RegisterValueChangedCallback((_) => urlDisplay.text = _settings.GetURL());
            prjct.RegisterValueChangedCallback((_) => urlDisplay.text = _settings.GetURL());

            // Channel
            var channel = _sharePage.Q<TextField>("channel");
            channel.BindProperty(settings.FindProperty("Channel"));
            var overrideChannel = _sharePage.Q<Toggle>("overrideChannel");
            overrideChannel.BindProperty(settings.FindProperty("OverrideChannel"));
            channel.visible = overrideChannel.value;
            overrideChannel.RegisterValueChangedCallback((x) => channel.visible = x.newValue);
            // Version
            var version = _sharePage.Q<TextField>("version");
            version.BindProperty(settings.FindProperty("Version"));
            var overrideVersion = _sharePage.Q<Toggle>("overrideVersion");
            overrideVersion.BindProperty(settings.FindProperty("OverrideVersion"));
            version.visible = overrideVersion.value;
            overrideVersion.RegisterValueChangedCallback((x) => version.visible = x.newValue);
            _sharePage.Q<TextField>("buildPath").BindProperty(settings.FindProperty("BuildPath"));

            _devBuildToggle = _sharePage.Q<Toggle>("devBuild");
            _devBuildToggle.SetValueWithoutNotify(EditorUserBuildSettings.development);
            _devBuildToggle.RegisterValueChangedCallback((x) => EditorUserBuildSettings.development = x.newValue);

            // Build button
            var buildButton = _sharePage.Q<Button>("build");
            buildButton.clicked += Build;

            // Console
            _console = _sharePage.Q<TextField>("console");
            _console.isReadOnly = true;

            // Initialize page
            if (!_butler.IsInstalled) ShowPage(_downloadPage);
            else ShowPage(_sharePage);
        }

        public void ShowPage(VisualElement pageElement)
        {
            rootVisualElement.Clear();
            rootVisualElement.Add(pageElement);
        }

        void SetConsoleContents(string msg)
        {
            _console.value = msg;
        }

        void AppendConsoleMessage(string msg)
        {
            _console.value += msg;
        }

        void ClearConsole() => _console.value = string.Empty;


        void OnGUI()
        {
            // There's no callbacks for certain values; so just update them in the OnGUI update loop.
            _devBuildToggle?.SetValueWithoutNotify(EditorUserBuildSettings.development);
        }


        private void Build()
        {
            bool confirm = EditorUtility.DisplayDialog("Build WebGL Player", "This might take a while... Continue?",
                "Confirm", "Cancel");
            if (!confirm) return;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions()
            {
                scenes = EditorBuildSettings.scenes.Select((scene) => scene.path).ToArray(),
                target = (BuildTarget) _settings.BuildTarget,
                locationPathName = _settings.BuildPath,
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None,
            });
            Share();
        }


        private void Share()
        {
            _butler.UploadBuild(_settings);
        }


        public bool IsEditorSupported()
        {
#if UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
        }
    }
}