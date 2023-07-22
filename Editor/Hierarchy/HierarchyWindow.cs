using System;
using System.Collections.Generic;
using UnityEditor;

namespace Quartzified.Tools.Hierarchy
{
    sealed class HierarchyWindow
    {
        public static Dictionary<int, EditorWindow> instances = new Dictionary<int, EditorWindow>();
        public static List<HierarchyWindow> windows = new List<HierarchyWindow>();

        public int instanceID = Int32.MinValue;
        public EditorWindow editorWindow;

        public HierarchyWindow(EditorWindow editorWindow)
        {
            this.editorWindow = editorWindow;

            instanceID = this.editorWindow.GetInstanceID();

            instances.Add(instanceID, this.editorWindow);
            windows.Add(this);

            // Debug.Log(string.Format("HierarchyWindow {0} Instanced.", instanceID));
        }


        public void Dispose()
        {
            editorWindow = null;
            instances.Remove(instanceID);
            windows.Remove(this);

            // Debug.Log(string.Format("HierarchyWindow {0} Disposed.", instanceID));
        }

        public void SetWindowTitle(string value)
        {
            if (editorWindow == null)
                return;

            editorWindow.titleContent.text = value;
        }
    }
}