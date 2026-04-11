using System;
using System.Collections.Generic;
using Data;

namespace Models
{
    public class OrderModel
    {
        private readonly List<OrderRequirement> _requirements;

        public OrderModel(TileType req1, TileType req2, TileType req3)
        {
            _requirements = new List<OrderRequirement>
            {
                new OrderRequirement(req1),
                new OrderRequirement(req2),
                new OrderRequirement(req3)
            };
        }

        public IReadOnlyList<OrderRequirement> Requirements => _requirements;
        public bool IsComplete => _requirements.TrueForAll(r => r.isFulfilled);

        public event Action<OrderModel> OnOrderChanged;
        public event Action<OrderModel> OnOrderCompleted;

        /// <summary>
        /// Returns true if type matches a requirement.
        /// Only the first unfulfilled slot of that type gets consumed if there is more than one.
        /// </summary>
        public bool TryFulfill(TileType type)
        {
            var requirement = _requirements.Find(r => r.tileType == type &&
                                                      !r.isFulfilled
            );
            if (requirement == null)
                return false;

            requirement.isFulfilled = true;
            OnOrderChanged?.Invoke(this);

            if (IsComplete)
                OnOrderCompleted?.Invoke(this);

            return true;
        }
    }
}