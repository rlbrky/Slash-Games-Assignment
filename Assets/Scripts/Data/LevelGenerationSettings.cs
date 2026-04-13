using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "LevelGenerationSettings", menuName = "TileJam/Level Generation Settings")]
    public class LevelGenerationSettings : ScriptableObject
    {
        [Header("Seed")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _useRandomSeed;

        [Header("Layout")] 
        [SerializeField] private float _tileSize = 160f;
        [SerializeField] private float _layerOffset = 40f;
        [SerializeField] private float _overlapLimit = 0.15f;
        [SerializeField] private float _layerScaleBonus = 0.1f;
        
        [Header("Board")]
        [SerializeField] private int _columns = 7;
        [SerializeField] private int _rows = 9;
        [SerializeField] [Tooltip("Bottom to top E.g. [25,15,10]")] private List<int> _tilesPerLayer = new() { 25, 15, 10};

        [Header("Tile Types")]
        [SerializeField] private TileDefinitionRegistry _tileRegistry;
        
        public int Seed => _useRandomSeed ? Random.Range(0, int.MaxValue) : _seed;
        public int Columns => _columns;
        public int Rows => _rows;
        public int LayerCount => _tilesPerLayer.Count;
        public IReadOnlyList<int> TilesPerLayer => _tilesPerLayer;
        public TileDefinitionRegistry TileRegistry => _tileRegistry;

        public float TileSize => _tileSize;
        public float LayerOffset => _layerOffset;
        public float OverlapLimit => _overlapLimit;
        public float LayerScaleBonus => _layerScaleBonus;
    }
}
