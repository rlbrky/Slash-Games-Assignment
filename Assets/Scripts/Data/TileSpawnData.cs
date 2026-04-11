using System;

namespace Data
{
    [Serializable]
    public class TileSpawnData
    {
        public int column;
        public int row;
        public int layer;
        public TileType tileType;
    }
}