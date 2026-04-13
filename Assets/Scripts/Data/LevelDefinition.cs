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

        [Header("Procedural Generation")]
        [SerializeField] private bool _useProceduralGeneration;
        [SerializeField] private LevelGenerationSettings _levelGenerationSettings;
        
        [Header("Pre-computed Orders")]
        [SerializeField] private List<TileType> _orderSequence = new();
        
        public int RowCount => rowCount;
        public int ColumnCount => columnCount;

        public IReadOnlyList<TileSpawnData> Tiles => _tiles;
        public IReadOnlyList<TileType> OrderSequence => _orderSequence;
        
        public bool UseProceduralGeneration => _useProceduralGeneration;
        public LevelGenerationSettings LevelGenerationSettings => _levelGenerationSettings;
        
#if UNITY_EDITOR
        public void AddTile(TileSpawnData tile)
        {
            _tiles.Add(tile);
        }

        public void RemoveTile(float column, float row, int layer)
        {
            _tiles.RemoveAll(t =>
                Mathf.Approximately(t.column, column) && 
                Mathf.Approximately(t.row, row) && 
                t.layer == layer);
        }

        public void RemoveAllTilesOnLayer(int layer)
        {
            _tiles.RemoveAll(t => t.layer == layer);
        }

        public void RemoveAllTiles()
        {
            _tiles.Clear();
        }

        public void SetOrderSequence(List<TileType> orderSequence)
        {
            _orderSequence = orderSequence;
        }
#endif
    }
}
