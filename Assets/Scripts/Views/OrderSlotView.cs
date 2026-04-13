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

        [Header("Complete Animation")]
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _flashDuration = 0.05f;
        [SerializeField] private float _punchUpScale = 1.35f;
        [SerializeField] private float _punchUpDuration = 0.1f;
        [SerializeField] private float _settleScale = 1.1f;
        [SerializeField] private float _settleDuration = 0.08f;
        [SerializeField] private float _shrinkDuration = 0.15f;
        [SerializeField] private float _shrinkEaseOvershoot = 0.2f;
        
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
            Color backgroundColor = _backgroundImage.color;
            
            // Flash white
            sequence.Append(_backgroundImage.DOColor(_flashColor, _flashDuration));
            
            // Punch scale with overshoot
            sequence.Append(transform.DOScale(Vector3.one * _punchUpScale, _punchUpDuration)
                .SetEase(Ease.OutQuad));
            sequence.Append(transform.DOScale(Vector3.one * _settleScale, _settleDuration)
                .SetEase(Ease.InOutQuad));
    
            // Shrink and fade out
            sequence.Append(transform.DOScale(Vector3.zero, _shrinkDuration).SetEase(Ease.InBack, _shrinkEaseOvershoot));
            sequence.Join(_backgroundImage.DOFade(0f, _shrinkDuration));
            
            sequence.OnComplete(() =>
            {
                transform.localScale = Vector3.one;
                _backgroundImage.color = backgroundColor;
                SetEmpty();
                onComplete?.Invoke();
            });
        }
    }
}