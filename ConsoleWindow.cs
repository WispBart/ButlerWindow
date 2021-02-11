
using UnityEditor;
using UnityEngine;

namespace ButlerWindow
{
    public class ConsoleWindow : EditorWindow
    {
        public string Contents;

        public void SetContents(string message)
        {
            Contents = message;
        }
        
        public void AppendContents(string message)
        {
            Contents += message;
        }
        
        public void Clear()
        {
            Contents = string.Empty;
        }

        
        void OnGUI()
        {
            if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Clear"), false, Clear);
                menu.ShowAsContext();
            }
            EditorGUILayout.TextArea(Contents, GUILayout.ExpandHeight(true));
        }

    }
}

