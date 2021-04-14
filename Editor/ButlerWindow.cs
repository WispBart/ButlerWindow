using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace ButlerWindow
{
    public class ButlerWindow : EditorWindow
    {
        public const string TITLE = "Upload to itch.io";
        public readonly string ShareUXML = "Packages/com.wispbart.butlerwindow/UI/ButlerWindow_Share.uxml";
        public readonly string DownloadUXML = "Packages/com.wispbart.butlerwindow/UI/ButlerWindow_Download.uxml";
        public readonly string WrongPlatformUXML = "Packages/com.wispbart.butlerwindow/UI/ButlerWindow_WrongPlatform.uxml";
        public readonly string MainStyleSheet = "Packages/com.wispbart.butlerwindow/UI/ButlerWindow.uss";

        public delegate void BuildCompleteHandler(BuildReport report);
        public static event BuildCompleteHandler OnBuildComplete;
        
        private ButlerWin64 _butler;
        private ButlerSettings _settings => ButlerSettings.instance;
        private Toggle _devBuildToggle;
        private VisualElement _downloadPage;
        private VisualElement _sharePage;
        private TextField _console;

        [MenuItem("Window/Upload to itch.io")]
        public static void Open()
        {
            ButlerWindow wnd = GetWindow<ButlerWindow>();
            wnd.titleContent = new GUIContent(TITLE);
            wnd.minSize = new Vector2(400, 500);
        }

        void OnEnable()
        {
            InitializeButlerIfNecessary();
        }

        private void OnDisable()
        {
            DestroyImmediate(_butler);
        }

        void InitializeButlerIfNecessary()
        {
            if (_butler != null) return;
            _butler = CreateInstance<ButlerWin64>();
            _butler.SetConsoleMessage = SetConsoleContents;
            _butler.AppendConsoleMessage = AppendConsoleMessage;
        }


        public void CreateGUI()
        {
            InitializeButlerIfNecessary();

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(MainStyleSheet);

            if (!IsEditorSupported())
            {
                var platformNotSupported = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WrongPlatformUXML).CloneTree();
                platformNotSupported.styleSheets.Add(styleSheet);
                ShowPage(platformNotSupported);
                return;
            }
            
            // Create Download Page
            _downloadPage = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DownloadUXML).CloneTree();
            _downloadPage.styleSheets.Add(styleSheet);
            
            var downloadButton = _downloadPage.Q<Button>("downloadButton");
            var downloadProgress = _downloadPage.Q<ProgressBar>("downloadProgress");

            downloadProgress.visible = false;
            downloadButton.clicked += () =>
            {
                downloadProgress.visible = true;
                _butler.DownloadButler(
                    progress => downloadProgress.value = progress,
                    onComplete: () => ShowPage(_sharePage));
            };

            // Create Share Page
            var settingsSo = new SerializedObject(_settings);
            // Import Share UXML
            _sharePage = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShareUXML).CloneTree();
            _sharePage.styleSheets.Add(styleSheet);
            
            // Authenticate Butler
            _sharePage.Q<Button>("auth").clicked += _butler.Login;
            _sharePage.Q<Button>("deAuth").clicked += () => SetConsoleContents(_butler.Logout());
            _sharePage.Q<Button>("update").clicked += () => SetConsoleContents(_butler.CheckForUpdates());
            

            _sharePage.Q<EnumField>("buildTarget").BindProperty(settingsSo.FindProperty("BuildTarget"));
            // Account, Project & URL
            var acct = _sharePage.Q<TextField>("account");
            acct.BindProperty(settingsSo.FindProperty("Account"));
            var prjct = _sharePage.Q<TextField>("project");
            prjct.BindProperty(settingsSo.FindProperty("Project"));
            var urlDisplay = _sharePage.Q<Label>("projectUrl");
            urlDisplay.RegisterCallback<MouseUpEvent>((cb) => Application.OpenURL(_settings.GetURL()));
            urlDisplay.text = _settings.GetURL();
            acct.RegisterValueChangedCallback((_) => urlDisplay.text = _settings.GetURL());
            prjct.RegisterValueChangedCallback((_) => urlDisplay.text = _settings.GetURL());

            // Channel
            var channel = _sharePage.Q<TextField>("channel");
            channel.BindProperty(settingsSo.FindProperty("Channel"));
            var overrideChannel = _sharePage.Q<Toggle>("overrideChannel");
            overrideChannel.BindProperty(settingsSo.FindProperty("OverrideChannel"));
            channel.visible = overrideChannel.value;
            overrideChannel.RegisterValueChangedCallback((x) => channel.visible = x.newValue);
            // Version
            var version = _sharePage.Q<TextField>("version");
            version.BindProperty(settingsSo.FindProperty("Version"));
            var overrideVersion = _sharePage.Q<Toggle>("overrideVersion");
            overrideVersion.BindProperty(settingsSo.FindProperty("OverrideVersion"));
            version.visible = overrideVersion.value;
            overrideVersion.RegisterValueChangedCallback((x) => version.visible = x.newValue);
            
            //buildPath
            var buildPath = _sharePage.Q<TextField>("buildPath");
            buildPath.BindProperty(settingsSo.FindProperty("BuildPath"));
            var overridebuildPath = _sharePage.Q<Toggle>("overrideBuildPath");
            buildPath.visible = overridebuildPath.value;
            overridebuildPath.RegisterValueChangedCallback((x) => buildPath.visible = x.newValue);


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
            if (!_butler.IsInstalled)
            {
                ShowPage(_downloadPage);
                return;
            }
            ShowPage(_sharePage);
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
                locationPathName = _settings.GetBuildPath(),
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None,
            });
            OnBuildComplete?.Invoke(report);
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