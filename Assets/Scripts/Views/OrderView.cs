using Controllers;
using Models;
using UnityEngine;

namespace Views
{
    public class OrderView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OrderSlotView _slot1;
        [SerializeField] private OrderSlotView _slot2;
        [SerializeField] private OrderSlotView _slot3;
        [SerializeField] private TileDefinitionRegistry _registry;

        private OrderSlotView[] _slots;

        private void Awake()
        {
            _slots = new[] { _slot1, _slot2, _slot3 };
        }

        public void BindToSystem(OrderSystem orderSystem)
        {
            orderSystem.OnNewOrder += HandleNewOrder;
            orderSystem.OnOrderChanged += HandleOrderChanged;
        }

        private void HandleNewOrder(OrderModel orderModel)
        {
            RefreshAll(orderModel);
        }

        private void HandleOrderChanged(OrderModel orderModel)
        {
            RefreshAll(orderModel);
        }

        private void RefreshAll(OrderModel orderModel)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (i < orderModel.Requirements.Count)
                {
                    var req = orderModel.Requirements[i];
                    var definition = _registry.Get(req.tileType);

                    if (definition != null)
                        _slots[i].SetRequirement(definition, req.isFulfilled);
                    else
                        _slots[i].SetEmpty();
                }
                else
                {
                    _slots[i].SetEmpty();
                }
            }
        }

        #region Helpers

        public Vector3 GetSlotWorldPosition(int index)
        {
            if(index < 0 || index >= _slots.Length)
                return Vector3.zero;
            
            return _slots[index].transform.position;
        }

        public int GetFirstUnfulfilledSlotIndex(OrderModel order)
        {
            for (int i = 0; i < order.Requirements.Count; i++)
            {
                if (!order.Requirements[i].isFulfilled)
                    return i;
            }

            return 0;
        }

        #endregion
    }
}