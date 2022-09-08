using System.ComponentModel;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ButlerWindow
{
    [FilePath("ButlerSettings.asset", FilePathAttribute.Location.ProjectSettings)]
    public class ButlerSettings : ScriptableSingleton<ButlerSettings>
    {
        protected override bool autoSave => true;
        protected override SerializationMode defaultSerializationMode => SerializationMode.Text;
        
        public SupportedBuildTarget BuildTarget = SupportedBuildTarget.WebGL;

        private const string BuildsDir = "Builds";
        public string GetDefaultBuildDirectory(SupportedBuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case SupportedBuildTarget.Windows: return Path.Combine(BuildsDir, "Windows");
                case SupportedBuildTarget.WebGL: return Path.Combine(BuildsDir, "WebGL");
                case SupportedBuildTarget.Android: return Path.Combine(BuildsDir, "Android");
                default: return Path.Combine(BuildsDir, "LatestBuild");
            }
        }

        public string AddFileNameIfNecessary(string path, SupportedBuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case SupportedBuildTarget.Android: return Path.Combine(path, Application.productName + ".apk");
                case SupportedBuildTarget.Windows: return Path.Combine(path, Application.productName + ".exe");
                default: return path;
            }
        }

        public string Account = "";
        public string Project = "";
        public bool OverrideChannel = false;
        public string Channel = "";
        public bool OverrideVersion = false;
        public string Version = "";
        [FormerlySerializedAs("OverrideBuildPath")] public bool OverrideBuildDirectory;
        [FormerlySerializedAs("BuildPath")] public string BuildDirectory;
        public bool ConfirmUpload = true;
        
        public string GetChannel() => OverrideChannel ? Channel : BuildTarget.ToString();
        public string GetBuildDirectory() => OverrideBuildDirectory ? BuildDirectory : GetDefaultBuildDirectory(BuildTarget);
        public string GetBuildPath() => AddFileNameIfNecessary(GetBuildDirectory(), BuildTarget);
        
        string GetPushDirectory(SupportedBuildTarget target)
        {
            switch (target)
            {
                // On Android we want to push the APK, on other platforms we push the build directory.
                case SupportedBuildTarget.Android:
                    return GetBuildPath();
                case SupportedBuildTarget.WebGL:
                case SupportedBuildTarget.Windows:
                    return GetBuildDirectory();
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
        
        public string GetURL() => $"https://{Account}.itch.io/{Project}";
        public string ToPushArgs()
        {
          var args = $"push \"{GetPushDirectory(BuildTarget)}\" {Account}/{Project}:{GetChannel()}";
          if (OverrideVersion) args += $" --userversion {Version}";
          return args;
        }

        public enum SupportedBuildTarget
        {
            Windows = 19,
            Android = 13,
            WebGL = 20,
        }
        
    }
}
