using System;
using Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class RackSlotView : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Image _backgroundImage;

        [SerializeField] private Image _iconImage;

        [Header("Colors")] 
        [SerializeField] private Color _emptyColor = Color.grey;
        
        [Header("Animation")] 
        [SerializeField] private float _drainAnimationY = 40f;
        [SerializeField] private float _drainAnimationDuration = 0.2f;
        [SerializeField] private float _clearAnimationDuration = 0.25f;

        public void SetTile(TileDefinition definition)
        {
            _iconImage.sprite = definition.Icon;
            _iconImage.gameObject.SetActive(true);
            _backgroundImage.color = definition.TileColor;
        }

        public void SetEmpty()
        {
            _iconImage.sprite = null;
            _iconImage.gameObject.SetActive(false);
            _backgroundImage.color = _emptyColor;
        }

        public void AnimateDrained(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();

            sequence.Append(_iconImage.transform.DOLocalMoveY(
                    _iconImage.transform.localPosition.y + _drainAnimationY, _drainAnimationDuration)
                .SetEase(Ease.OutQuad));
            sequence.Join(_iconImage.DOFade(0f, _drainAnimationDuration));
            sequence.Join(_backgroundImage.DOColor(Color.green, _drainAnimationDuration));
            sequence.OnComplete(() =>
            {
                _iconImage.transform.localPosition = Vector3.zero;
                var color = _iconImage.color;
                _iconImage.color = new Color(color.r, color.g, color.b, 1f);
                SetEmpty();
                onComplete?.Invoke();
            });
        }
    }
}