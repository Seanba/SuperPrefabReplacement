﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using SuperTiled2Unity;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

namespace SuperTiled2Unity.Editor
{
    [ScriptedImporter(ImporterConstants.MapVersion, ImporterConstants.MapExtension, ImporterConstants.MapImportOrder)]
    public partial class TmxAssetImporter : TiledAssetImporter
    {
        private SuperMap m_MapComponent;
        private GlobalTileDatabase m_GlobalTileDatabase;
        private Dictionary<uint, TilePolygonCollection> m_TilePolygonDatabase;
        private int m_ObjectIdCounter = 0;

        [SerializeField]
        private bool m_TilesAsObjects = false;
        public bool TilesAsObjects { get { return m_TilesAsObjects; } }

        [SerializeField]
        private SortingMode m_SortingMode = SortingMode.Stacked;
        public SortingMode SortingMode { get { return m_SortingMode; } }

        [SerializeField]
        private bool m_IsIsometric = false;
        public bool IsIsometric { get { return m_IsIsometric; } }

        [SerializeField]
        private string m_CustomImporterClassName = string.Empty;

        [SerializeField]
        private List<SuperTileset> m_InternalTilesets;

        protected override void InternalOnImportAsset()
        {
            base.InternalOnImportAsset();
            ImporterVersion = ImporterConstants.MapVersion;
            AddSuperAsset<SuperAssetMap>();

            XDocument doc = XDocument.Load(assetPath);
            if (doc != null)
            {
                var xMap = doc.Element("map");
                ProcessMap(xMap);
            }

            DoPrefabReplacements();
            DoCustomImporting();
        }

        private void ProcessMap(XElement xMap)
        {
            Assert.IsNotNull(xMap);
            Assert.IsNull(m_MapComponent);
            Assert.IsNull(m_GlobalTileDatabase);

            m_TilePolygonDatabase = new Dictionary<uint, TilePolygonCollection>();
            m_ObjectIdCounter = 0;
            RendererSorter.SortingMode = m_SortingMode;

            // Create our map and fill it out
            bool success = true;
            success = success && PrepareMainObject();
            success = success && ProcessMapAttributes(xMap);
            success = success && ProcessTilesetElements(xMap);

            if (success)
            {
                // Custom properties need to be in place before we process the map layers
                AddSuperCustomProperties(m_MapComponent.gameObject, xMap.Element("properties"));

                // Create our main grid object and add the layers to it
                ProcessMapLayers(m_MapComponent.gameObject, xMap);
            }
        }

        // The map object is our Main Asset - the prefab that is created in our scene when dragged into the hierarchy
        private bool PrepareMainObject()
        {
            var icon = SuperIcons.GetTmxIcon();

            // The Main Gameobject is our grid containing all the layers
            var goGrid = new GameObject("_MapMainObject");
            SuperImportContext.AddObjectToAsset("_MapPrfab", goGrid, icon);
            SuperImportContext.SetMainObject(goGrid);
            m_MapComponent = goGrid.AddComponent<SuperMap>();

            return true;
        }

        private bool ProcessMapAttributes(XElement xMap)
        {
            m_MapComponent.name = Path.GetFileNameWithoutExtension(this.assetPath);
            m_MapComponent.m_Version = xMap.GetAttributeAs<string>("version");
            m_MapComponent.m_TiledVersion = xMap.GetAttributeAs<string>("tiledversion");

            m_MapComponent.m_Orientation = xMap.GetAttributeAs<MapOrientation>("orientation");
            m_MapComponent.m_RenderOrder = xMap.GetAttributeAs<MapRenderOrder>("renderorder");

            m_MapComponent.m_Width = xMap.GetAttributeAs<int>("width");
            m_MapComponent.m_Height = xMap.GetAttributeAs<int>("height");

            m_MapComponent.m_TileWidth = xMap.GetAttributeAs<int>("tilewidth");
            m_MapComponent.m_TileHeight = xMap.GetAttributeAs<int>("tileheight");

            m_MapComponent.m_HexSideLength = xMap.GetAttributeAs<int>("hexsidelength");
            m_MapComponent.m_StaggerAxis = xMap.GetAttributeAs<StaggerAxis>("staggeraxis");
            m_MapComponent.m_StaggerIndex = xMap.GetAttributeAs<StaggerIndex>("staggerindex");

            m_MapComponent.m_Infinite = xMap.GetAttributeAs<bool>("infinite");
            m_MapComponent.m_BackgroundColor = xMap.GetAttributeAsColor("backgroundcolor", NamedColors.Gray);
            m_MapComponent.m_NextObjectId = xMap.GetAttributeAs<int>("nextobjectid");

            // Done reading in values from Xml. Update other properties that may have depended on those settings.
            m_MapComponent.UpdateProperties(SuperImportContext);

            var grid = m_MapComponent.gameObject.AddComponent<Grid>();
            grid.cellSize = m_MapComponent.CellSize;

            // Todo: figure out what to do about staggered and hex and Y-As-Z isometric
            switch (m_MapComponent.m_Orientation)
            {
#if UNITY_2018_3_OR_NEWER
                case MapOrientation.Isometric:
                    grid.cellLayout = GridLayout.CellLayout.Isometric;
                    break;
#endif
                default:
                    grid.cellLayout = GridLayout.CellLayout.Rectangle;
                    break;
            }

            m_IsIsometric = m_MapComponent.m_Orientation == MapOrientation.Isometric;
            return true;
        }

        private bool ProcessTilesetElements(XElement xMap)
        {
            Assert.IsNull(m_GlobalTileDatabase);

            bool success = true;

            // Our tile database will be fed with tiles from each referenced tileset
            m_GlobalTileDatabase = new GlobalTileDatabase();
            m_InternalTilesets = new List<SuperTileset>();

            foreach (var xTileset in xMap.Elements("tileset"))
            {
                if (xTileset.Attribute("source") != null)
                {
                    success = success && ProcessTilesetElementExternal(xTileset);
                }
                else
                {
                    success = success && ProcessTilesetElementInternal(xTileset);
                }
            }

            return success;
        }

        private bool ProcessTilesetElementExternal(XElement xTileset)
        {
            Assert.IsNotNull(xTileset);
            Assert.IsNotNull(m_GlobalTileDatabase);

            var firstId = xTileset.GetAttributeAs<int>("firstgid");
            var source = xTileset.GetAttributeAs<string>("source");

            // Load the tileset and process the tiles inside
            var tileset = RequestAssetAtPath<SuperTileset>(source);

            if (tileset == null)
            {
                // Tileset is either missing or is not yet ready
                ReportError("Missing tileset asset: {0}", source);
                return false;
            }
            else
            {
                if (tileset.m_HasErrors)
                {
                    ReportError("Errors detected in tileset '{0}'. Check the tileset inspector for more details. Your map may be broken until these are fixed.", source);
                }

                // Register all the tiles with the tile database for this map
                m_GlobalTileDatabase.RegisterTileset(firstId, tileset);
            }

            return true;
        }

        private bool ProcessTilesetElementInternal(XElement xTileset)
        {
            var firstId = xTileset.GetAttributeAs<int>("firstgid");
            var name = xTileset.GetAttributeAs<string>("name");

            var tileset = ScriptableObject.CreateInstance<SuperTileset>();
            tileset.m_IsInternal = true;
            tileset.name = name;
            m_InternalTilesets.Add(tileset);

            string assetName = string.Format("_TilesetScriptObjectInternal_{0}", m_InternalTilesets.Count);
            SuperImportContext.AddObjectToAsset(assetName, tileset);

            // In the case of internal tilesets, only use an atlas if it will help with seams
            bool useAtlas = xTileset.Element("image") != null;
            var loader = new TilesetLoader(tileset, this, useAtlas, 2048, 2048);

            if (loader.LoadFromXml(xTileset))
            {
                m_GlobalTileDatabase.RegisterTileset(firstId, tileset);
                ReportWarning("Tileset '{0}' is an embedded tileset. Exported tilesets are preferred.", tileset.name);
                return true;
            }

            return false;
        }

        private void ProcessMapLayers(GameObject goParent, XElement xMap)
        {
            // Note that this method is re-entrant due to group layers
            foreach (XElement xNode in xMap.Elements())
            {
                if (!xNode.GetAttributeAs<bool>("visible", true))
                {
                    continue;
                }

                if (xNode.Name == "layer")
                {
                    ProcessTileLayer(goParent, xNode);
                }
                else if (xNode.Name == "group")
                {
                    ProcessGroupLayer(goParent, xNode);
                }
                else if (xNode.Name == "objectgroup")
                {
                    ProcessObjectLayer(goParent, xNode);
                }
                else if (xNode.Name == "imagelayer")
                {
                    ProcessImageLayer(goParent, xNode);
                }
            }
        }

        private void DoPrefabReplacements()
        {
            // Should any of our objects (from Tiled) be replaced by instantiated prefabs?
            var supers = m_MapComponent.GetComponentsInChildren<SuperObject>();
            foreach (var so in supers)
            {
                var prefab = SuperImportContext.Settings.GetPrefabReplacement(so.m_Type);
                if (prefab != null)
                {
                    // Replace the super object with the instantiated prefab
                    var instance = Instantiate(prefab, so.transform.position + prefab.transform.localPosition, so.transform.rotation);
                    instance.transform.SetParent(so.transform.parent);

                    // Apply custom properties as messages to the instanced prefab
                    var props = so.GetComponent<SuperCustomProperties>();
                    if (props != null)
                    {
                        foreach (var p in props.m_Properties)
                        {
                            instance.gameObject.BroadcastProperty(p);
                        }
                    }

                    // Keep the name from Tiled.
                    string name = so.gameObject.name;
                    DestroyImmediate(so.gameObject);
                    instance.name = name;
                }
            }
        }

        private void DoCustomImporting()
        {
            ApplyUserImporter();
            ApplyAutoImporters();
        }

        private void ApplyUserImporter()
        {
            if (!string.IsNullOrEmpty(m_CustomImporterClassName))
            {
                var type = Type.GetType(m_CustomImporterClassName);

                if (type == null)
                {
                    ReportError("Custom Importer error. Class type '{0}' is missing.", m_CustomImporterClassName);
                    return;
                }

                RunCustomImporterType(type);
            }
        }

        private void ApplyAutoImporters()
        {
            foreach (var type in AutoCustomTmxImporterAttribute.GetOrderedAutoImportersTypes())
            {
                RunCustomImporterType(type);
            }
        }

        private void RunCustomImporterType(Type type)
        {
            // Instantiate a custom importer class for specialized projects to use
            CustomTmxImporter customImporter;
            try
            {
                customImporter = Activator.CreateInstance(type) as CustomTmxImporter;
            }
            catch (Exception e)
            {
                ReportError("Error creating custom importer class. Message = '{0}'", e.Message);
                return;
            }

            try
            {
                var args = new TmxAssetImportedArgs();
                args.AssetImporter = this;
                args.ImportedSuperMap = m_MapComponent;

                customImporter.TmxAssetImported(args);
            }
            catch (Exception e)
            {
                ReportError("Custom importer '{0}' threw an exception. Message = '{1}', Stack:\n{2}", customImporter.GetType().Name, e.Message, e.StackTrace);
            }
        }
    }
}
