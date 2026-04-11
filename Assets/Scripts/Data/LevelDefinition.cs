using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Level", menuName = "TileJam/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Board Settings")]
        [SerializeField] private int columnCount = 7;
        [SerializeField] private int rowCount = 9;

        [Header("Tiles")]
        [SerializeField] private List<TileSpawnData> _tiles = new();
        
        public int RowCount => rowCount;
        public int ColumnCount => columnCount;

        public IReadOnlyList<TileSpawnData> Tiles => _tiles;
        
#if UNITY_EDITOR
        public void AddTile(TileSpawnData tile)
        {
            _tiles.Add(tile);
        }

        public void RemoveTile(int column, int row, int layer)
        {
            _tiles.RemoveAll(t => t.column == column && t.row == row && t.layer == layer);
        }

        public void RemoveAllTilesOnLayer(int layer)
        {
            _tiles.RemoveAll(t => t.layer == layer);
        }

        public void RemoveAllTiles()
        {
            _tiles.Clear();
        }
#endif
    }
}
