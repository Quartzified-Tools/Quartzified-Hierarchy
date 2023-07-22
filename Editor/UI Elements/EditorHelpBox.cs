using UnityEditor;
using UnityEngine.UIElements;

namespace Quartzified.Tools.Hierarchy
{
    public class EditorHelpBox : VisualElement
    {
        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        private string label = "";

        public EditorHelpBox(string text, MessageType messageType, bool wide = true)
        {
            style.marginLeft = style.marginRight = style.marginTop = style.marginBottom = 4;
            Label = text;

            IMGUIContainer iMGUIContainer = new IMGUIContainer(() => { EditorGUILayout.HelpBox(label, messageType, wide); });

            iMGUIContainer.name = nameof(IMGUIContainer);
            Add(iMGUIContainer);
        }
    }
}
