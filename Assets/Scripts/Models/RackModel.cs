using System;
using System.Collections.Generic;
using Data;

namespace Models
{
    public class RackModel
    {
        private readonly List<TileType> _slots = new();

        public RackModel(int capacity)
        {
            Capacity = capacity;
        }

        public IReadOnlyList<TileType> Slots => _slots;
        public int Capacity { get; private set; }
        public bool IsFull => _slots.Count >= Capacity;

        public event Action<RackModel> OnRackChanged;
        public event Action<TileType> OnThreeMatched;
        public event Action OnRackFull;

        public bool TryAddTile(TileType tileType)
        {
            if (IsFull)
                return false;

            _slots.Add(tileType);
            OnRackChanged?.Invoke(this);

            if (IsFull)
                OnRackFull?.Invoke();

            return true;
        }
    }
}