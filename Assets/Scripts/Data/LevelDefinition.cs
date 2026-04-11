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
    }
}
