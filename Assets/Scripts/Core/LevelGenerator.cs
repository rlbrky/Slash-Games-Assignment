using System.Collections.Generic;
using Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core
{
    public static class LevelGenerator
    {
        /// <summary>
        /// Generates a reproducible tile layout from the given settings.
        /// The same seed always produces the same board, enabling replayable levels.
        /// </summary>
        public static List<TileSpawnData> Generate(LevelGenerationSettings settings)
        {
            // Save current random state so we can restore it after generation.
            // This ensures our seeded generation doesn't affect any other system
            // that relies on Random (e.g. particle effects, AI, etc.)
            Random.State state = Random.state;
            Random.InitState(settings.Seed);
            
            var tiles = new List<TileSpawnData>();
            var tileCounts = BuildTileCounts(settings);

            // Flatten the type→count dictionary into a single list.
            // e.g. { Donut:6, Pizza:3 } becomes [Donut, Donut, Donut, Donut, Donut, Donut, Pizza, Pizza, Pizza]
            var tilePool = new List<TileType>();
            foreach (var kvp in tileCounts)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    tilePool.Add(kvp.Key);
                }
            }

            Shuffle(tilePool);
            
            // Fill layers bottom to top
            int poolIndex = 0;
            for (int layer = 0; layer < settings.LayerCount && poolIndex < tilePool.Count; layer++)
            {
                int tilesOnLayer = Mathf.Min(settings.TilesPerLayer, tilePool.Count - poolIndex);
                
                // Get shuffled set of grid positions for current layer
                List<Vector2> positions = GetLayerPositions(settings.Columns, settings.Rows, tilesOnLayer, layer);

                foreach (Vector2 position in positions)
                {
                    if (poolIndex >= tilePool.Count)
                        break;
                    
                    tiles.Add(new TileSpawnData
                    {
                        column = position.x,
                        row = position.y,
                        layer = layer,
                        tileType = tilePool[poolIndex++]
                    });
                }
            }
            
            // Restore random state so other systems are unaffected by our seed.
            Random.state = state;
            return tiles;
        }

        /// <summary>
        /// Determines how many of each tile type to include in the level.
        /// Every type gets an equal share and all counts are multiples of 3
        /// so that every tile on the board can belong to a completable order.
        /// </summary>
        private static Dictionary<TileType, int> BuildTileCounts(LevelGenerationSettings settings)
        {
            var counts = new Dictionary<TileType, int>();
            var availableTypes = settings.TileRegistry.GetAllTileTypesList();
            int totalTiles = settings.LayerCount * settings.TilesPerLayer;
            
            if (availableTypes.Count == 0)
            {
                Debug.LogError("TileDefinitionRegistry has no valid definitions — cannot generate level.");
                return new Dictionary<TileType, int>();
            }
            
            // Since we are using int if totalTiles / 3 is decimal and we multiply by 3 afterwards
            // we will get a rounded down result.
            int adjustedTotal = (totalTiles / 3) * 3;
            // Each set is exactly 3 tiles of the same type
            int setsNeeded = adjustedTotal / 3;
            int typeCount = availableTypes.Count;
            
            // Distribute sets evenly across types
            // e.g. 4 sets across [Donut, Pizza, Cola] → Donut:6, Pizza:3, Cola:3
            for (int i = 0; i < setsNeeded; i++)
            {
                var type = availableTypes[i % typeCount];
                if (!counts.ContainsKey(type))
                    counts[type] = 0;
                counts[type] += 3;
            }
            
            return counts;
        }

        /// <summary>
        /// Generates tile positions for a layer using the half-unit stacking pattern.
        /// </summary>
        private static List<Vector2> GetLayerPositions(int columns, int rows, int tilesPerLayer, int layer)
        {
            // Odd layers shift by half a unit so upper tiles naturally center over lower tile corners.
            float offset = (layer % 2 == 0) ? 0f : 0.5f;
            
            // Odd layers have one fewer valid position per axis since the offset
            // would push the last column/row outside the board boundary.
            int effectiveColumns = (layer % 2 == 0) ? columns : columns - 1;
            int effectiveRows = (layer % 2 == 0) ? rows : rows - 1;
            
            var all = new List<Vector2>();
            for (int col = 0; col < effectiveColumns; col++)
            {
                for (int row = 0; row < effectiveRows; row++)
                {
                    all.Add(new Vector2(col + offset, row + offset));
                }
            }

            Shuffle(all);
            
            // avoids placing tiles in every cell
            return all.GetRange(0, Mathf.Min(tilesPerLayer, all.Count));
        }

        /// <summary>
        /// Fisher-Yates shuffle method
        /// </summary>
        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
