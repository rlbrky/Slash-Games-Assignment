using System;
using Controllers;
using DG.Tweening;
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
        private OrderSystem _orderSystem;

        private bool _isCompletionAnimating;

        private void Awake()
        {
            _slots = new[] { _slot1, _slot2, _slot3 };
        }

        public void BindToSystem(OrderSystem orderSystem)
        {
            _orderSystem = orderSystem;
            orderSystem.OnNewOrder += HandleNewOrder;
            orderSystem.OnOrderChanged += HandleOrderChanged;
            orderSystem.OnOrderCompleted += HandleOrderCompleted;
        }

        private void HandleNewOrder(OrderModel orderModel)
        {
            RefreshAll(orderModel);
        }

        private void HandleOrderChanged(OrderModel orderModel)
        {
            // Blocks refresh if completion animation is still in progress.
            if(_isCompletionAnimating)
                return;
            
            RefreshAll(orderModel);
        }

        private void HandleOrderCompleted(OrderModel orderModel)
        {
            _isCompletionAnimating = true;
            AnimateOrderComplete(() =>
            {
                _isCompletionAnimating = false;
                _orderSystem.ReadyForNextOrder();
            });
        }

        public void AnimateOrderComplete(Action onComplete)
        {
            int completedCount = 0;
            int totalSlots = _slots.Length;
            

            for (int i = 0; i < _slots.Length; i++)
            {
                // Since i is modified out of the scope we need to capture it inside this scope.
                int index = i;
                float delay = i * 0.1f;

                DOVirtual.DelayedCall(delay, () =>
                {
                    _slots[index].AnimateComplete(() =>
                    {
                        completedCount++;
                        if (completedCount >= totalSlots)
                            onComplete?.Invoke();
                    });
                });
            }
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