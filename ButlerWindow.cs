using System.Linq;
using Unity.Connect.Share.Editor;
using UnityEditor;
using UnityEngine;

namespace ButlerWindow
{

    public class ButlerWindow : EditorWindow
    {
        public const string TITLE = "Upload to Itch.io";
        private const string CONSOLE_TITLE = "Butler Console";
        private const string NO_WEBGL = "WebGL Module not installed. Please install it via the Unity Hub.";

        readonly static Vector2 MaxWindowSize = new Vector2(800, 800);
        readonly static Vector2 WindowSize = new Vector2(800, 300);

        public ConsoleWindow Console
        {
            get
            {
                if (_console == null) _console = GetWindow<ConsoleWindow>(CONSOLE_TITLE);
                return _console;
            }
        }

        private ConsoleWindow _console;
        private ButlerWin64 _butler;
        private Editor _settingsEditor;
        private ButlerSettings _settings => ButlerSettings.instance;

        [MenuItem("Window/Upload to Itch.io")]
        static void Open()
        {
            var window = GetWindow<ButlerWindow>(TITLE);
            window.minSize = window.maxSize = WindowSize;
        }

        private void OnEnable()
        {
            _butler = CreateInstance<ButlerWin64>();
            _butler.Console = Console;
            _settingsEditor = Editor.CreateEditor(ButlerSettings.instance, typeof(ButlerSettingsEditor));
        }

        private void OnDisable()
        {
            DestroyImmediate(_butler);
        }

        void OnGUI()
        {
            if (!IsEditorSupported())
            {
                EditorGUILayout.HelpBox("ButlerWindow is only supported on Windows.", MessageType.Error);
                return;
            }
            
            if (!_butler.IsInstalled)
            {
                ButlerNotInstalledGUI();
                return;
            }


            
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Butler Authentication");
            //TODO: Can check the pc for credentials to find out if it is authenticated.
            if (GUILayout.Button("Authenticate")) _butler.Login();
            if (GUILayout.Button("Remove Authentication")) Console.Contents = _butler.Logout();
            if (GUILayout.Button("Check for Updates")) Console.SetContents(_butler.CheckForUpdates());
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            _settingsEditor.OnInspectorGUI();
            
            var webGLAvailable = ModuleManagerProxy.IsBuildPlatformInstalled(BuildTarget.WebGL);
            if (!webGLAvailable)
            {
                EditorGUILayout.HelpBox(NO_WEBGL, MessageType.Warning);
            }

            EditorUserBuildSettings.development =
                EditorGUILayout.Toggle("Development Build", EditorUserBuildSettings.development);

            if (GUILayout.Button(_settings.GetURL(), EditorStyles.linkLabel))
            {
                Application.OpenURL(_settings.GetURL());
            }
            
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = webGLAvailable && !_butler.IsUploading;
            if (GUILayout.Button("Build & Share")) Build();
            if (GUILayout.Button("Share")) Share();
            if (_butler.IsUploading) EditorGUILayout.HelpBox("Uploading build to Itch.IO. Check console for progress.", MessageType.Info);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

        }

 
        void ButlerNotInstalledGUI()
        {
            GUILayout.Label("Butler not detected. Press the button to download.");
            
            if (GUILayout.Button("Download Butler"))
            {
                _butler.DownloadButler((x) =>
                {
                    EditorUtility.DisplayProgressBar("Downloading Butler", "", x);
                }, EditorUtility.ClearProgressBar);
            }
        }

        private void Build()
        {
            bool confirm = EditorUtility.DisplayDialog("Build WebGL Player", "This might take a while... Continue?", "Confirm", "Cancel");
            if (!confirm) return;
            
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions()
            {
                scenes = EditorBuildSettings.scenes.Select((scene) => scene.path).ToArray(),
                target = (BuildTarget)_settings.BuildTarget,
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
