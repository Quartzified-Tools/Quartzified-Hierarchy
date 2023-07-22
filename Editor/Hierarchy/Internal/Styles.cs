using UnityEngine;

namespace Quartzified.Tools.Hierarchy
{
    internal static class Styles
    {
        internal static GUIStyle Tag = new GUIStyle()
        {
            padding = new RectOffset(3, 4, 0, 0),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic,
            fontSize = 8,
            richText = true,
            border = new RectOffset(12, 12, 8, 8),
        };

        internal static GUIStyle Layer = new GUIStyle()
        {
            padding = new RectOffset(3, 4, 0, 0),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Italic,
            fontSize = 8,
            richText = true,
            border = new RectOffset(12, 12, 8, 8),
        };

        internal static GUIStyle Header = new GUIStyle(TreeBoldLabel)
        {
            richText = true,
            normal = new GUIStyleState() { textColor = Color.white }
        };

        internal static GUIStyle TreeBoldLabel
        {
            get { return UnityEditor.IMGUI.Controls.TreeView.DefaultStyles.boldLabel; }
        }

        internal static GUIStyle TreeLabel = new GUIStyle(UnityEditor.IMGUI.Controls.TreeView.DefaultStyles.label)
        {
            richText = true,
            normal = new GUIStyleState() { textColor = Color.white }
        };
    }
}


