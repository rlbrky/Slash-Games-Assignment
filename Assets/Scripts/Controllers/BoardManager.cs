using System;
using System.Collections.Generic;
using System.Linq;
using Data;
using DG.Tweening;
using Models;
using UnityEngine;
using Views;

namespace Controllers
{
    public class BoardManager : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TileDefinitionRegistry _registry;

        [SerializeField] private LevelDefinition _level;
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private RectTransform _boardRoot;

        [Header("Layout")] 
        [SerializeField] private float _tileSize = 100f;
        [SerializeField] private float _layerOffset = 6f;
        [SerializeField] private float _overlapLimit = 0.25f;
        [SerializeField] private float _layerScaleBonus = 0.04f;

        [Header("Systems")] 
        [SerializeField] private OrderSystem _orderSystem;
        [SerializeField] private OrderView _orderView;
        [SerializeField] private RackSystem _rackSystem;
        [SerializeField] private RackView _rackView;
        [SerializeField] private GameStateManager _gameStateManager;

        private readonly List<TileModel> _allTiles = new();
        private readonly Dictionary<TileModel, TileView> _tileViews = new();

        public event Action OnBoardCleared;
        
        private void Awake()
        {
            _registry.Initialize();
        }

        private void Start()
        {
            BuildBoard();
            _orderView.BindToSystem(_orderSystem);
            _rackView.BindToSystem(_rackSystem);
            _orderSystem.StartFirstOrder();
            _gameStateManager.BindToSystems(_rackSystem, this);
        }

        /// <summary>
        /// Creates game level using TileModel and TileDefinition info.
        /// </summary>
        private void BuildBoard()
        {
            foreach (var spawnData in _level.Tiles)
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
                rectTransform.localScale = Vector3.one * (1f + model.Layer * _layerScaleBonus);

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
                tile.SetBlocked(_allTiles.Any(other => other.Layer > tile.Layer && Overlaps(tile, other)));
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
            bool matched = _orderSystem.TrySubmitTile(model.TileType);

            if (matched)
            {
                int slotIndex = _orderView.GetFirstUnfulfilledSlotIndex(_orderSystem.ActiveOrder);
                Vector3 targetPos = _orderView.GetSlotWorldPosition(slotIndex);
                RemoveTileWithAnimation(model, targetPos);
                return;
            }

            bool addedToRack = _rackSystem.TryAddTile(model.TileType);
            if (addedToRack)
            {
                Vector3 targetPos = _rackView.GetNextEmptySlotWorldPosition(_rackSystem.Model);
                RemoveTileWithAnimation(model, targetPos);
            }
            else
                Debug.LogWarning("Rack is full - tile couldn't be added!");
        }

        private void RemoveTileWithAnimation(TileModel model, Vector3 targetWorldPos)
        {
            if (!_tileViews.TryGetValue(model, out var view)) return;

            _tileViews.Remove(model);
            _allTiles.Remove(model);
            RecalculateBlocking();

            view.AnimateFlyTo(targetWorldPos, () =>
            {
                Destroy(view.gameObject);
                if (_allTiles.Count == 0)
                    OnBoardCleared?.Invoke();
            });
        }

        #region Helpers

        private Vector2 GetTilePosition(TileModel model)
        {
            float gridWidth = (_level.ColumnCount - 1) * _tileSize;
            float gridHeight = (_level.RowCount - 1) * _tileSize;

            float startX = -gridWidth / 2f;
            float startY = -gridHeight / 2f;

            return new Vector2(
                startX + model.Column * _tileSize + model.Layer * _layerOffset,
                startY + model.Row * _tileSize + model.Layer * _layerOffset
            );
        }

        private bool Overlaps(TileModel tile, TileModel other)
        {
            float overlapX = 1f - Mathf.Abs(tile.Column - other.Column);
            float overlapY = 1f - Mathf.Abs(tile.Row - other.Row);
            return overlapX > _overlapLimit && overlapY > _overlapLimit;
        }

        #endregion
    }
}