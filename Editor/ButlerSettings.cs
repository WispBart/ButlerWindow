using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ButlerWindow
{
    [FilePath("ButlerSettings.asset", FilePathAttribute.Location.ProjectSettings)]
    public class ButlerSettings : ScriptableSingleton<ButlerSettings>
    {
        protected override bool autoSave => true;
        protected override SerializationMode defaultSerializationMode => SerializationMode.Text;
        
        public SupportedBuildTarget BuildTarget = SupportedBuildTarget.WebGL;
        public string GetDefaultBuildPath(SupportedBuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case SupportedBuildTarget.WebGL: return "Builds/WebGL";
                default: return "Builds/latestBuild";
            }
        }
        public string Account = "";
        public string Project = "";
        public bool OverrideChannel = false;
        public string Channel = "";
        public bool OverrideVersion = false;
        public string Version = "";
        public bool OverrideBuildPath;
        public string BuildPath;

        public string GetChannel() => OverrideChannel ? Channel : BuildTarget.ToString();
        public string GetBuildPath() => OverrideBuildPath ? BuildPath : GetDefaultBuildPath(BuildTarget);
        
        public string GetURL() => $"https://{Account}.itch.io/{Project}";
        public string ToPushArgs()
        {
          var args = $"push {BuildPath} {Account}/{Project}:{GetChannel()}";
          if (OverrideVersion) args += $" --userversion {Version}";
          return args;
        } 

        public enum SupportedBuildTarget
        {
            WebGL = 20,
        }
    }

    public class ButlerSettingsEditor : Editor
    {
        private string[] excludedProps = new string[3];

        private SerializedProperty overrideChannel;
        private SerializedProperty overrideVersion;
        void GetExcludedProps()
        {
            var t = serializedObject.targetObject as ButlerSettings;
            excludedProps[0] = "m_Script";
            excludedProps[1] = overrideChannel.boolValue ? "" : "Channel";
            excludedProps[2] = overrideVersion.boolValue ? "" : "Version";
        }

        void OnEnable()
        {
            overrideChannel = serializedObject.FindProperty("OverrideChannel");
            overrideVersion = serializedObject.FindProperty("OverrideVersion");
        }
        
        public override void OnInspectorGUI()
        {
            GetExcludedProps();
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, excludedProps);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
