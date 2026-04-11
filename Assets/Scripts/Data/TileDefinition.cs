using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "TileDefinition", menuName = "TileJam/Tile Definition")]
    public class TileDefinition : ScriptableObject
    {
        [SerializeField] private TileType _tileType;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _tileColor = Color.white;
    
        public TileType Type => _tileType;
        public Sprite Icon => _icon;
        public Color TileColor => _tileColor;
    }
}
