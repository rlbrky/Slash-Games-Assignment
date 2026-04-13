using System;

namespace Data
{
    [Serializable]
    public class TileSpawnData
    {
        public float column;
        public float row;
        public int layer;
        public TileType tileType;
    }
}