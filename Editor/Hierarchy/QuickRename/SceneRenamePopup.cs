using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor;

namespace Quartzified.Tools.Hierarchy
{
    public class SceneRenamePopup : EditorWindow
    {
        static EditorWindow window;
        public Scene scene;

        Label labelField;
        TextField nameField;

        public static SceneRenamePopup ShowPopup(Scene scene)
        {
            if (window == null)
                window = ScriptableObject.CreateInstance<SceneRenamePopup>();

            Vector2 v2 = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            window.position = new Rect(v2.x, v2.y, 200, 68);
            window.ShowPopup();
            window.Focus();

            SceneRenamePopup sceneRenamePopup = window as SceneRenamePopup;
            sceneRenamePopup.scene = scene;
            sceneRenamePopup.nameField.value = scene.name;
            sceneRenamePopup.nameField.Query("unity-text-input").First().Focus();

            return sceneRenamePopup;
        }

        public void OnLostFocus() => Close();

        void OnEnable()
        {
            rootVisualElement.StyleBorderWidth(1);
            Color c = new Color32(58, 121, 187, 255);
            rootVisualElement.StyleBorderColor(c);
            rootVisualElement.StyleJustifyContent(Justify.Center);

            labelField = new Label();
            labelField.text = "Quick Rename";
            labelField.StylePaddingTop(4);
            labelField.StylePaddingLeft(4);
            labelField.StylePaddingBottom(4);
            rootVisualElement.Add(labelField);

            nameField = new TextField();
            nameField.RegisterCallback<KeyUpEvent>((evt) =>
            {
                if (evt.keyCode == KeyCode.Return) Apply();
            });
            rootVisualElement.Add(nameField);

            Button apply = new Button(() => { Apply(); });

            apply.text = "Apply";
            rootVisualElement.Add(apply);
        }

        void Apply()
        {
            AssetDatabase.RenameAsset(scene.path, nameField.value);
            rootVisualElement.StyleDisplay(DisplayStyle.None);
            nameField.value = "";
            Close();
        }
    }
}