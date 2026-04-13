using System;
using Data;
using Models;
using UnityEngine;

namespace Controllers
{
    public class RackSystem : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private int _rackCapacity = 6;

        private RackModel _model;

        public RackModel Model => _model;
        
        public event Action<RackModel> OnRackChanged;
        public event Action OnRackFull;

        private void Awake()
        {
            _model = new RackModel(_rackCapacity);
            _model.OnRackChanged += HandleRackChanged;
            _model.OnRackFull += HandleRackFull;
        }

        private void OnDestroy()
        {
            _model.OnRackChanged -= HandleRackChanged;
            _model.OnRackFull -= HandleRackFull;
        }
        
        public bool TryAddTile(TileType tileType)
        {
            return _model.TryAddTile(tileType);
        }

        public void CheckFullAfterAnimation()
        {
            _model.NotifyIfNull();
        }

        private void HandleRackChanged(RackModel model) => OnRackChanged?.Invoke(model);
        private void HandleRackFull() => OnRackFull?.Invoke();
    }
}