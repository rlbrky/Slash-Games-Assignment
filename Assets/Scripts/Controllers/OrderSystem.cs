using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Data;
using Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Controllers
{
    public class OrderSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private TileDefinitionRegistry _registry;
        
        private readonly List<TileType> _allTileTypes = new();

        private const int OrderSize = 3;
        
        private OrderModel _activeOrder;
        private IBoardTileProvider _boardTileProvider;

        public OrderModel ActiveOrder => _activeOrder;

        public event Action<OrderModel> OnNewOrder;
        public event Action<OrderModel> OnOrderChanged;
        
        private void Awake()
        {
            _registry.Initialize();
        }

        public void Initialize(IBoardTileProvider boardTileProvider)
        {
            _boardTileProvider = boardTileProvider;
        }
        
        public void StartFirstOrder()
        {
            if (_boardTileProvider == null)
            {
                Debug.LogError("Order System has no IBoardTileProvider!");
                return;
            }
            
            GenerateNextOrder();
        }

        public bool TrySubmitTile(TileType type)
        {
            if (_activeOrder == null)
                return false;

            return _activeOrder.TryFulfill(type);
        }

        private void GenerateNextOrder()
        {
            List<TileType> requirements = BuildRequirements();

            if (requirements == null)
                return;

            if (_activeOrder != null)
            {
                _activeOrder.OnOrderChanged -= HandleOrderChanged;
                _activeOrder.OnOrderCompleted -= HandleOrderCompleted;
            }

            _activeOrder = new OrderModel(requirements);
            _activeOrder.OnOrderChanged += HandleOrderChanged;
            _activeOrder.OnOrderCompleted += HandleOrderCompleted;

            OnNewOrder?.Invoke(_activeOrder);
        }

        /// <summary>
        /// Builds a list of tile requirements guaranteed to be fulbillable.
        /// </summary>
        private List<TileType> BuildRequirements()
        {
            // Build our available tiles dictionary.
            var allAvailable = _boardTileProvider.GetRemainingTileCounts()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            var unblockedAvailable = _boardTileProvider.GetUnblockedTileCounts()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            var requirements = new List<TileType>();

            for (int i = 0; i < OrderSize; i++)
            {
                // Prefer unblocked tiles to make the game easier.
                var preferredCandidates = unblockedAvailable
                    .Where(kvp => kvp.Value > 0)
                    .Select(kvp => kvp.Key).ToList();
                
                var fallbackCandidates = allAvailable
                    .Where(kvp => kvp.Value > 0)
                    .Select(kvp => kvp.Key).ToList();

                if (preferredCandidates.Count == 0 && fallbackCandidates.Count == 0)
                {
                    Debug.LogWarning($"Ran out of available tile types at slot {i}.");
                    return requirements.Count > 0 ? requirements : null;
                }
                
                var pool = preferredCandidates.Count > 0 ? preferredCandidates : fallbackCandidates;
                var chosen = pool[Random.Range(0, pool.Count)];
                requirements.Add(chosen);
                
                // Decrement from both pools to stay consistent
                if(unblockedAvailable.ContainsKey(chosen))
                    unblockedAvailable[chosen]--;
                
                if(allAvailable.ContainsKey(chosen))
                    allAvailable[chosen]--;
            }
            
            return requirements;
        }

        private void HandleOrderChanged(OrderModel order) => OnOrderChanged?.Invoke(order);

        private void HandleOrderCompleted(OrderModel order)
        {
            GenerateNextOrder();
        }
    }
}