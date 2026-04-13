using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "LevelGenerationSettings", menuName = "TileJam/Level Generation Settings")]
    public class LevelGenerationSettings : ScriptableObject
    {
        [Header("Seed")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private bool _useRandomSeed;

        [Header("Board")]
        [SerializeField] private int _columns = 7;
        [SerializeField] private int _rows = 9;
        [SerializeField] private int _layerCount = 3;
        [SerializeField] private int _tilesPerLayer = 15;

        [Header("Tile Types")]
        [SerializeField] private TileDefinitionRegistry _tileRegistry;
        
        public int Seed => _useRandomSeed ? Random.Range(0, int.MaxValue) : _seed;
        public int Columns => _columns;
        public int Rows => _rows;
        public int LayerCount => _layerCount;
        public int TilesPerLayer => _tilesPerLayer;
        public TileDefinitionRegistry TileRegistry => _tileRegistry;
    }
}
