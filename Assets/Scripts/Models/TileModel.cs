using System;
using Data;

namespace Models
{
    public class TileModel
    {
        public float Row { get; private set; }
        public float Column { get; private set; }
        public int Layer { get; private set; }
        public TileType TileType { get; private set; }
        public bool IsBlocked { get; private set; }

        public event Action<TileModel> OnBlockedStateChanged;
        
        public TileModel(TileSpawnData spawnData)
        {
            Column = spawnData.column;
            Row = spawnData.row;
            Layer = spawnData.layer;
            TileType = spawnData.tileType;
        }

        public void SetBlocked(bool blocked)
        {
            if(IsBlocked == blocked) return;
            
            IsBlocked = blocked;
            OnBlockedStateChanged?.Invoke(this);
        }
    }
}
