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
        
        [Header("Drain Animation")]
        [SerializeField] private float _drainAnimationY = 40f;
        [SerializeField] private float _drainAnticipationDuration = 0.06f;
        [SerializeField] private Vector3 _drainAnticipationScale = new Vector3(1.1f, 0.9f, 1f);
        [SerializeField] private float _drainRiseDuration = 0.2f;
        [SerializeField] private Vector3 _drainStretchScale = new Vector3(0.9f, 1.15f, 1f);
        [SerializeField] private float _drainStretchDuration = 0.1f;
        [SerializeField] private float _drainSettleDuration = 0.1f;

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

            // Anticipation
            sequence.Append(transform.DOScale(_drainAnticipationScale, _drainAnticipationDuration));
            
            // Pop up with stretch
            sequence.Append(_iconImage.transform.DOLocalMoveY(
                    _iconImage.transform.localPosition.y + _drainAnimationY, _drainRiseDuration)
                .SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(_drainStretchScale, _drainStretchDuration));
            sequence.Join(_iconImage.DOFade(0f, _drainRiseDuration));
            sequence.Join(_backgroundImage.DOColor(Color.green, _drainRiseDuration * 0.5f));

            // Settle back
            sequence.Append(transform.DOScale(Vector3.one, _drainSettleDuration).SetEase(Ease.OutBounce));

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