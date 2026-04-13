using System.Collections.Generic;
using System.Linq;
using Data;
using UnityEngine;

[CreateAssetMenu(fileName = "TileDefinitionRegistry", menuName = "TileJam/Tile Definition Registry")]
public class TileDefinitionRegistry : ScriptableObject
{
    [SerializeField] private List<TileDefinition> _definitions;

    private Dictionary<TileType, TileDefinition> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<TileType, TileDefinition>();
        foreach (TileDefinition tileDefinition in _definitions)
            _lookup.Add(tileDefinition.Type, tileDefinition);
    }

    public TileDefinition Get(TileType tileType)
    {
        _lookup.TryGetValue(tileType, out TileDefinition tileDefinition);
        return tileDefinition;
    }

    public List<TileType> GetAllTileTypesList()
    {
        return _definitions
            .Where(d => d != null && d.Type != TileType.None)
            .Select(d => d.Type)
            .ToList();
    }
    
    private void OnEnable()
    {
        Initialize();
    }
}
