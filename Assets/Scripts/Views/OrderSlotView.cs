using System;
using Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class OrderSlotView : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _fulfilledOverlay;

        [Header("Colors")]
        [SerializeField] private Color _fulfilledColor = Color.green;
        
        [Header("Animation")]
        [SerializeField] private float _completeScaleUpDuration = 0.15f;
        [SerializeField] private float _completeScaleDownDuration = 0.2f;

        public void SetRequirement(TileDefinition definition, bool isFulfilled)
        {
            // Kill any running color tweens before assigning new color
            _backgroundImage.DOKill();
            _iconImage.DOKill();
            
            _iconImage.gameObject.SetActive(true);
            
            _iconImage.sprite = definition.Icon;
            _backgroundImage.color = definition.TileColor;
            SetFulfilled(isFulfilled);
        }

        public void SetFulfilled(bool isFulfilled)
        {
            _fulfilledOverlay.SetActive(isFulfilled);
            _backgroundImage.color = isFulfilled ? _fulfilledColor : Color.white;
        }

        public void SetEmpty()
        {
            _iconImage.gameObject.SetActive(false);
        }
        
        public void AnimateComplete(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            
            // Make it 25% bigger
            sequence.Append(transform.DOScale(Vector3.one * 1.25f, _completeScaleUpDuration)
                .SetEase(Ease.OutBack));
            sequence.Append(transform.DOScale(Vector3.zero, _completeScaleDownDuration)
                .SetEase(Ease.InBack));
            sequence.OnComplete(() =>
            {
                transform.localScale = Vector3.one;
                SetEmpty();
                onComplete?.Invoke();
            });
        }
    }
}