using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Data;
using Models;
using UnityEngine;

namespace Controllers
{
    public class OrderSystem : MonoBehaviour
    {
        private const int OrderSize = 3;
        
        private RackSystem _rackSystem;
        private OrderModel _activeOrder;
        private IBoardTileProvider _boardTileProvider;
        private List<TileType> _orderSequence = new();
        private int _orderIndex;

        public OrderModel ActiveOrder => _activeOrder;

        public event Action<OrderModel> OnNewOrder;
        public event Action<OrderModel> OnOrderChanged;
        public event Action<OrderModel> OnOrderCompleted;
        public event Action<int> OnRackTileDrained;
        
        public void Initialize(IBoardTileProvider boardTileProvider, RackSystem rackSystem, IReadOnlyList<TileType> orderSequence)
        {
            _boardTileProvider = boardTileProvider;
            _rackSystem = rackSystem;
            _orderSequence = new List<TileType>(orderSequence);
            _orderIndex = 0;
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

        public void DrainRackIntoOrder()
        {
            if(_activeOrder == null || _rackSystem == null)
                return;

            bool anyFulfilled = true;
            
            // Loop just in case fulfilling one slot exposes the next slot which
            // could be also filled by another tile in the rack.
            while (anyFulfilled)
            {
                anyFulfilled = false;

                OrderRequirement next = _activeOrder.Requirements.FirstOrDefault(r => !r.isFulfilled);
                if (next == null)
                    break;

                if (_rackSystem.TryConsumeTile(next.tileType, out int slotIndex))
                {
                    OnRackTileDrained?.Invoke(slotIndex);
                    _activeOrder.TryFulfill(next.tileType);
                    anyFulfilled = true;
                }
            }
        }

        private void GenerateNextOrder()
        {
            if (_orderIndex >= _orderSequence.Count)
            {
                Debug.Log("All pre-computed orders exhausted — level complete.");
                return;
            }

            var chosen = _orderSequence[_orderIndex++];

            if (_activeOrder != null)
            {
                _activeOrder.OnOrderChanged -= HandleOrderChanged;
                _activeOrder.OnOrderCompleted -= HandleOrderCompleted;
            }

            _activeOrder = new OrderModel(chosen);
            _activeOrder.OnOrderChanged += HandleOrderChanged;
            _activeOrder.OnOrderCompleted += HandleOrderCompleted;

            OnNewOrder?.Invoke(_activeOrder);
            DrainRackIntoOrder();
        }

        private void HandleOrderChanged(OrderModel order) => OnOrderChanged?.Invoke(order);

        private void HandleOrderCompleted(OrderModel order)
        {
            OnOrderCompleted?.Invoke(order);
        }

        public void ReadyForNextOrder()
        {
            GenerateNextOrder();
        }
    }
}