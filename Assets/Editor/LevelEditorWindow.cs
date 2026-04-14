using System;
using System.Collections.Generic;
using Core;
using Data;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        #region Constants

        private const float CellSize = 40f;
        private const float CellPadding = 2f;
        private const float ToolbarHeight = 30f;
        private const float PaletteWidth = 160f;
        private const float TopBarHeight = 60f;

        #endregion

        #region State

        private LevelDefinition _targetLevel;
        private TileDefinitionRegistry _registry;

        private int _selectedLayer;
        private TileType _selectedTileType = TileType.Donut;

        private int MaxLayers
        {
            get
            {
                if (_targetLevel != null && _targetLevel.LevelGenerationSettings != null)
                    return _targetLevel.LevelGenerationSettings.LayerCount;
                
                // Fallback
                return 3;
            }
        }

        private Vector2 _gridScrollPos;
        private Vector2 _paletteScrollPos;

        private readonly List<TileType> _tileTypes = new();

        #endregion

        [MenuItem("TileJam/Level Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.minSize = new Vector2(700, 500);
        }

        private void OnEnable()
        {
            CacheFileTypes();
        }

        private void CacheFileTypes()
        {
            _tileTypes.Clear();
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                if(type == TileType.None)
                    continue;

                _tileTypes.Add(type);
            }
        }

        private void OnGUI()
        {
            DrawTopBar();

            if (_targetLevel == null || _registry == null)
            {
                EditorGUILayout.HelpBox("Assign a Level Definition and Tile Definition Registry above to begin editing.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawPalette();
            DrawGrid();
            EditorGUILayout.EndHorizontal();
        }

        #region Top Bar

        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _targetLevel = (LevelDefinition)EditorGUILayout.ObjectField("Level Definition", _targetLevel, typeof(LevelDefinition), false);
            _registry = (TileDefinitionRegistry)EditorGUILayout.ObjectField("Tile Definition Registry", _registry, typeof(TileDefinitionRegistry), false);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Active Layer", GUILayout.Width(90));
            for (int i = 0; i < MaxLayers; i++)
            {
                bool isSelected = _selectedLayer == i;
                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                if (GUILayout.Button($"Layer {i}", GUILayout.Width(70), GUILayout.Height(ToolbarHeight)))
                    _selectedLayer = i;
            }
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(20);

            if (GUILayout.Button("Clear All", GUILayout.Width(80), GUILayout.Height(ToolbarHeight)))
            {
                if (EditorUtility.DisplayDialog("Clear All Tiles", "Remove all tiles from every layer?", "Clear", "Cancel"))
                    ClearAll();
            }

            if (GUILayout.Button("Generate Orders", GUILayout.Width(110), GUILayout.Height(ToolbarHeight)))
            {
                GenerateOrderSequence();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Palette

        private void DrawPalette()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(PaletteWidth));
            GUILayout.Label("Tile Palette", EditorStyles.boldLabel);
            
            _paletteScrollPos = EditorGUILayout.BeginScrollView(_paletteScrollPos);

            foreach (TileType tileType in _tileTypes)
            {
                bool isSelected = _selectedTileType == tileType;
                GUI.backgroundColor = isSelected ? Color.yellow : Color.white;

                var definition = _registry.Get(tileType);
                string label = definition != null ? tileType.ToString() : $"{tileType} (missing)";

                if (GUILayout.Button(label, GUILayout.Height(30)))
                    _selectedTileType = tileType;
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Grid

        private void DrawGrid()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"Layer {_selectedLayer} - click to place, right-click to erase", EditorStyles.miniLabel);

            _gridScrollPos = EditorGUILayout.BeginScrollView(_gridScrollPos);

            bool isOddLayer = _selectedLayer % 2 != 0;
            float offset = isOddLayer ? 0.5f : 0f;

            int effectiveColumns = isOddLayer ? _targetLevel.ColumnCount - 1 : _targetLevel.ColumnCount;
            int effectiveRows = isOddLayer ? _targetLevel.RowCount - 1 : _targetLevel.RowCount;

            Rect gridRect = GUILayoutUtility.GetRect(
                effectiveColumns * (CellSize + CellPadding),
                effectiveRows * (CellSize + CellPadding));

            // Draw rows top-to-bottom so row 0 is at the bottom visually
            for (int row = effectiveRows - 1; row >= 0; row--)
            {
                for (int col = 0; col < effectiveColumns; col++)
                {
                    float x = gridRect.x + col * (CellSize + CellPadding);
                    float y = gridRect.y + (effectiveRows - 1 - row) * (CellSize + CellPadding);
                    
                    DrawCell(new Rect(x, y, CellSize, CellSize), col + offset, row + offset);
                }
            }
            
            HandleGridInput(gridRect, effectiveColumns, effectiveRows, offset);
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCell(Rect rect, float col, float row)
        {
            var tilesOnCell = GetTilesAt(col, row);
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.2f));

            // To show cell bounds
            DrawRectBorder(rect, new Color(0.3f, 0.3f, 0.4f), 1f);
            
            foreach (var tile in tilesOnCell)
            {
                float inset = tile.layer * 3f;
                Rect tileRect = new Rect(
                    rect.x + inset,
                    rect.y + inset,
                    rect.width - inset * 2f,
                    rect.height - inset * 2f
                );

                var definition = _registry.Get(tile.tileType);
                Color tileColor = definition != null ? definition.TileColor : Color.magenta;
                
                // Dim tiles not on the selected layer
                bool isActiveLayer = tile.layer == _selectedLayer;
                if (!isActiveLayer)
                    tileColor = Color.Lerp(tileColor, Color.black, 0.6f);
                
                EditorGUI.DrawRect(tileRect, tileColor);

                // Draw tile type initial as label
                string label = tile.tileType.ToString();
                label = label.Length >= 2 ? label.Substring(0, 2) : label;
                
                var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = isActiveLayer ? 12 : 8,
                    fontStyle = isActiveLayer ? FontStyle.Bold : FontStyle.Normal,
                    normal = { textColor = GetContrastingTextColor(tileColor) }
                };
                EditorGUI.LabelField(tileRect, label, labelStyle);
                
                // Highlight active layer tile with a border
                if(isActiveLayer)
                    DrawRectBorder(tileRect, Color.yellow, 2f);
            }
            
            // Highlight active layer tile slot with border
            bool hasOnActiveLayer = tilesOnCell.Exists(t => t.layer == _selectedLayer);
            if (!hasOnActiveLayer)
            {
                //Draw empty slot indicator for active layer
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Color.gray);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), Color.gray);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Color.gray);
            }
            
            // Draw col/row label on bottom-left cell
            // Draw col/row label on every cell
            var coordStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 7,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            EditorGUI.LabelField(new Rect(rect.x + 2, rect.yMax - 12, 40, 12), $"{col},{row}", coordStyle);
        }

        private void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color); // Top
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color); // Bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color); // Left
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color); // Right
        }

        private Color GetContrastingTextColor(Color background)
        {
            float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
            return luminance > 0.5f ? Color.black : Color.white;
        }
        
        private void HandleGridInput(Rect gridRect, int columns, int rows, float offset)
        {
            Event e = Event.current;

            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
            if (!gridRect.Contains(e.mousePosition)) return;

            int col = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / (CellSize + CellPadding));
            int row = rows - 1 - Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / (CellSize + CellPadding));

            if (col < 0 || col >= columns || row < 0 || row >= rows) return;

            float actualCol = col + offset;
            float actualRow = row + offset;
            
            if (e.button == 0)
                PlaceTile(actualCol, actualRow, _selectedLayer, _selectedTileType);
            else if (e.button == 1)
                EraseTile(actualCol, actualRow, _selectedLayer);

            e.Use();
            EditorUtility.SetDirty(_targetLevel);
            Repaint();
        }

        #endregion

        #region Tile Operations

        private List<TileSpawnData> GetTilesAt(float col, float row)
        {
            var result = new List<TileSpawnData>();
            foreach (var tile in _targetLevel.Tiles)
            {
                if (Mathf.Approximately(tile.column, col) && Mathf.Approximately(tile.row, row))
                    result.Add(tile);
            }
            return result;
        }
        
        private void PlaceTile(float col, float row, int layer, TileType tileType)
        {
            // Replace if tile already exists on this layer at this position
            EraseTile(col, row, layer);
            _targetLevel.AddTile(new TileSpawnData { column = col, row = row, layer = layer, tileType = tileType });
        }

        private void EraseTile(float col, float row, int layer)
        {
            _targetLevel.RemoveTile(col, row, layer);
        }

        private void ClearLayer(int layer)
        {
            _targetLevel.RemoveAllTilesOnLayer(layer);
            EditorUtility.SetDirty(_targetLevel);
        }

        private void ClearAll()
        {
            _targetLevel.RemoveAllTiles();
            EditorUtility.SetDirty(_targetLevel);
        }
        
        #endregion

        private void GenerateOrderSequence()
        {
            if (_targetLevel.Tiles.Count == 0)
            {
                EditorUtility.DisplayDialog("No Tiles", "Place some tiles first before generating orders.", "OK");
                return;
            }
            
            // Ensure tile count is multiple of 3
            var tileCounts = new Dictionary<TileType, int>();
            foreach (var tile in _targetLevel.Tiles)
            {
                if(!tileCounts.ContainsKey(tile.tileType))
                    tileCounts[tile.tileType] = 0;
                tileCounts[tile.tileType]++;
            }

            var invalidTypes = new List<string>();
            foreach (var kvp in tileCounts)
            {
                if(kvp.Value % 3 != 0)
                    invalidTypes.Add($"{kvp.Key}: ({kvp.Value})");
            }

            if (invalidTypes.Count > 0)
            {
                string message = "Tile counts must be multiples of 3 for solvability.\n\nInvalid counts:\n" + 
                                 string.Join("\n", invalidTypes);
                EditorUtility.DisplayDialog("Invalid Tile Counts", message, "OK");
                return;
            }
            
            // Generate order sequence using LevelGenerator's simulation
            var tiles = new List<TileSpawnData>(_targetLevel.Tiles);
            var orderSequence = LevelGenerator.SimulateSolutionOrders(tiles, _targetLevel.LevelGenerationSettings);

            _targetLevel.SetOrderSequence(orderSequence);
            EditorUtility.SetDirty(_targetLevel);
            
            EditorUtility.DisplayDialog("Orders Generated", 
                $"Generated {orderSequence.Count} orders for {_targetLevel.Tiles.Count} tiles.", "OK");
        }
    }
}
