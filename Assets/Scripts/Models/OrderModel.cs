using System;
using System.Collections.Generic;
using Data;

namespace Models
{
    public class OrderModel
    {
        public IReadOnlyList<OrderRequirement> Requirements => _requirements;
        public TileType RequiredType { get; private set; }
        public bool IsComplete => _requirements.TrueForAll(r => r.isFulfilled);
        
        private readonly List<OrderRequirement> _requirements;
        
        public event Action<OrderModel> OnOrderChanged;
        public event Action<OrderModel> OnOrderCompleted;
        
        /// <summary>
        /// Constructor creates the order using the tile type 
        /// </summary>
        public OrderModel(TileType tileType)
        {
            RequiredType = tileType;
            _requirements = new List<OrderRequirement>
            {
                new OrderRequirement(tileType),
                new OrderRequirement(tileType),
                new OrderRequirement(tileType)
            };
        }
        
        /// <summary>
        /// Returns true if type matches a requirement.
        /// Only the first unfulfilled slot of that type gets consumed if there is more than one.
        /// </summary>
        public bool TryFulfill(TileType type)
        {
            // Must match required type and fill sequentially
            if (type != RequiredType) return false;

            var next = _requirements.Find(r => !r.isFulfilled);
            if (next == null) return false;

            next.isFulfilled = true;
            OnOrderChanged?.Invoke(this);

            if (IsComplete)
                OnOrderCompleted?.Invoke(this);

            return true;
        }
    }
}