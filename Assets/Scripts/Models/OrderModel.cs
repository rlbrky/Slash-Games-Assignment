using System;
using System.Collections.Generic;
using System.Linq;
using Data;

namespace Models
{
    public class OrderModel
    {
        private readonly List<OrderRequirement> _requirements;

        /// <summary>
        /// Constructor creates the order requirement list from passed tile type list 
        /// </summary>
        public OrderModel(List<TileType> requirements)
        {
            _requirements = requirements.Select(t => new OrderRequirement(t)).ToList();
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