using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Quartzified.Tools.Hierarchy
{
    public static class HierarchySettingsProvider
    {
        static HierarchySettings instance => HierarchySettings.instance;

        static float TITLE_MARGIN_TOP = 14;
        static float TITLE_MARGIN_BOTTOM = 8;
        static float CONTENT_MARGIN_LEFT = 10;


        [SettingsProvider]
        static SettingsProvider SettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Quartzified/Hierarchy", SettingsScope.User)
            {
                label = "Hierarchy",

                activateHandler = (searchContext, rootElement) =>
                {
                    HierarchySettings settings = HierarchySettings.GetAssets();

                    HorizontalLayout horizontalLayout = new HorizontalLayout();
                    horizontalLayout.style.backgroundColor = new Color(0, 0, 0, 0.2f);
                    horizontalLayout.style.paddingTop = 4;
                    horizontalLayout.style.paddingBottom = 10;
                    rootElement.Add(horizontalLayout);

                    Label hierarchyTitle = new Label("Hierarchy");
                    hierarchyTitle.StyleFontSize(20);
                    hierarchyTitle.StyleMargin(10, 0, 2, 2);
                    hierarchyTitle.StyleFont(FontStyle.Bold);
                    horizontalLayout.Add(hierarchyTitle);

                    Label importButton = new Label();
                    importButton.StyleFontSize(14);
                    importButton.StyleMargin(0, 0, 6, 0);
                    importButton.text = "  Import";
                    importButton.style.unityFontStyleAndWeight = FontStyle.Italic;
                    Color importExportButtonColor = new Color32(102, 157, 246, 255);
                    importButton.style.color = importExportButtonColor;
                    importButton.RegisterCallback<PointerUpEvent>(evt => HierarchySettings.instance.ImportFromJson());
                    horizontalLayout.Add(importButton);

                    Label exportButton = new Label();
                    exportButton.StyleFontSize(14);
                    exportButton.StyleMargin(0, 0, 6, 0);
                    exportButton.text = "| Export";
                    exportButton.style.unityFontStyleAndWeight = FontStyle.Italic;
                    exportButton.style.color = importExportButtonColor;
                    exportButton.RegisterCallback<PointerUpEvent>(evt => HierarchySettings.instance.ExportToJson());
                    horizontalLayout.Add(exportButton);

                    ScrollView scrollView = new ScrollView();
                    rootElement.Add(scrollView);

                    VerticalLayout verticalLayout = new VerticalLayout();
                    verticalLayout.StylePadding(8, 8, 8, 8);
                    scrollView.Add(verticalLayout);

                    var Object = new Label("Object");
                    Object.StyleFont(FontStyle.Bold);
                    Object.StyleMargin(0, 0, 0, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(Object);

                    var displayCustomObjectIcon = new Toggle("Display Custom Icon");
                    displayCustomObjectIcon.value = settings.displayCustomObjectIcon;
                    displayCustomObjectIcon.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.displayCustomObjectIcon = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.displayCustomObjectIcon));
                    });
                    displayCustomObjectIcon.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(displayCustomObjectIcon);

                    var View = new Label("View");
                    View.StyleFont(FontStyle.Bold);
                    View.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(View);

                    var displayRowBackground = new Toggle("Display RowBackground");
                    displayRowBackground.value = settings.displayRowBackground;
                    displayRowBackground.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.displayRowBackground = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.displayRowBackground));
                    });
                    displayRowBackground.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(displayRowBackground);

                    var displayTreeView = new Toggle("Display TreeView");
                    displayTreeView.value = settings.displayTreeView;
                    displayTreeView.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.displayTreeView = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.displayTreeView));
                    });
                    displayTreeView.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(displayTreeView);

                    var displayGrid = new Toggle("Display Grid");
                    displayGrid.value = settings.displayGrid;
                    displayGrid.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.displayGrid = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.displayGrid));
                    });
                    displayGrid.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(displayGrid);

                    var Components = new Label("Components");
                    Components.StyleFont(FontStyle.Bold);
                    Components.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(Components);

                    var displayComponents = new Toggle("Display Components Icon");
                    displayComponents.value = settings.displayComponents;
                    displayComponents.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.displayComponents = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.displayComponents));
                    });
                    displayComponents.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(displayComponents);

                    var componentAlignment = new EnumField(settings.componentAlignment);
                    componentAlignment.label = "Component Alignment";
                    componentAlignment.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.componentAlignment = (HierarchySettings.ElementAlignment)evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.componentAlignment));
                    });
                    componentAlignment.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(componentAlignment);

                    var componentDisplayMode = new EnumField(settings.componentDisplayMode);
                    componentDisplayMode.label = "Component Display Mode";
                    componentDisplayMode.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(componentDisplayMode);

                    var componentListInput = new TextField("Components");
                    componentListInput.value = string.Join(" ", settings.components);
                    componentListInput.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(componentListInput);
                    componentListInput.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.components = evt.newValue.Split(' ');
                        settings.OnSettingsChanged(nameof(settings.components));
                    });
                    componentDisplayMode.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.componentDisplayMode = (HierarchySettings.ComponentDisplayMode)evt.newValue;
                        switch (settings.componentDisplayMode)
                        {
                            case HierarchySettings.ComponentDisplayMode.Specified:
                                componentListInput.StyleDisplay(true);
                                break;

                            case HierarchySettings.ComponentDisplayMode.Ignore:
                                componentListInput.StyleDisplay(true);
                                break;

                            case HierarchySettings.ComponentDisplayMode.All:
                                componentListInput.StyleDisplay(false);
                                break;

                            case HierarchySettings.ComponentDisplayMode.ScriptOnly:
                                componentListInput.StyleDisplay(false);
                                break;
                        }

                        settings.OnSettingsChanged(nameof(settings.componentDisplayMode));
                    });

                    var componentSizeEnum = HierarchySettings.ComponentSize.Normal;
                    switch (settings.componentSize)
                    {
                        case 12:
                            componentSizeEnum = HierarchySettings.ComponentSize.Small;
                            break;

                        case 14:
                            componentSizeEnum = HierarchySettings.ComponentSize.Normal;
                            break;

                        case 16:
                            componentSizeEnum = HierarchySettings.ComponentSize.Large;
                            break;
                    }

                    var componentSize = new EnumField(componentSizeEnum);
                    componentSize.label = "Component Size";
                    componentSize.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    componentSize.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        switch (evt.newValue)
                        {
                            case HierarchySettings.ComponentSize.Small:
                                settings.componentSize = 12;
                                break;

                            case HierarchySettings.ComponentSize.Normal:
                                settings.componentSize = 14;
                                break;

                            case HierarchySettings.ComponentSize.Large:
                                settings.componentSize = 16;
                                break;
                        }

                        settings.OnSettingsChanged(nameof(settings.componentSize));
                    });
                    verticalLayout.Add(componentSize);

                    var componentSpacing = new IntegerField();
                    componentSpacing.label = "Component Spacing";
                    componentSpacing.value = settings.componentSpacing;
                    componentSpacing.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    componentSpacing.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.componentSpacing = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.componentSpacing));
                    });
                    verticalLayout.Add(componentSpacing);

                    var tag = new Label("Tag");
                    tag.StyleFont(FontStyle.Bold);
                    tag.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(tag);

                    var displayTag = new Toggle("Display Tag");
                    displayTag.value = settings.displayTag;
                    displayTag.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.displayTag = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.displayTag));
                    });
                    displayTag.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(displayTag);

                    var applyTagTargetAndChild = new Toggle("Tag Recursive Change");
                    applyTagTargetAndChild.value = settings.applyTagTargetAndChild;
                    applyTagTargetAndChild.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.applyTagTargetAndChild = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.applyTagTargetAndChild));
                    });
                    applyTagTargetAndChild.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(applyTagTargetAndChild);

                    var tagAlignment = new EnumField(settings.tagAlignment);
                    tagAlignment.label = "Tag Alignment";
                    tagAlignment.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.tagAlignment = (HierarchySettings.ElementAlignment)evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.tagAlignment));
                    });
                    tagAlignment.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(tagAlignment);

                    var layer = new Label("Layer");
                    layer.StyleFont(FontStyle.Bold);
                    layer.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(layer);

                    var displayLayer = new Toggle("Display Layer");
                    displayLayer.value = settings.displayLayer;
                    displayLayer.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.displayLayer = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.displayLayer));
                    });
                    displayLayer.style.marginTop = 8;
                    displayLayer.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(displayLayer);

                    var applyLayerTargetAndChild = new Toggle("Layer Recursive Change");
                    applyLayerTargetAndChild.value = settings.applyLayerTargetAndChild;
                    applyLayerTargetAndChild.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.applyLayerTargetAndChild = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.applyLayerTargetAndChild));
                    });
                    applyLayerTargetAndChild.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(applyLayerTargetAndChild);

                    var layerAlignment = new EnumField(settings.layerAlignment);
                    layerAlignment.label = "Layer Alignment";
                    layerAlignment.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.layerAlignment = (HierarchySettings.ElementAlignment)evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.layerAlignment));
                    });
                    layerAlignment.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(layerAlignment);

                    var advanced = new Label("Advanced");
                    advanced.StyleFont(FontStyle.Bold);
                    advanced.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(advanced);

                    var onlyDisplayWhileMouseHovering = new Toggle("Display Hovering");
                    onlyDisplayWhileMouseHovering.tooltip = "Only display while mouse hovering";
                    onlyDisplayWhileMouseHovering.StyleMarginTop(7);
                    onlyDisplayWhileMouseHovering.value = settings.onlyDisplayWhileMouseEnter;
                    onlyDisplayWhileMouseHovering.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.onlyDisplayWhileMouseEnter = evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.onlyDisplayWhileMouseEnter));
                    });
                    onlyDisplayWhileMouseHovering.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                    verticalLayout.Add(onlyDisplayWhileMouseHovering);

                    var contentMaskEnumFlags = new EnumFlagsField(settings.contentDisplay);
                    contentMaskEnumFlags.StyleDisplay(onlyDisplayWhileMouseHovering.value);
                    contentMaskEnumFlags.label = "Content Mask";
                    onlyDisplayWhileMouseHovering.RegisterValueChangedCallback((evt) => { contentMaskEnumFlags.StyleDisplay(evt.newValue); });
                    contentMaskEnumFlags.RegisterValueChangedCallback((evt) =>
                    {
                        Undo.RecordObject(settings, "Change Settings");

                        settings.contentDisplay = (HierarchySettings.ContentDisplay)evt.newValue;
                        settings.OnSettingsChanged(nameof(settings.contentDisplay));
                    });
                    contentMaskEnumFlags.style.marginLeft = CONTENT_MARGIN_LEFT;
                    verticalLayout.Add(contentMaskEnumFlags);

                    var Theme = new Label("Theme");
                    Theme.StyleFont(FontStyle.Bold);
                    Theme.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(Theme);

                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorHelpBox themeWarningPlaymode =
                            new EditorHelpBox("This setting only available on edit mode.", MessageType.Info);
                        verticalLayout.Add(themeWarningPlaymode);
                    }
                    else
                    {
                        EditorHelpBox selectionColorHelpBox = new EditorHelpBox(
                            "Theme selection color require editor assembly recompile to take affect.\nBy selecting any script, right click -> Reimport. it will force the editor to recompile.",
                            MessageType.Info);
                        selectionColorHelpBox.StyleDisplay(false);
                        verticalLayout.Add(selectionColorHelpBox);

                        ColorField colorRowEven = new ColorField("Row Even");
                        colorRowEven.value = settings.usedThemeData.colorRowEven;
                        colorRowEven.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        colorRowEven.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.colorRowEven = evt.newValue;
                            else
                                settings.personalTheme.colorRowEven = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(colorRowEven);

                        ColorField colorRowOdd = new ColorField("Row Odd");
                        colorRowOdd.value = settings.usedThemeData.colorRowOdd;
                        colorRowOdd.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        colorRowOdd.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.colorRowOdd = evt.newValue;
                            else
                                settings.personalTheme.colorRowOdd = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(colorRowOdd);

                        ColorField colorGrid = new ColorField("Grid Color");
                        colorGrid.value = settings.usedThemeData.colorGrid;
                        colorGrid.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        colorGrid.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.colorGrid = evt.newValue;
                            else
                                settings.personalTheme.colorGrid = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(colorGrid);

                        ColorField colorTreeView = new ColorField("TreeView");
                        colorTreeView.value = settings.usedThemeData.colorTreeView;
                        colorTreeView.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        colorTreeView.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.colorTreeView = evt.newValue;
                            else
                                settings.personalTheme.colorTreeView = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(colorTreeView);

                        ColorField colorLockIcon = new ColorField("Lock Icon");
                        colorLockIcon.value = settings.usedThemeData.colorLockIcon;
                        colorLockIcon.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        colorLockIcon.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.colorLockIcon = evt.newValue;
                            else
                                settings.personalTheme.colorLockIcon = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(colorLockIcon);

                        ColorField tagColor = new ColorField("Tag Text");
                        tagColor.value = settings.usedThemeData.tagColor;
                        tagColor.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        tagColor.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.tagColor = evt.newValue;
                            else
                                settings.personalTheme.tagColor = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(tagColor);

                        ColorField layerColor = new ColorField("Layer Text");
                        layerColor.value = settings.usedThemeData.layerColor;
                        layerColor.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        layerColor.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.layerColor = evt.newValue;
                            else
                                settings.personalTheme.layerColor = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(layerColor);

                        ColorField comSelBGColor = new ColorField("Component Selection");
                        comSelBGColor.value = settings.usedThemeData.comSelBGColor;
                        comSelBGColor.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        comSelBGColor.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            if (EditorGUIUtility.isProSkin)
                                settings.professionalTheme.comSelBGColor = evt.newValue;
                            else
                                settings.personalTheme.comSelBGColor = evt.newValue;

                            settings.OnSettingsChanged();
                        });
                        verticalLayout.Add(comSelBGColor);
                    }

                    var headerTags = new Label("Header Tags");
                    headerTags.StyleFont(FontStyle.Bold);
                    headerTags.StyleMargin(0, 0, TITLE_MARGIN_TOP, TITLE_MARGIN_BOTTOM);
                    verticalLayout.Add(headerTags);

                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorHelpBox themeWarningPlaymode =
                            new EditorHelpBox("This setting only available on edit mode.", MessageType.Info);
                        verticalLayout.Add(themeWarningPlaymode);
                    }
                    else
                    {
                        var headerCount = new IntegerField();
                        headerCount.label = "Header Tag Count";
                        headerCount.value = settings.tagData.headerCount;
                        headerCount.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                        headerCount.RegisterValueChangedCallback((evt) =>
                        {
                            Undo.RecordObject(settings, "Change Settings");

                            int value = evt.newValue;

                            if (evt.newValue > 32)
                            {
                                value = 32;
                                headerCount.value = value;
                            }

                            settings.tagData.headerCount = value;

                            settings.OnSettingsChanged();

                            UpdateHeaderTags(settings, value, verticalLayout);

                            Debug.Log("Value Changed");
                        });
                        verticalLayout.Add(headerCount);

                        UpdateHeaderTags(settings, headerCount.value, verticalLayout);
                    }
                    Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                    Undo.undoRedoPerformed += OnUndoRedoPerformed;
                },

                deactivateHandler = () => Undo.undoRedoPerformed -= OnUndoRedoPerformed,

                keywords = new HashSet<string>(new[] { "Hierarchy" })
            };

            provider.Repaint();

            return provider;
        }

        private static void OnUndoRedoPerformed()
        {
            SettingsService.NotifySettingsProviderChanged();

            if (instance != null)
            {
                instance.onSettingsChanged?.Invoke(nameof(instance.components)); // Refresh components on undo & redo
            }
        }

        static void UpdateHeaderTags(HierarchySettings settings, int count, VerticalLayout layout)
        {
            int difference = count - settings.tagData.headerTag.Count;
            if (difference > 0)
            {
                ExpandList(settings.tagData.headerTag, count);
                ExpandList(settings.tagData.headerColor, count);
            }
            else if (difference < 0)
            {
                ReduceList(settings.tagData.headerTag, count);
                ReduceList(settings.tagData.headerColor, count);
            }

            for (int i = 0; i < settings.tagData.headerTag.Count; i++)
            {
                int index = i;

                HorizontalLayout horizontalLayout = new HorizontalLayout();

                TextField tagName = new TextField("Tag Name: " + index);
                tagName.value = settings.tagData.headerTag[i];
                tagName.StyleMarginLeft(CONTENT_MARGIN_LEFT);
                tagName.StyleWidth(250);
                tagName.RegisterValueChangedCallback((evt) =>
                {
                    Undo.RecordObject(settings, "Change Settings");

                    settings.tagData.headerTag[index] = evt.newValue;

                    settings.OnSettingsChanged();
                }); 
                horizontalLayout.Add(tagName);

                ColorField tagColor = new ColorField("Tag Color: " + index);
                tagColor.value = settings.tagData.headerColor[index];
                tagColor.RegisterValueChangedCallback((evt) =>
                {
                    Undo.RecordObject(settings, "Change Settings");

                    settings.tagData.headerColor[index] = evt.newValue;

                    settings.OnSettingsChanged();
                });
                horizontalLayout.Add(tagColor);


                layout.Add(horizontalLayout);
            }
        }

        public static void ExpandList<T>(List<T> originalList, int newSize)
        {
            int countToAdd = newSize - originalList.Count;
            if (countToAdd > 0)
            {
                T defaultElement = default(T); // Default value for the list's element type
                originalList.AddRange(Enumerable.Repeat(defaultElement, countToAdd));
            }
        }

        public static void ReduceList<T>(List<T> originalList, int newSize)
        {
            if (originalList.Count > newSize)
            {
                originalList.RemoveRange(newSize, originalList.Count - newSize);
            }
        }

        static T[] ExpandArray<T>(T[] originalArray, int newSize)
        {
            T[] newArray = new T[newSize];
            Array.Copy(originalArray, newArray, Mathf.Min(originalArray.Length, newSize));
            return newArray;
        }

        static T[] ReduceArray<T>(T[] originalArray, int newSize)
        {
            T[] newArray = new T[newSize];
            Array.Copy(originalArray, newArray, Mathf.Min(originalArray.Length, newSize));
            return newArray;
        }
    }

}

