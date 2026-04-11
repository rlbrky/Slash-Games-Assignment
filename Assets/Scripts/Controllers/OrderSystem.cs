using System;
using System.Collections.Generic;
using Data;
using Models;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Controllers
{
    public class OrderSystem : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private TileDefinitionRegistry _registry;

        private readonly List<TileType> _allTileTypes = new();

        private OrderModel _activeOrder;

        public OrderModel ActiveOrder => _activeOrder;

        private void Awake()
        {
            _registry.Initialize();
            CacheAvailableTileTypes();
        }

        public event Action<OrderModel> OnNewOrder;
        public event Action<OrderModel> OnOrderChanged;
        public event Action OnAllOrdersComplete;

        private void CacheAvailableTileTypes()
        {
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                if (type == TileType.None)
                    continue;
                _allTileTypes.Add(type);
            }
        }

        public void StartFirstOrder()
        {
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
            var req1 = GetRandomTileType();
            var req2 = GetRandomTileType();
            var req3 = GetRandomTileType();

            _activeOrder = new OrderModel(req1, req2, req3);
            _activeOrder.OnOrderChanged += HandleOrderChanged;
            _activeOrder.OnOrderCompleted += HandleOrderCompleted;

            OnNewOrder?.Invoke(_activeOrder);
        }

        private void HandleOrderChanged(OrderModel order)
        {
            OnOrderChanged?.Invoke(order);
        }

        private void HandleOrderCompleted(OrderModel order)
        {
            Debug.Log("Order completed! Generating next order.");
            GenerateNextOrder();
        }

        private TileType GetRandomTileType()
        {
            return _allTileTypes[Random.Range(0, _allTileTypes.Count)];
        }
    }
}