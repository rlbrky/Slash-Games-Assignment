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
        [SerializeField] private Color _filledColor = Color.white;
        
        [Header("Drain Animation")]
        [SerializeField] private float _drainAnimationY = 40f;
        [SerializeField] private float _drainAnticipationDuration = 0.06f;
        [SerializeField] private Vector3 _drainAnticipationScale = new Vector3(1.1f, 0.9f, 1f);
        [SerializeField] private float _drainRiseDuration = 0.2f;
        [SerializeField] private Vector3 _drainStretchScale = new Vector3(0.9f, 1.15f, 1f);
        [SerializeField] private float _drainStretchDuration = 0.1f;
        [SerializeField] private float _drainSettleDuration = 0.1f;

        private Sequence _drainSequence;
        
        public void SetTile(TileDefinition definition)
        {
            _drainSequence?.Kill();
            _backgroundImage.DOKill();
            _iconImage.DOKill();
            transform.DOKill();

            _iconImage.transform.localPosition = Vector3.zero;
            _iconImage.color = new Color(_iconImage.color.r, _iconImage.color.g, _iconImage.color.b, 1f);
            transform.localScale = Vector3.one;
            
            _iconImage.sprite = definition.Icon;
            _iconImage.gameObject.SetActive(true);
            
            _backgroundImage.color = definition.TileColor;
        }

        public void SetEmpty()
        {
            _drainSequence?.Kill();
            _backgroundImage.DOKill();
            _iconImage.DOKill();
            transform.DOKill();
            
            _iconImage.transform.localPosition = Vector3.zero;
            _iconImage.color = new Color(_iconImage.color.r, _iconImage.color.g, _iconImage.color.b, 1f);
            transform.localScale = Vector3.one;
            
            _iconImage.sprite = null;
            _iconImage.gameObject.SetActive(false);
            
            _backgroundImage.color = _emptyColor;
        }

        public void AnimateDrained(Action onComplete)
        {
            _drainSequence?.Kill();
            _drainSequence = DOTween.Sequence();

            // Anticipation
            _drainSequence.Append(transform.DOScale(_drainAnticipationScale, _drainAnticipationDuration));
            
            // Pop up with stretch
            _drainSequence.Append(_iconImage.transform.DOLocalMoveY(
                    _iconImage.transform.localPosition.y + _drainAnimationY, _drainRiseDuration)
                .SetEase(Ease.OutQuad));
            _drainSequence.Join(transform.DOScale(_drainStretchScale, _drainStretchDuration));
            _drainSequence.Join(_iconImage.DOFade(0f, _drainRiseDuration));
            _drainSequence.Join(_backgroundImage.DOColor(Color.green, _drainRiseDuration * 0.5f));

            // Settle back
            _drainSequence.Append(transform.DOScale(Vector3.one, _drainSettleDuration).SetEase(Ease.OutBounce));

            _drainSequence.OnComplete(() =>
            {
                _iconImage.transform.localPosition = Vector3.zero;
                var color = _iconImage.color;
                _iconImage.color = new Color(color.r, color.g, color.b, 1f);
                _backgroundImage.color = _emptyColor;
                _drainSequence = null;
                onComplete?.Invoke();
            });
        }
    }
}