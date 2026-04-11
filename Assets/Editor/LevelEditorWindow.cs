using System;
using System.Collections.Generic;
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

        private int _maxLayer = 3;

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
            for (int i = 0; i < _maxLayer; i++)
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

            int columns = _targetLevel.ColumnCount;
            int rows = _targetLevel.RowCount;

            float totalWidth = columns * (CellSize + CellPadding);
            float totalHeight = rows * (CellSize + CellPadding);
            
            Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);

            // Draw rows top-to-bottom so row 0 is at the bottom visually
            for (int row = rows - 1; row >= 0; row--)
            {
                for (int col = 0; col < columns; col++)
                {
                    float x = gridRect.x + col * (CellSize + CellPadding);
                    float y = gridRect.y + (rows - 1 - row) * (CellSize + CellPadding);
                    
                    Rect cellRect = new Rect(x, y, CellSize, CellSize);
                    DrawCell(cellRect, col, row);
                }
            }
            
            HandleGridInput(gridRect, columns, rows);
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCell(Rect rect, int col, int row)
        {
            var tilesOnCell = GetTilesAt(col, row);
            
            // Background
            EditorGUI.DrawRect(rect, Color.blue);

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
                Color tileColor = definition != null ? definition.TileColor : Color.white;
                
                // Dim tiles not on the selected layer
                if (tile.layer != _selectedLayer)
                    tileColor = Color.Lerp(tileColor, Color.black, 0.5f);
                
                EditorGUI.DrawRect(tileRect, tileColor);

                // Draw tile type initial as label
                var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 8,
                    normal = { textColor = Color.white }
                };
                EditorGUI.LabelField(tileRect, tile.tileType.ToString().Substring(0, 2), labelStyle);
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
            if (col == 0 || row == 0)
            {
                var coordStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 7,
                    normal = { textColor = Color.white }
                };
                EditorGUI.LabelField(rect, $"{col},{row}", coordStyle);
            }
        }

        private void HandleGridInput(Rect gridRect, int columns, int rows)
        {
            Event e = Event.current;

            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) return;
            if (!gridRect.Contains(e.mousePosition)) return;

            int col = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / (CellSize + CellPadding));
            int row = rows - 1 - Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / (CellSize + CellPadding));

            if (col < 0 || col >= columns || row < 0 || row >= rows) return;

            if (e.button == 0)
                PlaceTile(col, row, _selectedLayer, _selectedTileType);
            else if (e.button == 1)
                EraseTile(col, row, _selectedLayer);

            e.Use();
            EditorUtility.SetDirty(_targetLevel);
            Repaint();
        }

        #endregion

        #region Tile Operations

        private List<TileSpawnData> GetTilesAt(int col, int row)
        {
            var result = new List<TileSpawnData>();
            foreach (var tile in _targetLevel.Tiles)
            {
                if (tile.column == col && tile.row == row)
                    result.Add(tile);
            }
            return result;
        }
        
        private void PlaceTile(int col, int row, int layer, TileType tileType)
        {
            // Replace if tile already exists on this layer at this position
            EraseTile(col, row, layer);
            _targetLevel.AddTile(new TileSpawnData { column = col, row = row, layer = layer, tileType = tileType });
        }

        private void EraseTile(int col, int row, int layer)
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
    }
}
