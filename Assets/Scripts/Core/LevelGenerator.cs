using System.Collections.Generic;
using System.Linq;
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
        public static (List<TileSpawnData> tiles, List<TileType> orderSequence) Generate(LevelGenerationSettings settings)
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
                int tilesOnLayer = Mathf.Min(settings.TilesPerLayer[layer], tilePool.Count - poolIndex);
                
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
            
            var orderSequence = SimulateSolutionOrders(tiles, settings);
            return (tiles, orderSequence);
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
            int totalTiles = settings.TilesPerLayer.Sum();
            
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

            // To prevent out-of-bounds tiles
            float maxCol = columns - 1f;
            float maxRow = rows - 1f;
            
            var all = new List<Vector2>();
            for (int col = 0; col < effectiveColumns; col++)
            {
                for (int row = 0; row < effectiveRows; row++)
                {
                    float x = Mathf.Clamp(col + offset, 0f, maxCol);
                    float y = Mathf.Clamp(row + offset, 0f, maxRow);
                    all.Add(new Vector2(x, y));
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

        /// <summary>
        /// Picks most accessible tile types at each step, creating a solution path.
        /// </summary>
        public static List<TileType> SimulateSolutionOrders(List<TileSpawnData> tiles, LevelGenerationSettings settings)
        {
            // Uses a copy so that original layout is unaffected.
            var remaining = tiles.Select(t => new TileSpawnData
            {
                column = t.column,
                row = t.row,
                layer = t.layer,
                tileType = t.tileType,
            }).ToList();
            
            List<TileType> orders = new List<TileType>();

            while (remaining.Count > 0)
            {
                var unblockedCounts = remaining
                    .Where(t => !SimulateIsBlocked(t, remaining, settings))
                    .GroupBy(t => t.tileType)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                var totalCounts = remaining
                    .GroupBy(t => t.tileType)
                    .ToDictionary(k => k.Key, k => k.Count());
                
                // Only types with 3+ total remaining are eligible for a full order.
                // Among eligible, prefer with most currently unblocked copies.
                var chosen = unblockedCounts
                    .Where(kvp => totalCounts.ContainsKey(kvp.Key) && totalCounts[kvp.Key] >= 3)
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => (TileType?)kvp.Key)
                    .FirstOrDefault();

                if (chosen == null)
                {
                    Debug.LogError("Level simulation failed — no valid order found. Check tile counts are multiples of 3.");
                    break;
                }
                
                orders.Add(chosen.Value);

                // Remove 3 tiles of the chosen type, prefer unblocked ones
                var toRemove = remaining
                    .Where(t => t.tileType == chosen.Value)
                    .OrderBy(t => SimulateIsBlocked(t, remaining, settings) ? 1 : 0)
                    .Take(3)
                    .ToList();
                
                foreach (TileSpawnData tile in toRemove)
                    remaining.Remove(tile);
            }

            return orders;
        }

        private static bool SimulateIsBlocked(TileSpawnData tile, List<TileSpawnData> all, LevelGenerationSettings settings)
        {
            return all.Any(other =>
                other != tile &&
                other.layer > tile.layer &&
                SimulateOverlaps(tile, other, settings));
        }

        private static bool SimulateOverlaps(TileSpawnData lower, TileSpawnData upper, LevelGenerationSettings settings)
        {
            float layerOffsetInGridUnits = settings.LayerOffset / settings.TileSize;
            int layerDiff = upper.layer - lower.layer;
            float visualShift = layerDiff * layerOffsetInGridUnits;

            float effectiveUpperColumn = upper.column + visualShift;
            float effectiveUpperRow = upper.row + visualShift;
            
            float overlapX = 1f - Mathf.Abs(lower.column - effectiveUpperColumn);
            float overlapY = 1f - Mathf.Abs(lower.row - effectiveUpperRow);
            
            float upperScale = 1f + upper.layer * settings.LayerScaleBonus;
            float effectiveLimit = settings.OverlapLimit / upperScale;
            
            return overlapX > effectiveLimit && overlapY > effectiveLimit;
        }
    }
}
