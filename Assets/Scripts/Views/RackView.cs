using System.Collections.Generic;
using Controllers;
using Models;
using UnityEngine;

namespace Views
{
    public class RackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private List<RackSlotView> _slots;
        [SerializeField] private TileDefinitionRegistry _registry;

        public void BindToSystem(RackSystem rackSystem)
        {
            rackSystem.OnRackChanged += HandleRackChanged;
            RefreshAll(rackSystem.Model);
        }

        public void HandleRackChanged(RackModel rackModel)
        {
            RefreshAll(rackModel);
        }

        private void RefreshAll(RackModel rackModel)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < rackModel.Slots.Count)
                {
                    var definition = _registry.Get(rackModel.Slots[i]);
                    if (definition != null)
                        _slots[i].SetTile(definition);
                }
                else
                {
                    _slots[i].SetEmpty();
                }
            }
        }
        
        public Vector3 GetNextEmptySlotWorldPosition(RackModel model)
        {
            int nextIndex = model.Slots.Count;
            
            if (nextIndex >= _slots.Count)
                return Vector3.zero;

            return _slots[nextIndex].transform.position;
        }
    }
}