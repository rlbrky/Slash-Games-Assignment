using System.Collections.Generic;
using Data;

namespace Core
{
    public interface IBoardTileProvider
    {
        IReadOnlyDictionary<TileType, int> GetRemainingTileCounts();
        IReadOnlyDictionary<TileType, int> GetUnblockedTileCounts();
    }
}
