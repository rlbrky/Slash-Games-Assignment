using System;
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
        private const int OrderSize = 3;
        
        private OrderModel _activeOrder;
        private IBoardTileProvider _boardTileProvider;

        public OrderModel ActiveOrder => _activeOrder;

        public event Action<OrderModel> OnNewOrder;
        public event Action<OrderModel> OnOrderChanged;
        
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
            var eligible = _boardTileProvider.GetRemainingTileCounts()
                .Where(kvp => kvp.Value >= OrderSize)
                .Select(kvp => kvp.Key)
                .ToList();

            if (eligible.Count == 0)
            {
                Debug.LogWarning("No tile type has 3 or more remaining — cannot generate order.");
                return;
            }

            // Prefer unblocked types so order is easily fillable
            var unblockedEligible = _boardTileProvider.GetUnblockedTileCounts()
                .Where(kvp => kvp.Value >= 1 && eligible.Contains(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();
            
            var pool = unblockedEligible.Count > 0 ? unblockedEligible : eligible;
            var chosen = pool[Random.Range(0, pool.Count)];
            
            if (_activeOrder != null)
            {
                _activeOrder.OnOrderChanged -= HandleOrderChanged;
                _activeOrder.OnOrderCompleted -= HandleOrderCompleted;
            }

            _activeOrder = new OrderModel(chosen);
            _activeOrder.OnOrderChanged += HandleOrderChanged;
            _activeOrder.OnOrderCompleted += HandleOrderCompleted;

            OnNewOrder?.Invoke(_activeOrder);
        }

        private void HandleOrderChanged(OrderModel order) => OnOrderChanged?.Invoke(order);

        private void HandleOrderCompleted(OrderModel order)
        {
            GenerateNextOrder();
        }
    }
}