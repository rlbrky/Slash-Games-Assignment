using System;

namespace Data
{
    [Serializable]
    public class OrderRequirement
    {
        public TileType tileType;
        public bool isFulfilled;

        public OrderRequirement(TileType type)
        {
            tileType = type;
            isFulfilled = false;
        }
    }
}