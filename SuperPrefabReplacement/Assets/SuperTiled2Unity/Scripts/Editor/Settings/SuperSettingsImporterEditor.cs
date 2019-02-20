﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace SuperTiled2Unity.Editor
{
    [CustomEditor(typeof(SuperSettingsImporter))]
    public class SuperSettingsImporterEditor : SuperImporterEditor<SuperSettingsImporter>
    {
        public override bool showImportedObject { get { return false; } }

        protected override string EditorLabel { get { return "SuperTiled2Unity Project Settings"; } }

        protected override string EditorDefinition { get { return "Project Settings related to SuperTiled2Unity importing of textures, tilesets, and maps."; } }

        // Serialized properties
        private SerializedProperty m_PixelsPerUnit;
        private readonly GUIContent m_PixelsPerUnitContent = new GUIContent("Default Pixels Per Unit", "How many pixels in the sprite correspond to one unit in the world. (Default Setting)");

        private SerializedProperty m_EdgesPerEllipse;
        private readonly GUIContent m_EdgesPerEllipseContent = new GUIContent("Default Edges Per Ellipse", "How many edges to use when appromixating ellipse/circle colliders. (Default Setting)");

        private SerializedProperty m_AnimationFramerate;
        private readonly GUIContent m_AnimationFramerateContent = new GUIContent("Animation Framerate", "How many frames per second for tile animations.");

        private SerializedProperty m_DefaultMaterial;
        private readonly GUIContent m_DefaultMaterialContent = new GUIContent("Default Material", "Set to the material you want to use for sprites and tiles imported by SuperTiled2Unity. Leave empy to use built-in sprite material.");

        private SerializedProperty m_ObjectTypesXml;
        private readonly GUIContent m_ObjectTypesXmlContent = new GUIContent("Object Types Xml", "Set to an Object Types Xml file exported from Tiled Object Type Editor.");

        private ST2USettings m_ST2USettings;
        private bool m_ShowObjectTypes;

        public override bool HasPreviewGUI()
        {
            return false;
        }

        protected override void OnHeaderGUI()
        {
            if (assetTarget != null)
            {
                base.OnHeaderGUI();

                CacheSerializedProperites();
            }
        }

        protected override void InternalOnInspectorGUI()
        {
            m_ST2USettings = GetAssetTarget<ST2USettings>();

            if (m_ST2USettings != null)
            {
                DoGuiHeader();
                AddSeperator();

                DoGuiSettings();
                AddSeperator();

                DoObjectTypesDisplay();
                AddSeperator();

                DoGuiLayerColors();
                ApplyRevertGUI();

                AddSeperator();
                DoGuiReimportAssets();
            }
        }

        protected override void Apply()
        {
            // Set any limits on properties
            m_PixelsPerUnit.floatValue = Clamper.ClampPixelsPerUnit(m_PixelsPerUnit.floatValue);
            m_EdgesPerEllipse.intValue = Clamper.ClampEdgesPerEllipse(m_EdgesPerEllipse.intValue);
            m_AnimationFramerate.intValue = Clamper.ClampAnimationFramerate(m_AnimationFramerate.intValue);
            base.Apply();
        }

        protected override void ResetValues()
        {
            base.ResetValues();
            CacheSerializedProperites();
        }

        private void CacheSerializedProperites()
        {
            m_PixelsPerUnit = serializedObject.FindProperty("m_PixelsPerUnit");
            Assert.IsNotNull(m_PixelsPerUnit);

            m_EdgesPerEllipse = serializedObject.FindProperty("m_EdgesPerEllipse");
            Assert.IsNotNull(m_EdgesPerEllipse);

            m_AnimationFramerate = serializedObject.FindProperty("m_AnimationFramerate");
            Assert.IsNotNull(m_AnimationFramerate);

            m_DefaultMaterial = serializedObject.FindProperty("m_DefaultMaterial");
            Assert.IsNotNull(m_DefaultMaterial);

            m_ObjectTypesXml = serializedObject.FindProperty("m_ObjectTypesXml");
            Assert.IsNotNull(m_ObjectTypesXml);
        }

        private void ReimportTiledAssets()
        {
            // Reimport tilesets first
            foreach (var guid in AssetDatabase.FindAssets("t:SuperAssetTileset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            // Then templates
            foreach (var guid in AssetDatabase.FindAssets("t:SuperAssetTemplate"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            // Then maps
            foreach (var guid in AssetDatabase.FindAssets("t:SuperAssetMap"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private void DoGuiHeader()
        {
            EditorGUILayout.LabelField("Version: " + m_ST2USettings.Version);
            EditorGUILayout.Space();
        }

        private void DoGuiSettings()
        {
            EditorGUILayout.LabelField("SuperTiled2Unity Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PixelsPerUnit, m_PixelsPerUnitContent);
            EditorGUILayout.PropertyField(m_EdgesPerEllipse, m_EdgesPerEllipseContent);
            EditorGUILayout.PropertyField(m_DefaultMaterial, m_DefaultMaterialContent);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AnimationFramerate, m_AnimationFramerateContent);
            EditorGUILayout.HelpBox("In frames-per-second. Note: You will need to reimport all your tilesets after making changes to the animation framerate for tiles.", MessageType.None);
        }

        private void DoObjectTypesDisplay()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Object Types", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ObjectTypesXml, m_ObjectTypesXmlContent);

            DoDisplayPrefabReplacements();

            using (new GuiScopedIndent())
            {
                var title = string.Format("Custom Object Types Properties ({0})", m_ST2USettings.CustomObjectTypes.Count());
                var tip = "These are the objects created in the Object Types Editor in Tiled.";
                var content = new GUIContent(title, tip);

                m_ShowObjectTypes = EditorGUILayout.Foldout(m_ShowObjectTypes, content);

                if (m_ShowObjectTypes)
                {
                    using (new GuiScopedIndent())
                    {
                        foreach (var objectType in m_ST2USettings.CustomObjectTypes)
                        {
                            // Display color for custom property
                            GUI.enabled = false;
                            var objectTip = string.Format("Object type '{0}' described in '{1}' Xml file.", objectType.m_Name, m_ObjectTypesXml);
                            var objectContent = new GUIContent(objectType.m_Name, objectTip);
                            EditorGUILayout_ColorFieldNoEdit(objectContent, objectType.m_Color);

                            // Display custom properties
                            using (new GuiScopedIndent())
                            {
                                foreach (var customProperty in objectType.m_CustomProperties)
                                {
                                    EditorGUILayout.TextField(customProperty.m_Name, customProperty.m_Value);
                                }

                            }
                            GUI.enabled = true;
                        }
                    }
                }
            }

            EditorGUILayout.Space();
        }

        private void DoDisplayPrefabReplacements()
        {
            EditorGUILayout.Space();

            // Display the prefab replacements for our Object Types
            {
                var title = "Prefab Replacements";
                var tip = "These prefabs will replace Tiled type objects during import.";
                var content = new GUIContent(title, tip);
                EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            }

            using (new GuiScopedIndent())
            {
                SerializedProperty listProperty = serializedObject.FindProperty("m_PrefabReplacements");

                for (int i = 0; i < listProperty.arraySize; i++)
                {
                    var soRep = listProperty.GetArrayElementAtIndex(i);
                    var soTypeName = soRep.FindPropertyRelative("m_TypeName");
                    var soPrefab = soRep.FindPropertyRelative("m_Prefab");

                    var label = soTypeName.stringValue;
                    var tip = string.Format("Replace Tile Objects named '{0}' with this prefab instance.", label);
                    var content = new GUIContent(label, tip);

                    EditorGUILayout.PropertyField(soPrefab, content);
                }
            }

            EditorGUILayout.Space();
        }

        private void DoGuiLayerColors()
        {
            EditorGUILayout.LabelField("Layer Colors (Physics)", EditorStyles.boldLabel);

            SerializedProperty listProperty = serializedObject.FindProperty("m_LayerColors");

            using (new GuiScopedIndent())
            {
                for (int i = 0; i < 32; i++)
                {
                    string layerName = LayerMask.LayerToName(i);
                    if (string.IsNullOrEmpty(layerName))
                    {
                        continue;
                    }

                    // For each layer that is named give the user a change to modify its color
                    SerializedProperty indexProperty = listProperty.GetArrayElementAtIndex(i);
                    GUIContent indexPropertyContext = new GUIContent(layerName, string.Format("Select color for {0} tile layer colliders", layerName));
                    EditorGUILayout.PropertyField(indexProperty, indexPropertyContext);
                }
            }
        }

        private void DoGuiReimportAssets()
        {
            EditorGUILayout.LabelField("Reimport Assets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(@"You may want to reimport all Tiled assets after making changes to settings." +
                                @" Be aware this may take a few minutes if you have a lot of Tiled assets." +
                                @" This will force import tilesets, templates, and maps." +
                                @" Note that some assets may be reimported multiple times because of dependencies.",
                                MessageType.Info);
            if (GUILayout.Button("Reimport Tiled Assets"))
            {
                ReimportTiledAssets();
            }
        }

        private void AddSeperator()
        {
            EditorGUILayout.TextArea("", GUI.skin.horizontalSlider);
        }
    }
}
