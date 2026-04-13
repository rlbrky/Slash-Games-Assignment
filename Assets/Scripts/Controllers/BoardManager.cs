using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Data;
using DG.Tweening;
using Models;
using UnityEditor;
using UnityEngine;
using Views;

namespace Controllers
{
    public class BoardManager : MonoBehaviour, IBoardTileProvider
    {
        [Header("References")] 
        [SerializeField] private TileDefinitionRegistry _registry;

        [SerializeField] private LevelDefinition _level;
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private RectTransform _boardRoot;

        [Header("Systems")] 
        [SerializeField] private OrderSystem _orderSystem;
        [SerializeField] private OrderView _orderView;
        [SerializeField] private RackSystem _rackSystem;
        [SerializeField] private RackView _rackView;
        [SerializeField] private GameStateManager _gameStateManager;

        [Header("UI")]
        [SerializeField] private GameResultView _gameResultView;
        
        private readonly List<TileModel> _allTiles = new();
        private readonly Dictionary<TileModel, TileView> _tileViews = new();

        private float OverlapLimit => _level.LevelGenerationSettings.OverlapLimit;
        private float LayerScaleBonus => _level.LevelGenerationSettings.LayerScaleBonus;
        
        public event Action OnBoardCleared;
        
        private void Awake()
        {
            _registry.Initialize();
        }

        private void Start()
        {
            BuildBoard();
            _orderSystem.Initialize(this, _rackSystem, _level.OrderSequence);
            _orderView.BindToSystem(_orderSystem);
            _orderSystem.OnRackTileDrained += slotIndex => _rackView.AnimateDrainSlot(slotIndex, null);
            _rackView.BindToSystem(_rackSystem);
            _orderSystem.StartFirstOrder();
            _gameStateManager.BindToSystems(_rackSystem, this);
            _gameResultView.BindToGameStateManager(_gameStateManager);
        }

        /// <summary>
        /// Creates game level using TileModel and TileDefinition info.
        /// </summary>
        private void BuildBoard()
        {
            List<TileSpawnData> tiles;

            if (_level.UseProceduralGeneration)
            {
                var result = LevelGenerator.Generate(_level.LevelGenerationSettings);
                tiles = result.tiles;
#if UNITY_EDITOR
                // Store computed order sequence back on the asset to make it persist
                _level.SetOrderSequence(result.orderSequence);
                EditorUtility.SetDirty(_level);
#endif
            }
            else
            {
                tiles = new List<TileSpawnData>(_level.Tiles);
            }
            
            foreach (var spawnData in tiles)
            {
                var model = new TileModel(spawnData);
                var definition = _registry.Get(spawnData.tileType);

                if (definition == null)
                {
                    Debug.LogWarning($"No tile definition for {spawnData.tileType}");
                    continue;
                }

                var view = Instantiate(_tilePrefab, _boardRoot);
                var rectTransform = view.Rect;

                rectTransform.anchoredPosition = GetTilePosition(model);
                // Make each layer's tiles bigger than the last for depth effect. Closer tiles look bigger.
                rectTransform.localScale = Vector3.one * (1f + model.Layer * _level.LevelGenerationSettings.LayerScaleBonus);

                view.Initialize(model, definition);
                view.OnTileClicked += HandleTileClicked;

                float delay = model.Layer * 0.05f;
                DOVirtual.DelayedCall(delay, () => view.AnimatePunchOnSpawn());
                
                _allTiles.Add(model);
                _tileViews[model] = view;
            }

            SortTilesByLayer();
            RecalculateBlocking();
        }

        public void RecalculateBlocking()
        {
            foreach (var tile in _allTiles)
            {
                var blockers = _allTiles.Where(other => other.Layer > tile.Layer && Overlaps(tile, other)).ToList();
                bool shouldBeBlocked = blockers.Count > 0;
        
                tile.SetBlocked(shouldBeBlocked);
            }
        }

        /// <summary>
        /// Helps unity draw tiles following our layer setup from data.
        /// </summary>
        private void SortTilesByLayer()
        {
            var sorted = _tileViews.OrderBy(pair => pair.Key.Layer).ToList();

            foreach (var pair in sorted)
                pair.Value.transform.SetAsLastSibling();
        }

        private void HandleTileClicked(TileModel model)
        {
            // Stop tracking immediately so GenerateNextOrder function in OrderSystem calculates accurately.
            _allTiles.Remove(model);
            RecalculateBlocking();
            
            bool matched = _orderSystem.TrySubmitTile(model.TileType);

            if (matched)
            {
                int slotIndex = _orderView.GetFirstUnfulfilledSlotIndex(_orderSystem.ActiveOrder);
                Vector3 targetPos = _orderView.GetSlotWorldPosition(slotIndex);
                AnimateAndDestroy(model, targetPos, null);
                return;
            }

            bool addedToRack = false;
            Vector3 rackTargetPos = Vector3.zero;

            // Since TryAddTile increments slot count, capture position before adding.
            rackTargetPos = _rackView.GetNextEmptySlotWorldPosition(_rackSystem.Model);
            addedToRack = _rackSystem.TryAddTile(model.TileType);

            if (addedToRack)
            {
                AnimateAndDestroy(model, rackTargetPos, () =>
                {
                    _orderSystem.DrainRackIntoOrder();
                    _rackSystem.CheckFullAfterAnimation();
                });
            }
            else
                Debug.LogWarning("Rack is full - tile couldn't be added!");
        }

        private void AnimateAndDestroy(TileModel model, Vector3 targetWorldPos, Action onComplete)
        {
            if (!_tileViews.TryGetValue(model, out var view)) return;

            _tileViews.Remove(model);

            view.AnimateFlyTo(targetWorldPos, () =>
            {
                Destroy(view.gameObject);
                
                if (_allTiles.Count == 0)
                    OnBoardCleared?.Invoke();
                
                onComplete?.Invoke();
            });
        }

        #region Helpers

        private Vector2 GetTilePosition(TileModel model)
        {
            float gridWidth = (_level.ColumnCount - 1) * _level.LevelGenerationSettings.TileSize;
            float gridHeight = (_level.RowCount - 1) * _level.LevelGenerationSettings.TileSize;

            int maxLayer = _level.UseProceduralGeneration
                ? _level.LevelGenerationSettings.LayerCount - 1
                : _level.Tiles.Max(t => t.layer);
            float totalLayerOffset = maxLayer * _level.LevelGenerationSettings.LayerOffset;
            
            float startX = -gridWidth / 2f - totalLayerOffset / 2f;
            float startY = -gridHeight / 2f - totalLayerOffset / 2f;

            return new Vector2(
                startX + model.Column * _level.LevelGenerationSettings.TileSize + model.Layer * _level.LevelGenerationSettings.LayerOffset,
                startY + model.Row * _level.LevelGenerationSettings.TileSize + model.Layer * _level.LevelGenerationSettings.LayerOffset
            );
        }

        private bool Overlaps(TileModel lower, TileModel upper)
        {
            // Convert layer offset from pixels to grid units
            float layerOffsetInGridUnits =
                _level.LevelGenerationSettings.LayerOffset / _level.LevelGenerationSettings.TileSize;
            int layerDiff = upper.Layer - lower.Layer;
            
            // Calculate visual shift caused by layer offset
            float visualShift = layerDiff * layerOffsetInGridUnits;
            
            // Adjust upper tile position to match visual position
            float effectiveUpperColumn = upper.Column + visualShift;
            float effectiveUpperRow = upper.Row + visualShift;
            
            float overlapX = 1f - Mathf.Abs(lower.Column - effectiveUpperColumn);
            float overlapY = 1f - Mathf.Abs(lower.Row - effectiveUpperRow);
            
            float upperScale = 1f + upper.Layer * _level.LevelGenerationSettings.LayerScaleBonus;
            float effectiveLimit = _level.LevelGenerationSettings.OverlapLimit / upperScale;
            bool result = overlapX > effectiveLimit && overlapY > effectiveLimit;

            return overlapX > effectiveLimit && overlapY > effectiveLimit;
        }

        public IReadOnlyDictionary<TileType, int> GetRemainingTileCounts()
        {
            var counts = new Dictionary<TileType, int>();
            foreach (TileModel tile in _allTiles)
            {
                if(!counts.ContainsKey(tile.TileType))
                    counts[tile.TileType] = 0;
                counts[tile.TileType]++;
            }
            
            return counts;
        }

        public IReadOnlyDictionary<TileType, int> GetUnblockedTileCounts()
        {
            var counts = new Dictionary<TileType, int>();
            foreach (TileModel tile in _allTiles.Where(t => !t.IsBlocked))
            {
                if(!counts.ContainsKey(tile.TileType))
                    counts[tile.TileType] = 0;
                counts[tile.TileType]++;
            }
            
            return counts;
        }
        
        #endregion
    }
}