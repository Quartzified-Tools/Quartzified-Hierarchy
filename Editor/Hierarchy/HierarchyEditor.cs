using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

namespace Quartzified.Tools.Hierarchy
{
    [InitializeOnLoad]
    public sealed class HierarchyEditor
    {
        internal const int GLOBAL_SPACE_OFFSET_LEFT = 16 * 2;

        static HierarchyEditor instance;

        public static HierarchyEditor Instance
        {
            get
            {
                if (instance == null)
                    instance = new HierarchyEditor();

                return instance;
            }
            private set { instance = value; }
        }

        Dictionary<int, UnityEngine.Object> selectedComponents = new Dictionary<int, UnityEngine.Object>();
        Dictionary<string, string> dicComponents = new Dictionary<string, string>(StringComparer.Ordinal);
        UnityEngine.Object activeComponent;

        GUIContent tooltipContent = new GUIContent();

        HierarchySettings settings;
        HierarchyResources resources;

        HierarchySettings.ThemeData ThemeData
        {
            get { return settings.usedThemeData; }
        }

        int deepestRow = int.MinValue;
        int previousRowIndex = int.MinValue;

        int sceneIndex = 0;
        Scene currentScene;
        Scene previousScene;

        public static bool IsMultiScene
        {
            get { return SceneManager.sceneCount > 1; }
        }

        bool selectionStyleAfterInvoke = false;
        bool checkingAllHierarchy = false;

        Event currentEvent;

        RowItem rowItem = new RowItem();
        RowItem previousElement = null;
        WidthUse widthUse = WidthUse.zero;

        static HierarchyEditor()
        {
            if (instance == null)
                instance = new HierarchyEditor();
        }

        public HierarchyEditor()
        {
            InternalReflection();
            EditorApplication.update += EditorAwake;
            EditorApplication.hierarchyWindowItemOnGUI += UpdateHierarchyItem;
            AssetDatabase.importPackageCompleted += ImportPackageCompleted;
        }

        void UpdateHierarchyItem(int instanceID, Rect selectionRect)
        {
            GameObject hierarchyObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if(hierarchyObject != null)
            {
                HierarchySettings settings = HierarchySettings.instance;
                HierarchySettings.HeaderTagData data = settings?.tagData;
                selectionRect.width = selectionRect.width - 8;

                if (data == null)
                    return; 

                for (int i = 0; i < data.headerCount; i++)
                {
                    if (string.IsNullOrEmpty(data.headerTag[i]))
                        continue;

                    string newName = hierarchyObject.name;
                    int charCount = data.headerTag[i].Length + 1;

                    string[] objectWords = hierarchyObject.name.Split(' ');

                    if (objectWords.Length <= 1)
                        return;

                    if (objectWords[0].ToLower().Equals(data.headerTag[i].ToLower(), StringComparison.Ordinal))
                    {
                        newName = newName.Substring(charCount, newName.Length - charCount);
                        ChangeHierarchyItem(selectionRect, newName, data.headerColor[i]);
                    }
                }
            }

        }

        static void ChangeHierarchyItem(Rect selectionRect, string name, Color color)
        {
            color.a = 1;
            EditorGUI.DrawRect(selectionRect, color);

            Rect nameRect = selectionRect;
            nameRect.center = new Vector2(nameRect.center.x + (nameRect.width / 2) - name.Length * 4, nameRect.center.y);

            GUIStyle style = new GUIStyle();
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            style.richText = true;
            style.normal.textColor = Color.white;

            EditorGUI.LabelField(nameRect, name, style);
        }

        static string[] GetSurroundedString(string value)
        {
            string[] results = Regex.Matches(value, @"%(.+?)%")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToArray();

            return results;
        }

        static string GetFirstSurroundedString(string value)
        {
            string[] results = Regex.Matches(value, @"%(.+?)%")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToArray();

            return results[0];
        }

        static List<Type> InternalEditorType = new List<Type>();
        static Dictionary<string, Type> dicInternalEditorType = new Dictionary<string, Type>();
        static List<Type> DisplayOnHierarchyScriptType = new List<Type>();
        static Dictionary<string, Type> dicDisplayOnHierarchyScriptType = new Dictionary<string, Type>();

        static Type SceneHierarchyWindow;
        static Type SceneHierarchy;
        static Type GameObjectTreeViewGUI;

        static FieldInfo m_SceneHierarchy;
        static FieldInfo m_TreeView;
        static PropertyInfo gui;
        static FieldInfo k_IconWidth;

        static Func<SearchableEditorWindow> lastInteractedHierarchyWindowDelegate;
        static Func<IEnumerable> GetAllSceneHierarchyWindowsDelegate;
        static Func<GameObject, Rect, bool, bool> IconSelectorShowAtPositionDelegate;
        static Action<Rect, UnityEngine.Object, int> DisplayObjectContextMenuDelegate;

        public static Action OnRepaintHierarchyWindowCallback;
        public static Action OnWindowsReorderedCallback;

        static void InternalReflection()
        {
            var arrayInteralEditorType = typeof(Editor).Assembly.GetTypes();
            InternalEditorType = arrayInteralEditorType.ToList();
            dicInternalEditorType = arrayInteralEditorType.ToDictionary(type => type.FullName);

            FieldInfo refreshHierarchy = typeof(EditorApplication).GetField(nameof(refreshHierarchy), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo OnRepaintHierarchyWindow = typeof(HierarchyEditor).GetMethod(nameof(OnRepaintHierarchyWindow), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate refreshHierarchyDelegate = Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction), OnRepaintHierarchyWindow);
            refreshHierarchy.SetValue(null, refreshHierarchyDelegate);


            FieldInfo windowsReordered = typeof(EditorApplication).GetField(nameof(windowsReordered), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo OnWindowsReordered = typeof(HierarchyEditor).GetMethod(nameof(OnWindowsReordered), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate windowsReorderedDelegate = Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction), OnWindowsReordered);
            windowsReordered.SetValue(null, windowsReorderedDelegate);

            {
                dicInternalEditorType.TryGetValue(nameof(UnityEditor) + "." + nameof(SceneHierarchyWindow), out SceneHierarchyWindow);
                dicInternalEditorType.TryGetValue(nameof(UnityEditor) + "." + nameof(GameObjectTreeViewGUI), out GameObjectTreeViewGUI); //GameObjectTreeViewGUI : TreeViewGUI
                dicInternalEditorType.TryGetValue(nameof(UnityEditor) + "." + nameof(SceneHierarchy), out SceneHierarchy);
            }

            FieldInfo s_LastInteractedHierarchy = SceneHierarchyWindow.GetField(nameof(s_LastInteractedHierarchy), BindingFlags.NonPublic | BindingFlags.Static);

            MethodInfo lastInteractedHierarchyWindow = SceneHierarchyWindow.GetProperty(nameof(lastInteractedHierarchyWindow), BindingFlags.Static | BindingFlags.Public).GetGetMethod();
            lastInteractedHierarchyWindowDelegate = Delegate.CreateDelegate(typeof(Func<SearchableEditorWindow>), lastInteractedHierarchyWindow) as Func<SearchableEditorWindow>;

            MethodInfo GetAllSceneHierarchyWindows = SceneHierarchyWindow.GetMethod(nameof(GetAllSceneHierarchyWindows), BindingFlags.Static | BindingFlags.Public);
            GetAllSceneHierarchyWindowsDelegate = Delegate.CreateDelegate(typeof(Func<IEnumerable>), GetAllSceneHierarchyWindows) as Func<IEnumerable>;

            {
                m_SceneHierarchy = SceneHierarchyWindow.GetField(nameof(m_SceneHierarchy), BindingFlags.NonPublic | BindingFlags.Instance);
                m_TreeView = SceneHierarchy.GetField(nameof(m_TreeView), BindingFlags.NonPublic | BindingFlags.Instance);
                gui = m_TreeView.FieldType.GetProperty(nameof(gui).ToLower(), BindingFlags.Public | BindingFlags.Instance);
                k_IconWidth = GameObjectTreeViewGUI.GetField(nameof(k_IconWidth), BindingFlags.Public | BindingFlags.Instance);
            }

            MethodInfo DisplayObjectContextMenu = typeof(EditorUtility).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single
            (
                method => method.Name == nameof(DisplayObjectContextMenu) && method.GetParameters()[1].ParameterType == typeof(UnityEngine.Object)
            );
            DisplayObjectContextMenuDelegate = Delegate.CreateDelegate(typeof(Action<Rect, UnityEngine.Object, int>), DisplayObjectContextMenu) as Action<Rect, UnityEngine.Object, int>;


            Type IconSelector = typeof(EditorWindow).Assembly.GetTypes().Single(type =>
                type.BaseType == typeof(EditorWindow) && type.Name == nameof(IconSelector)) as Type;
            MethodInfo ShowAtPosition = IconSelector.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single
            (
                method => method.Name == nameof(ShowAtPosition) &&
                          method.GetParameters()[0].ParameterType == typeof(UnityEngine.Object)
            );
            IconSelectorShowAtPositionDelegate = Delegate.CreateDelegate(typeof(Func<GameObject, Rect, bool, bool>), ShowAtPosition) as Func<GameObject, Rect, bool, bool>;

            GetItemAndRowIndexMethod = m_TreeView.FieldType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(method => method.Name == "GetItemAndRowIndex");

            m_TreeView_IData = m_TreeView.FieldType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Single(property => property.Name == "data");

            m_Rows = InternalEditorType.Find(type => type.Name == "TreeViewDataSource").GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Single(field => field.Name.Contains(nameof(m_Rows)));
        }

        public static IEnumerable GetAllSceneHierarchyWindows() => GetAllSceneHierarchyWindowsDelegate();

        public static void DisplayObjectContextMenu(Rect rect, UnityEngine.Object unityObject, int value) => DisplayObjectContextMenuDelegate(rect, unityObject, value);

        public static bool IconSelectorShowAtPosition(GameObject gameObject, Rect rect, bool value) => IconSelectorShowAtPositionDelegate(gameObject, rect, value);

        private static MethodInfo GetItemAndRowIndexMethod;
        private static PropertyInfo m_TreeView_IData;
        private static FieldInfo m_Rows;

        static void OnRepaintHierarchyWindow()
        {
            OnRepaintHierarchyWindowCallback?.Invoke();
        }

        static void OnWindowsReordered()
        {
            OnWindowsReorderedCallback?.Invoke();
        }

        void EditorAwake()
        {
            settings = HierarchySettings.GetAssets();
            if (settings is null) return;
            OnSettingsChanged(nameof(settings.components));
            settings.onSettingsChanged += OnSettingsChanged;

            resources = HierarchyResources.GetAssets();
            if (resources is null) return;
            resources.GenerateKeyForAssets();

            EditorApplication.hierarchyWindowItemOnGUI += HierarchyOnGUI;

            if (settings.activeHierarchy)
                Invoke();
            else
                Dispose();

            EditorApplication.update -= EditorAwake;
        }

        void ImportPackageCompleted(string packageName)
        {
        }

        void OnSettingsChanged(string param)
        {
            switch (param)
            {
                case nameof(settings.components):
                    dicComponents.Clear();
                    foreach (string componentType in settings.components)
                    {
                        if (!dicComponents.ContainsKey(componentType))
                            dicComponents.Add(componentType, componentType);
                    }

                    break;
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        public void Invoke()
        {
            EditorSceneManager.newSceneCreated += OnNewSceneCreated;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.sceneUnloaded += OnSceneUnloaded;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneDirtied += OnSceneDirtied;

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.modifierKeysChanged += OnModifierKeysChanged;

            PrefabUtility.prefabInstanceUpdated += OnPrefabUpdated;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;

            EditorApplication.update += OnEditorUpdate;

            selectionStyleAfterInvoke = false;
            EditorApplication.RepaintHierarchyWindow();
            EditorApplication.RepaintProjectWindow();
        }

        public void Dispose()
        {
            EditorSceneManager.newSceneCreated -= OnNewSceneCreated;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneLoaded -= OnSceneLoaded;
            EditorSceneManager.sceneUnloaded -= OnSceneUnloaded;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneDirtied -= OnSceneDirtied;

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.modifierKeysChanged -= OnModifierKeysChanged;

            PrefabUtility.prefabInstanceUpdated -= OnPrefabUpdated;
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;

            EditorApplication.update -= OnEditorUpdate;


            foreach (EditorWindow window in GetAllSceneHierarchyWindowsDelegate())
            {
                window.titleContent.text = "Hierarchy";
            }

            EditorApplication.RepaintHierarchyWindow();
            EditorApplication.RepaintProjectWindow();
        }

        double lastTimeSinceStartup = EditorApplication.timeSinceStartup;

        void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - lastTimeSinceStartup >= 1)
            {
                DelayCall();
                lastTimeSinceStartup = EditorApplication.timeSinceStartup;
            }
        }

        void DelayCall()
        {
            if (checkingAllHierarchy == true)
            {
                for (int i = 0; i < HierarchyWindow.windows.Count; ++i)
                {
                    if (HierarchyWindow.windows[i].editorWindow == null)
                    {
                        HierarchyWindow.windows[i].Dispose();
                        --i;
                    }
                }

                foreach (EditorWindow window in GetAllSceneHierarchyWindowsDelegate())
                {
                    if (!HierarchyWindow.instances.ContainsKey(window.GetInstanceID()))
                    {
                        var hierarchyWindow = new HierarchyWindow(window);
                        hierarchyWindow.SetWindowTitle("Quartzified Hierarchy");
                    }
                }

                checkingAllHierarchy = false;
            }

            if (hierarchyChangedRequireUpdating == true)
            {
                hierarchyChangedRequireUpdating = false;
            }
        }

        void OnModifierKeysChanged()
        {
        }

        [DidReloadScripts]
        static void OnEditorCompiled()
        {
        }

        void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (settings is null) return;
        }

        void OnSceneClosed(Scene scene)
        {
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
        }

        void OnSceneUnloaded(Scene scene)
        {
        }

        void OnSceneSaved(Scene scene)
        {
        }

        void OnSceneDirtied(Scene scene)
        {
        }

        bool hierarchyChangedRequireUpdating = false;

        void OnHierarchyChanged()
        {
            hierarchyChangedRequireUpdating = true;
        }

        void OnPrefabUpdated(GameObject prefab)
        {
        }

        bool prefabStageChanged = false;

        void OnPrefabStageOpened(PrefabStage stage)
        {
            prefabStageChanged = true;
        }

        void OnPrefabStageClosing(PrefabStage stage)
        {
            prefabStageChanged = true;
        }

        void HierarchyOnGUI(int selectionID, Rect selectionRect)
        {
            currentEvent = Event.current;

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.H && currentEvent.control)
            {
                if (!settings.activeHierarchy)
                    Invoke();
                else
                    Dispose();

                settings.activeHierarchy = !settings.activeHierarchy;
                currentEvent.Use();
            }

            if (!settings.activeHierarchy)
                return;

            if (currentEvent.control && currentEvent.keyCode == KeyCode.D)
                return;

            if (currentEvent.type == EventType.Layout)
            {
                if (prefabStageChanged)
                {
                    prefabStageChanged = false;
                }

                return;
            }

            checkingAllHierarchy = true;

            if (selectionStyleAfterInvoke == false && currentEvent.type == EventType.MouseDown)
            {
                selectionStyleAfterInvoke = true;
            }

            rowItem.Dispose();
            rowItem.ID = selectionID;
            rowItem.gameObject = EditorUtility.InstanceIDToObject(rowItem.ID) as GameObject;
            rowItem.rect = selectionRect;
            rowItem.rowIndex = GetRowIndex(selectionRect);
            rowItem.isSelected = InSelection(selectionID);
            rowItem.isFirstRow = IsFirstRow(selectionRect);
            rowItem.isFirstElement = IsFirstElement(selectionRect);

            rowItem.isNull = rowItem.gameObject == null ? true : false;

            if (!rowItem.isNull)
            {

                rowItem.isDirty = EditorUtility.IsDirty(selectionID);

                if (true && rowItem.isDirty)
                {
                    rowItem.isPrefab = PrefabUtility.IsPartOfAnyPrefab(rowItem.gameObject);

                    if (rowItem.isPrefab)
                        rowItem.isPrefabMissing = PrefabUtility.IsPrefabAssetMissing(rowItem.gameObject);
                }
            }

            rowItem.isRootObject = rowItem.isNull || rowItem.gameObject.transform.parent == null ? true : false;
            rowItem.isMouseHovering = selectionRect.Contains(currentEvent.mousePosition);

            if (rowItem.isFirstRow) //Instance always null
            {
                sceneIndex = 0;

                if (deepestRow > previousRowIndex)
                    deepestRow = previousRowIndex;
            }

            if (rowItem.isNull)
            {
                if (!IsMultiScene)
                    currentScene = SceneManager.GetActiveScene();
                else
                {
                    if (!rowItem.isFirstRow && sceneIndex < SceneManager.sceneCount - 1)
                        sceneIndex++;
                    currentScene = SceneManager.GetSceneAt(sceneIndex);
                }

                RenameSceneInHierarchy();

                if (settings.displayRowBackground)
                {
                    if (deepestRow != rowItem.rowIndex)
                        DisplayRowBackground();
                }

                previousElement = rowItem;
                previousRowIndex = rowItem.rowIndex;
                previousScene = currentScene;

                if (previousRowIndex > deepestRow)
                    deepestRow = previousRowIndex;
                return;
            }
            else
            {
                if (rowItem.isFirstElement)
                {
                    if (deepestRow > previousRowIndex)
                        deepestRow = previousRowIndex;
                    deepestRow -= rowItem.rowIndex;

                    if (IsMultiScene)
                    {
                        if (!previousElement.isNull)
                        {
                            for (int i = 0; i < SceneManager.sceneCount; ++i)
                            {
                                if (SceneManager.GetSceneAt(i) == rowItem.gameObject.scene)
                                {
                                    sceneIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (IsMultiScene)
                {
                }

                rowItem.nameRect = rowItem.rect;
                GUIStyle nameStyle = TreeStyleFromFont(FontStyle.Normal);
                rowItem.nameRect.width = nameStyle.CalcSize(new GUIContent(rowItem.gameObject.name)).x;

                rowItem.nameRect.x += 16;

                bool isPrefab = PrefabUtility.IsPartOfPrefabAsset(rowItem.gameObject);
                bool isInstance = PrefabUtility.IsPartOfPrefabInstance(rowItem.gameObject);
                bool isPrefabParent = PrefabUtility.IsAnyPrefabInstanceRoot(rowItem.gameObject);
                bool isPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null ? true : false;

                if (settings.displayRowBackground && deepestRow != rowItem.rowIndex)
                {
                    if (isPrefabMode)
                    {
                        if (rowItem.gameObject.transform.parent == null) //Should use row index instead.
                        {
                            if (deepestRow != 0)
                                DisplayRowBackground();
                        }
                    }
                    else
                        DisplayRowBackground();
                }

                if (settings.useInstantBackground)
                    CustomRowBackground();

                if (settings.displayTreeView && !rowItem.isRootObject)
                    DisplayTreeView();

                if (settings.displayCustomObjectIcon)
                    DisplayCustomObjectIcon(null);

                widthUse = WidthUse.zero;
                widthUse.left += GLOBAL_SPACE_OFFSET_LEFT;
                if (isPrefabMode) widthUse.left -= 2;   
                widthUse.afterName = rowItem.nameRect.x + rowItem.nameRect.width;

                widthUse.afterName += settings.offSetIconAfterName;

                DisplayEditableIcon();

                //DisplayNoteIcon();

                widthUse.afterName += 8;

                if(isInstance && isPrefabParent)
                    widthUse.right += 14;

                if (settings.displayTag && !rowItem.gameObject.CompareTag("Untagged"))
                {
                    if (!settings.onlyDisplayWhileMouseEnter ||
                        (settings.contentDisplay & HierarchySettings.ContentDisplay.Tag) !=
                        HierarchySettings.ContentDisplay.Tag ||
                        ((settings.contentDisplay & HierarchySettings.ContentDisplay.Tag) ==
                            HierarchySettings.ContentDisplay.Tag && rowItem.isMouseHovering))
                    {
                        DisplayTag();
                    }
                }

                if (settings.displayLayer && rowItem.gameObject.layer != 0)
                {
                    if (!settings.onlyDisplayWhileMouseEnter ||
                        (settings.contentDisplay & HierarchySettings.ContentDisplay.Layer) !=
                        HierarchySettings.ContentDisplay.Layer ||
                        ((settings.contentDisplay & HierarchySettings.ContentDisplay.Layer) ==
                            HierarchySettings.ContentDisplay.Layer && rowItem.isMouseHovering))
                    {
                        DisplayLayer();
                    }
                }

                if (settings.displayComponents)
                {
                    if (!settings.onlyDisplayWhileMouseEnter ||
                        (settings.contentDisplay & HierarchySettings.ContentDisplay.Component) !=
                        HierarchySettings.ContentDisplay.Component ||
                        ((settings.contentDisplay & HierarchySettings.ContentDisplay.Component) ==
                            HierarchySettings.ContentDisplay.Component && rowItem.isMouseHovering))
                    {
                        DisplayComponents();
                    }
                }

                ElementEvent(rowItem);

            FINISH:
                if (settings.displayGrid)
                    DisplayGrid();

                previousElement = rowItem;
                previousRowIndex = rowItem.rowIndex;
                previousScene = currentScene;

                if (previousRowIndex > deepestRow)
                {
                    deepestRow = previousRowIndex;
                }
            }
        }

        GUIStyle TreeStyleFromFont(FontStyle fontStyle)
        {
            GUIStyle style;
            switch (fontStyle)
            {
                case FontStyle.Bold:
                    style = new GUIStyle(Styles.TreeBoldLabel);
                    break;

                case FontStyle.Italic:
                    style = new GUIStyle(Styles.TreeLabel);
                    break;

                case FontStyle.BoldAndItalic:
                    style = new GUIStyle(Styles.TreeBoldLabel);
                    break;

                default:
                    style = new GUIStyle(Styles.TreeLabel);
                    break;
            }

            return style;
        }

        void CustomRowBackground()
        {
            if (currentEvent.type != EventType.Repaint)
                return;

            HierarchySettings.InstantBackgroundColor instantBackgroundColor = new HierarchySettings.InstantBackgroundColor();
            bool contain = false;
            for (int i = 0; i < settings.instantBackgroundColors.Count; ++i)
            {
                if (!settings.instantBackgroundColors[i].active) continue;
                if
                (
                    (settings.instantBackgroundColors[i].useTag && !string.IsNullOrEmpty(settings.instantBackgroundColors[i].tag) && rowItem.gameObject.CompareTag(settings.instantBackgroundColors[i].tag)) ||
                    (settings.instantBackgroundColors[i].useLayer && (1 << rowItem.gameObject.layer & settings.instantBackgroundColors[i].layer) != 0) ||
                    (settings.instantBackgroundColors[i].useStartWith && !string.IsNullOrEmpty(settings.instantBackgroundColors[i].startWith) && rowItem.name.StartsWith(settings.instantBackgroundColors[i].startWith))
                )
                {
                    contain = true;
                    instantBackgroundColor = settings.instantBackgroundColors[i];
                }
            }

            if (!contain) return;
            Color guiColor = GUI.color;
            GUI.color = instantBackgroundColor.color;
            Rect rect;
            var texture = Resources.PixelWhite;
            rect = RectFromRight(rowItem.rect, rowItem.rect.width + 16, 0);
            rect.x += 16;
            rect.xMin = 32;

            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
            GUI.color = guiColor;
        }

        void ElementEvent(RowItem element)
        {
            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.control && currentEvent.shift && currentEvent.alt &&
                    currentEvent.keyCode == KeyCode.C && lastInteractedHierarchyWindowDelegate() != null)
                    CollapseAll();
            }

            if (currentEvent.type == EventType.KeyUp &&
                currentEvent.keyCode == KeyCode.F2 &&
                Selection.gameObjects.Length > 1)
            {
                var window = SelectionsRenamePopup.ShowPopup();
                currentEvent.Use();
                return;
            }

            if (element.rect.Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseUp &&
                currentEvent.button == 2)
            {
                Undo.RegisterCompleteObjectUndo(element.gameObject,
                    element.gameObject.activeSelf ? "Inactive object" : "Active object");
                element.gameObject.SetActive(!element.gameObject.activeSelf);
                currentEvent.Use();
                return;
            }
        }

        void StaticIcon(RowItem element)
        {
            if (!element.isStatic) return;

            var rect = element.rect;
            rect = RectFromRight(rect, 3, 0);

            if (currentEvent.type == EventType.MouseUp &&
                currentEvent.button == 1 &&
                rect.Contains(currentEvent.mousePosition))
            {
                GenericMenu staticMenu = new GenericMenu();
                staticMenu.AddItem(new GUIContent("Apply All Children"), settings.applyStaticTargetAndChild,
                    () => { settings.applyStaticTargetAndChild = !settings.applyStaticTargetAndChild; });
                staticMenu.AddItem(new GUIContent("True"), element.gameObject.isStatic ? true : false,
                    () => { element.gameObject.isStatic = !element.gameObject.isStatic; });
                staticMenu.AddItem(new GUIContent("False"), !element.gameObject.isStatic ? true : false,
                    () => { element.gameObject.isStatic = !element.gameObject.isStatic; });
                staticMenu.ShowAsContext();
                currentEvent.Use();
            }
        }

        void ApplyStaticTargetAndChild(Transform target, bool value)
        {
            target.gameObject.isStatic = value;

            for (int i = 0; i < target.childCount; ++i)
                ApplyStaticTargetAndChild(target.GetChild(i), value);
        }

        void DisplayCustomObjectIcon(Texture icon)
        {
            var rect = RectFromRight(rowItem.nameRect, 16, rowItem.nameRect.width + 1);
            rect.height = 16;

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1 &&
                rect.Contains(currentEvent.mousePosition))
            {
                IconSelectorShowAtPositionDelegate(rowItem.gameObject, rect, true);
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.Repaint)
            {
                if (rect.Contains(currentEvent.mousePosition))
                {
                }

                if (icon == null)
                {
                    icon = AssetPreview.GetMiniThumbnail(rowItem.gameObject);
                    if (icon.name == "GameObject Icon" || icon.name == "d_GameObject Icon" || icon.name == "Prefab Icon" ||
                        icon.name == "d_Prefab Icon" || icon.name == "PrefabModel Icon" ||
                        icon.name == "d_PrefabModel Icon")
                        return;
                }

                Color guiColor = GUI.color;
                GUI.color = rowItem.rowIndex % 2 != 0 ? ThemeData.colorRowEven : ThemeData.colorRowOdd;
                GUI.DrawTexture(rect, Resources.PixelWhite);
                GUI.color = guiColor;
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            }
        }

        void DisplayEditableIcon()
        {
            if (rowItem.gameObject.hideFlags == HideFlags.NotEditable)
            {
                Rect lockRect = RectFromLeft(rowItem.nameRect, 12, ref widthUse.afterName);

                if (currentEvent.type == EventType.Repaint)
                {
                    GUI.color = ThemeData.colorLockIcon;
                    GUI.DrawTexture(lockRect, Resources.lockIconOn, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }

                if (currentEvent.type == EventType.MouseUp &&
                    currentEvent.button == 1 &&
                    lockRect.Contains(currentEvent.mousePosition))
                {
                    GenericMenu lockMenu = new GenericMenu();

                    GameObject gameObject = rowItem.gameObject;

                    lockMenu.AddItem(new GUIContent("Unlock"), false, () =>
                    {
                        Undo.RegisterCompleteObjectUndo(gameObject, "Unlock...");
                        foreach (Component component in gameObject.GetComponents<Component>())
                        {
                            if (component)
                            {
                                Undo.RegisterCompleteObjectUndo(component, "Unlock...");
                                component.hideFlags = HideFlags.None;
                            }
                        }

                        gameObject.hideFlags = HideFlags.None;

                        InternalEditorUtility.RepaintAllViews();
                    });
                    lockMenu.ShowAsContext();
                    currentEvent.Use();
                }
            }
        }

        void DisplayComponents()
        {
            var components = rowItem.gameObject.GetComponents(typeof(Component)).ToList<UnityEngine.Object>();
            var rendererComponent = rowItem.gameObject.GetComponent<Renderer>();
            bool hasMaterial = rendererComponent != null && rendererComponent.sharedMaterial != null;

            if (hasMaterial)
            {
                for (int i = 0; i < rendererComponent.sharedMaterials.Length; ++i)
                {
                    Material sharedMat = rendererComponent.sharedMaterials[i];
                    components.Add(sharedMat);
                }
            }

            int length = components.Count;
            float widthUsedCached = 0;
            widthUse.right -= 16;
            if (settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName)
            {
                widthUsedCached = widthUse.afterName;
                widthUse.afterName += 4;
            }
            else
            {
                widthUsedCached = widthUse.right;
                widthUse.right += 2;
            }

            for (int i = 0; i < length; ++i)
            {
                var component = components[i];

                try
                {
                    Type comType = component.GetType();

                    if (comType != null)
                    {
                        bool isMono = false;
                        if (comType.BaseType == typeof(MonoBehaviour)) isMono = true;
                        if (isMono)
                        {
                            //TODO: ???
                            bool shouldIgnoreThisMono = false;
                            if (shouldIgnoreThisMono) continue;
                        }

                        switch (settings.componentDisplayMode)
                        {
                            case HierarchySettings.ComponentDisplayMode.ScriptOnly:
                                if (!isMono)
                                    continue;
                                break;

                            case HierarchySettings.ComponentDisplayMode.Specified:
                                if (!dicComponents.ContainsKey(comType.Name))
                                    continue;
                                break;

                            case HierarchySettings.ComponentDisplayMode.Ignore:
                                if (dicComponents.ContainsKey(comType.Name))
                                    continue;
                                break;
                        }

                        Rect rect = Rect.zero;

                        if (settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName)
                            rect = RectFromLeft(rowItem.nameRect, settings.componentSize, ref widthUse.afterName);
                        else
                            rect = RectFromRight(rowItem.rect, settings.componentSize, ref widthUse.right);


                        if (hasMaterial && i == length - rendererComponent.sharedMaterials.Length &&
                            settings.componentDisplayMode != HierarchySettings.ComponentDisplayMode.ScriptOnly)
                        {
                            for (int m = 0; m < rendererComponent.sharedMaterials.Length; ++m)
                            {
                                var sharedMaterial = rendererComponent.sharedMaterials[m];

                                if (sharedMaterial == null) continue;
                                ComponentIcon(sharedMaterial, comType, rect, true);

                                if (settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName)
                                    rect = RectFromLeft(rowItem.nameRect, settings.componentSize,
                                        ref widthUse.afterName);
                                else
                                    rect = RectFromRight(rowItem.rect, settings.componentSize, ref widthUse.right);
                            }

                            break;
                        }

                        ComponentIcon(component, comType, rect);

                        if (settings.componentAlignment == HierarchySettings.ElementAlignment.AfterName)
                            widthUse.afterName += settings.componentSpacing;
                        else
                            widthUse.right += settings.componentSpacing;
                    }
                }
                catch (System.Exception)
                {
                    continue;
                }
            }
        }

        void ComponentIcon(UnityEngine.Object component, Type componentType, Rect rect, bool isMaterial = false)
        {
            int comHash = component.GetHashCode();

            if (currentEvent.type == EventType.Repaint)
            {
                Texture image = EditorGUIUtility.ObjectContent(component, componentType).image;

                if (selectedComponents.ContainsKey(comHash))
                {
                    Color guiColor = GUI.color;
                    GUI.color = ThemeData.comSelBGColor;
                    GUI.DrawTexture(rect, Resources.PixelWhite, ScaleMode.StretchToFill);
                    GUI.color = guiColor;
                }

                string tooltip = isMaterial ? component.name : componentType.Name;
                tooltipContent.tooltip = tooltip;
                GUI.Box(rect, tooltipContent, GUIStyle.none);

                GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit);
            }


            if (rect.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (currentEvent.button == 0)
                    {
                        if (currentEvent.control)
                        {
                            if (!selectedComponents.ContainsKey(comHash))
                            {
                                selectedComponents.Add(comHash, component);
                                activeComponent = component;
                            }
                            else
                            {
                                selectedComponents.Remove(comHash);
                            }

                            currentEvent.Use();
                            return;
                        }

                        selectedComponents.Clear();
                        selectedComponents.Add(comHash, component);
                        activeComponent = component;
                        currentEvent.Use();
                        return;
                    }

                    if (currentEvent.button == 1)
                    {
                        if (currentEvent.control)
                        {
                            GenericMenu componentGenericMenu = new GenericMenu();

                            componentGenericMenu.AddItem(new GUIContent("Remove All Component"), false, () =>
                            {
                                if (!selectedComponents.ContainsKey(comHash))
                                    selectedComponents.Add(comHash, component);

                                foreach (var selectedComponent in selectedComponents.ToList())
                                {
                                    if (selectedComponent.Value is Material)
                                        continue;

                                    selectedComponents.Remove(selectedComponent.Key);
                                    Undo.DestroyObjectImmediate(selectedComponent.Value);
                                }

                                selectedComponents.Clear();
                            });
                            componentGenericMenu.ShowAsContext();
                        }
                        else
                        {
                            DisplayObjectContextMenuDelegate(rect, component, 0);
                        }

                        currentEvent.Use();
                        return;
                    }
                }

                if (currentEvent.type == EventType.MouseUp)
                {
                    if (currentEvent.button == 0)
                    {
                        List<UnityEngine.Object> inspectorComponents = new List<UnityEngine.Object>();

                        foreach (var selectedComponent in selectedComponents)
                            inspectorComponents.Add(selectedComponent.Value);

                        if (!selectedComponents.ContainsKey(comHash))
                            inspectorComponents.Add(component);

                        var window = QuickInspect.OpenEditor();
                        window.Fill(inspectorComponents,
                            currentEvent.alt ? QuickInspect.FillMode.Add : QuickInspect.FillMode.Default);
                        window.Focus();

                        currentEvent.Use();
                        return;
                    }
                }
            }

            if (selectedComponents.Count > 0 &&
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                !currentEvent.control &&
                !rect.Contains(currentEvent.mousePosition))
            {
                selectedComponents.Clear();
                activeComponent = null;
            }
        }

        void DisplayTag()
        {
            GUIContent tagContent = new GUIContent(rowItem.gameObject.tag);

            var style = Styles.Tag;
            style.normal.textColor = ThemeData.tagColor;
            Rect rect;

            if (settings.tagAlignment == HierarchySettings.ElementAlignment.AfterName)
            {
                rect = RectFromLeft(rowItem.nameRect, style.CalcSize(tagContent).x, ref widthUse.afterName);

                if (currentEvent.type == EventType.Repaint)
                {
                    GUI.Label(rect, tagContent, style);
                }
            }
            else
            {
                rect = RectFromRight(rowItem.rect, style.CalcSize(tagContent).x, ref widthUse.right);

                if (currentEvent.type == EventType.Repaint)
                {
                    GUI.Label(rect, tagContent, style);
                }
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1 &&
                rect.Contains(currentEvent.mousePosition))
            {
                GenericMenu menuTags = new GenericMenu();
                GameObject gameObject = rowItem.gameObject;

                menuTags.AddItem(new GUIContent("Apply All Children"), settings.applyTagTargetAndChild,
                    () => { settings.applyTagTargetAndChild = !settings.applyTagTargetAndChild; });

                foreach (var tag in InternalEditorUtility.tags)
                {
                    menuTags.AddItem(new GUIContent(tag), gameObject.tag == tag ? true : false, () =>
                    {
                        if (settings.applyTagTargetAndChild)
                            ApplyTagTargetAndChild(gameObject.transform, tag);
                        else
                        {
                            Undo.RegisterCompleteObjectUndo(gameObject, "Change Tag");
                            gameObject.tag = tag;
                        }
                    });
                }

                menuTags.ShowAsContext();
                currentEvent.Use();
            }
        }

        void ApplyTagTargetAndChild(Transform target, string tag)
        {
            Undo.RegisterCompleteObjectUndo(target.gameObject, "Change Tag");
            target.gameObject.tag = tag;

            for (int i = 0; i < target.childCount; ++i)
                ApplyTagTargetAndChild(target.GetChild(i), tag);
        }

        void DisplayLayer()
        {
            GUIContent layerContent = new GUIContent(LayerMask.LayerToName(rowItem.gameObject.layer));
            var style = Styles.Layer;
            style.normal.textColor = ThemeData.layerColor;
            Rect rect;

            if (settings.layerAlignment == HierarchySettings.ElementAlignment.AfterName)
            {
                rect = RectFromLeft(rowItem.nameRect, style.CalcSize(layerContent).x, ref widthUse.afterName);

                if (currentEvent.type == EventType.Repaint)
                {
                    GUI.Label(rect, layerContent, style);
                }
            }
            else
            {
                rect = RectFromRight(rowItem.rect, style.CalcSize(layerContent).x, ref widthUse.right);

                if (currentEvent.type == EventType.Repaint)
                {
                    GUI.Label(rect, layerContent, style);
                }
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 1 &&
                rect.Contains(currentEvent.mousePosition))
            {
                GenericMenu menuLayers = new GenericMenu();
                GameObject gameObject = rowItem.gameObject;

                menuLayers.AddItem(new GUIContent("Apply All Children"), settings.applyLayerTargetAndChild,
                    () => { settings.applyLayerTargetAndChild = !settings.applyLayerTargetAndChild; });

                foreach (string layer in InternalEditorUtility.layers)
                {
                    menuLayers.AddItem(new GUIContent(layer),
                        LayerMask.NameToLayer(layer) == gameObject.layer ? true : false, () =>
                        {
                            if (settings.applyLayerTargetAndChild)
                                ApplyLayerTargetAndChild(gameObject.transform, LayerMask.NameToLayer(layer));
                            else
                            {
                                Undo.RegisterCompleteObjectUndo(gameObject, "Change Layer");
                                gameObject.layer = LayerMask.NameToLayer(layer);
                            }
                        });
                }

                menuLayers.ShowAsContext();
                currentEvent.Use();
            }
        }

        void ApplyLayerTargetAndChild(Transform target, int layer)
        {
            Undo.RegisterCompleteObjectUndo(target.gameObject, "Change Layer");
            target.gameObject.layer = layer;

            for (int i = 0; i < target.childCount; ++i)
                ApplyLayerTargetAndChild(target.GetChild(i), layer);
        }

        void DisplayRowBackground(bool nextRow = true)
        {
            if (currentEvent.type != EventType.Repaint)
                return;

            Rect rect = rowItem.rect;
            rect.xMin = -1;
            rect.width += 16;

            Color color = (rect.y / rect.height) % 2 == 0 ? ThemeData.colorRowEven : ThemeData.colorRowOdd;

            if (nextRow)
                rect.y += rect.height;

            Color guiColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Resources.PixelWhite, ScaleMode.StretchToFill);
            GUI.color = guiColor;
        }

        void DisplayGrid()
        {
            if (currentEvent.type != EventType.Repaint)
                return;

            var rect = rowItem.rect;

            rect.xMin = GLOBAL_SPACE_OFFSET_LEFT;
            rect.y += 15;
            rect.width += 16;
            rect.height = 1;

            Color guiColor = GUI.color;
            GUI.color = ThemeData.colorGrid;
            GUI.DrawTexture(rect, Resources.PixelWhite, ScaleMode.StretchToFill);
            GUI.color = guiColor;
        }

        void DisplayTreeView()
        {
            if (currentEvent.type != EventType.Repaint)
                return;

            Rect rect = rowItem.rect;

            rect.width = 40;
            rect.x -= 34;
            var t = rowItem.gameObject.transform.parent;

            Color guiColor = GUI.color;
            GUI.color = ThemeData.colorTreeView;

            if (t.childCount == 1 || t.GetChild(t.childCount - 1) == rowItem.gameObject.transform)
            {
                GUI.DrawTexture(rect, resources.GetIcon("icon_branch_L"), ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.DrawTexture(rect, resources.GetIcon("icon_branch_T"), ScaleMode.ScaleToFit);
            }

            while (t != null)
            {
                if (t.parent == null)
                    break;

                if (t == t.parent.GetChild(t.parent.childCount - 1))
                {
                    t = t.parent;
                    rect.x -= 14;
                    continue;
                }

                rect.x -= 14;
                GUI.DrawTexture(rect, resources.GetIcon("icon_branch_I"), ScaleMode.ScaleToFit);
                t = t.parent;
            }

            GUI.color = guiColor;
        }

        GUIContent tmpSceneContent = new GUIContent();

        void RenameSceneInHierarchy()
        {
            string name = currentScene.name;
            if (name == "")
                return;

            var leftTitleWidthUsed = 48f;
#if UNITY_2019_1_OR_NEWER
            leftTitleWidthUsed += 24f;
#endif

            if (!currentScene.isLoaded)
                name = string.Format("{0} (not loaded", name);

            tmpSceneContent.text = name == "" ? "Untitled" : name;
            Vector2 size = Styles.TreeBoldLabel.CalcSize(tmpSceneContent);
            leftTitleWidthUsed += size.x;


            if (currentEvent.type == EventType.KeyDown &&
                currentEvent.keyCode == KeyCode.F2 &&
                rowItem.rect.Contains(currentEvent.mousePosition))
            {
                SceneRenamePopup.ShowPopup(currentScene);
            }
        }

        void CollapseAll()
        {
        }

        void DirtyScene(Scene scene)
        {
            if (EditorApplication.isPlaying)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
        }

        bool IsFirstElement(Rect rect) => previousRowIndex > rect.y / rect.height;

        bool IsFirstRow(Rect rect) => rect.y / rect.height == 0;

        int GetRowIndex(Rect rect) => (int)(rect.y / rect.height);

        bool InSelection(int ID) => Selection.Contains(ID) ? true : false;

        bool IsElementDirty(int ID) => EditorUtility.IsDirty(ID);

        Rect RectFromRight(Rect rect, float width, float usedWidth)
        {
            usedWidth += width;
            rect.x = rect.x + rect.width - usedWidth;
            rect.width = width;
            return rect;
        }

        Rect RectFromRight(Rect rect, float width, ref float usedWidth)
        {
            usedWidth += width;
            rect.x = rect.x + rect.width - usedWidth;
            rect.width = width;
            return rect;
        }

        Rect RectFromRight(Rect rect, Vector2 offset, float width, ref float usedWidth)
        {
            usedWidth += width;
            rect.position += offset;
            rect.x = rect.x + rect.width - usedWidth;
            rect.width = width;
            return rect;
        }

        Rect RectFromLeft(Rect rect, float width, float usedWidth, bool usexmin = true)
        {
            if (usexmin)
                rect.xMin = 0;
            rect.x += usedWidth;
            rect.width = width;
            usedWidth += width;
            return rect;
        }

        Rect RectFromLeft(Rect rect, float width, ref float usedWidth, bool usexmin = true)
        {
            if (usexmin)
                rect.xMin = 0;
            rect.x += usedWidth;
            rect.width = width;
            usedWidth += width;
            return rect;
        }

        Rect RectFromLeft(Rect rect, Vector2 offset, float width, ref float usedWidth, bool usexmin = true)
        {
            if (usexmin)
                rect.xMin = 0;
            rect.position += offset;
            rect.x += usedWidth;
            rect.width = width;
            usedWidth += width;
            return rect;
        }

        struct WidthUse
        {
            public float left;
            public float right;
            public float afterName;

            public WidthUse(float left, float right, float afterName)
            {
                this.left = left;
                this.right = right;
                this.afterName = afterName;
            }

            public static WidthUse zero
            {
                get { return new WidthUse(0, 0, 0); }
            }
        }

    }
}