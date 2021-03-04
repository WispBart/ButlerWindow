using System;
using System.IO;
using UnityEditorInternal;
using UnityEngine;


namespace ButlerWindow
{
    /// Taken from CrispyBeans: https://forum.unity3d.com/threads/missing-documentation-for-scriptable-singleton.292754/
    /// Based on UnityEditorInternal ScriptableSingleton class.
    public class ScriptableSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        protected static T s_Instance;

        public static T instance
        {
            get
            {
                if (ScriptableSingleton<T>.s_Instance == null)
                {
                    ScriptableSingleton<T>.CreateAndLoad();
                }

                return ScriptableSingleton<T>.s_Instance;
            }
        }

        protected ScriptableSingleton()
        {
            if (ScriptableSingleton<T>.s_Instance != null)
            {
                Debug.LogError("ScriptableSingleton already exists. Did you query the singleton in a constructor?");
            }
            else
            {
                ScriptableSingleton<T>.s_Instance = (this as T);
            }
        }

        private static void CreateAndLoad()
        {
            string filePath = FilePathAttribute.GetFilePath(typeof(T)); //ScriptableSingleton<T>.GetFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                InternalEditorUtility.LoadSerializedFileAndForget(filePath);
            }

            if (ScriptableSingleton<T>.s_Instance == null)
            {
                T t = ScriptableObject.CreateInstance<T>();
                t.hideFlags = HideFlags.DontSave;
            }
        }

        protected virtual void Save(bool saveAsText)
        {
            if (ScriptableSingleton<T>.s_Instance == null)
            {
                Debug.Log("Cannot save ScriptableSingleton: no instance!");
                return;
            }

            string filePath = FilePathAttribute.GetFilePath(typeof(T));
            if (!string.IsNullOrEmpty(filePath))
            {
                string directoryName = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                InternalEditorUtility.SaveToSerializedFileAndForget(new T[] {ScriptableSingleton<T>.s_Instance},
                    filePath, saveAsText);
            }
        }

        protected virtual void OnDisable()
        {
            if (autoSave)
            {
                Save(defaultSerializationMode == SerializationMode.Text);
            }
        }

        public void WriteChangesToDisk()
        {
            Save(defaultSerializationMode == SerializationMode.Text);
        }

        protected enum SerializationMode
        {
            Text,
            Binary
        }


        protected virtual SerializationMode defaultSerializationMode
        {
            get { return SerializationMode.Binary; }
        }

        protected virtual bool autoSave
        {
            get { return false; }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FilePathAttribute : Attribute
    {
        public enum Location
        {
            PreferencesFolder,
            ProjectFolder,
            ProjectSettings,
            Library,
        }

        public string filepath { get; set; }

        public FilePathAttribute(string relativePath, FilePathAttribute.Location location)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Debug.LogError("Invalid relative path! (its null or empty)");
                return;
            }

            if (relativePath[0] == '/')
            {
                relativePath = relativePath.Substring(1);
            }

            if (location == Location.PreferencesFolder)
            {
                this.filepath = InternalEditorUtility.unityPreferencesFolder + "/" + relativePath;
            }
            else if (location == Location.Library)
            {
                this.filepath = "Library/" + relativePath;
            }
            else if (location == Location.ProjectSettings)
            {
                this.filepath = "ProjectSettings/" + relativePath;
            }
            else
            {
                this.filepath = relativePath;
            }
        }

        public static string GetFilePath(Type sourceType)
        {
            Type typeFromHandle = sourceType;
            object[] customAttributes = typeFromHandle.GetCustomAttributes(true);
            object[] array = customAttributes;
            for (int i = 0; i < array.Length; i++)
            {
                object obj = array[i];
                if (obj is FilePathAttribute)
                {
                    FilePathAttribute filePathAttribute = obj as FilePathAttribute;
                    return filePathAttribute.filepath;
                }
            }

            return null;
        }
    }
}