using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

#if UNITY_2020_2_OR_NEWER
namespace ButlerWindow
{
    public class ButlerWindow2 : EditorWindow
    {
        public const string TITLE = "Upload to itch.io";
        public readonly string ShareUXML = "Packages/com.wispfire.butlerwindow/UI/ButlerWindow_Share.uxml";
        public readonly string DownloadUXML = "Packages/com.wispfire.butlerwindow/UI/ButlerWindow_Download.uxml";

        private ButlerWin64 _butler;
        private ButlerSettings _settings => ButlerSettings.instance;
        private Toggle _devBuildToggle;
        private VisualElement _downloadPage;
        private VisualElement _sharePage;
        private Label _console;

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
            // Create Download Page
            _downloadPage = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DownloadUXML).Instantiate();

            var platformNotSupported = _downloadPage.Q<Label>("platformNotSupported");
            var downloadButton = _downloadPage.Q<Button>("downloadButton");
            var downloadProgress = _downloadPage.Q<ProgressBar>("downloadProgress");

            if (IsEditorSupported()) platformNotSupported.RemoveFromHierarchy();
            else downloadButton.RemoveFromHierarchy();

            downloadProgress.visible = false;
            downloadButton.RegisterCallback<ClickEvent>(evt =>
            {
                downloadProgress.visible = true;
                _butler.DownloadButler(
                    progress => downloadProgress.value = progress,
                    onComplete: () => ShowPage(_sharePage));
            });

            // Create Share Page
            var settings = new SerializedObject(ButlerSettings.instance);
            // Import Share UXML
            _sharePage = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShareUXML).Instantiate();

            // Authenticate Butler
            _sharePage.Q<Button>("auth").RegisterCallback<ClickEvent>((_) => _butler.Login());
            _sharePage.Q<Button>("deAuth").RegisterCallback<ClickEvent>(_ => SetConsoleContents(_butler.Logout()));
            _sharePage.Q<Button>("update")
                .RegisterCallback<ClickEvent>(_ => SetConsoleContents(_butler.CheckForUpdates()));

            _sharePage.Q<EnumField>("buildTarget").BindProperty(settings.FindProperty("BuildTarget"));
            // Account, Project & URL
            var acct = _sharePage.Q<TextField>("account");
            acct.BindProperty(settings.FindProperty("Account"));
            var prjct = _sharePage.Q<TextField>("project");
            prjct.BindProperty(settings.FindProperty("Project"));
            var urlDisplay = _sharePage.Q<Label>("projectUrl");
            urlDisplay.RegisterCallback<ClickEvent>((cb) => Application.OpenURL(_settings.GetURL()));
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
            buildButton.RegisterCallback<ClickEvent>(_ => Build());

            // Console
            _console = _sharePage.Q<Label>("console");

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
            _console.text = msg;
        }

        void AppendConsoleMessage(string msg)
        {
            _console.text += msg;
        }

        void ClearConsole() => _console.text = string.Empty;


        void OnGUI()
        {
            // There's no callbacks for certain values; so just update them in the OnGUI update loop.
            _devBuildToggle.SetValueWithoutNotify(EditorUserBuildSettings.development);
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
#endif