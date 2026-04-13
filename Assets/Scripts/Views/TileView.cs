using System;
using Data;
using DG.Tweening;
using Models;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Views
{
    public class TileView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _blockedOverlay;
        [SerializeField] private Button _button;

        [Header("Blocked State")]
        [SerializeField] private Color _blockedTint = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color _normalTint = Color.white;

        [Header("Animation")]
        [SerializeField] private float _blockedFadeDuration = 0.2f;
        
        [Header("Fly Animation")]
        [SerializeField] private float _flyDuration = 0.5f;
        [SerializeField] private float _flyAnticipationDuration = 0.08f;
        [SerializeField] private float _flyAnticipationScale = 0.9f;
        [SerializeField] private Vector3 _flyAnticipationShake = new Vector3(0, 0, 5f);
        [SerializeField] private float _flySquashScale = 0.85f;
        [SerializeField] private float _flySquashDuration = 0.1f;
        [SerializeField] private float _flyTravelDurationRatio = 0.8f;
        [SerializeField] private float _flyEndScale = 0.6f;
        [SerializeField] private float _flyRotationDegrees = 360f;
        
        [Header("Spawn Animation")]
        [SerializeField] private float _spawnDuration = 0.35f;
        [SerializeField] private float _spawnOffsetRange = 50f;
        [SerializeField] private float _spawnRotationRange = 15f;
        
        private bool _isAnimating;
        public bool IsAnimating => _isAnimating;
        public RectTransform Rect => _rect;
        
        private TileModel _model;
        private RectTransform _rect;
        
        public event Action<TileModel> OnTileClicked;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void Initialize(TileModel model, TileDefinition definition)
        {
            _model = model;
            _iconImage.sprite = definition.Icon;
            _backgroundImage.color = definition.TileColor;
            
            _button.onClick.AddListener(HandleClick);
            _model.OnBlockedStateChanged += HandleBlockedChanged;

            RefreshBlockedVisual();
        }

        private void HandleClick()
        {
            if(_model.IsBlocked) 
                return;
            
            OnTileClicked?.Invoke(_model);
        }

        private void HandleBlockedChanged(TileModel model)
        {
            RefreshBlockedVisual(true);
        }

        private void RefreshBlockedVisual(bool animate = false)
        {
            bool blocked = _model.IsBlocked;

            if (animate)
            {
                AnimateBlockedChange(blocked);
                return;
            }
            
            _blockedOverlay.gameObject.SetActive(blocked);
            _button.interactable = !blocked;
        }

        #region Animation
        
        public void AnimateFlyTo(Vector2 targetWorldPos, Action onComplete)
        {
            _isAnimating = true;
            _button.interactable = false;
            transform.SetAsLastSibling();
            
            Sequence sequence = DOTween.Sequence();
            
            // Anticipation
            sequence.Append(_rect.DOScale(Vector3.one * _flyAnticipationScale, _flyAnticipationDuration).SetEase(Ease.InQuad));
            sequence.Join(_rect.DOShakeRotation(_flyAnticipationDuration, _flyAnticipationShake, 10, 90, false));
            
            // Squash horizontally as it launches
            sequence.Append(_rect.DOScale(_flySquashScale, _flySquashDuration).SetEase(Ease.OutQuad));
            
            // Fly with rotation for better feel
            float travelDuration = _flyDuration * _flyTravelDurationRatio;
            sequence.Append(_rect.DOMove(targetWorldPos, travelDuration).SetEase(Ease.InOutQuad));
            sequence.Join(_rect.DOScale(Vector3.one * _flyEndScale, travelDuration).SetEase(Ease.InQuad));
            sequence.Join(_rect.DORotate(new Vector3(0, 0, _flyRotationDegrees), travelDuration, RotateMode.FastBeyond360));

            sequence.OnComplete(() =>
            {
                _isAnimating = false;
                onComplete?.Invoke();
            });
        }

        public void AnimateBlockedChange(bool blocked)
        {
            _button.interactable = !blocked;

            if (blocked)
            {
                _blockedOverlay.gameObject.SetActive(true);
                Color color = _blockedOverlay.color;
                _blockedOverlay.color = new Color(color.r, color.g, color.b, 0f);
                _blockedOverlay.DOFade(color.a, _blockedFadeDuration);
            }
            else
            {
                _blockedOverlay.DOFade(0f, _blockedFadeDuration)
                    .OnComplete(() => _blockedOverlay.gameObject.SetActive(false));
            }
        }

        public void AnimatePunchOnSpawn()
        {
            Vector3 targetScale = _rect.localScale;
            Vector3 randomOffset = new Vector3(
                Random.Range(-_spawnOffsetRange, _spawnOffsetRange),
                Random.Range(-_spawnOffsetRange, _spawnOffsetRange),
                0f
            );

            Vector2 startPos = _rect.anchoredPosition;
            _rect.anchoredPosition = startPos + (Vector2)randomOffset;
            _rect.localScale = Vector3.zero;
            _rect.localRotation = Quaternion.Euler(0, 0, Random.Range(-_spawnRotationRange, _spawnRotationRange));
            
            Sequence sequence = DOTween.Sequence();
            sequence.Append(_rect.DOScale(targetScale, _spawnDuration).SetEase(Ease.OutBack, 1.5f));
            sequence.Join(_rect.DOAnchorPos(startPos, _spawnDuration).SetEase(Ease.OutBack, 1.2f));
            sequence.Join(_rect.DORotate(Vector3.zero, _spawnDuration).SetEase(Ease.OutBack));
        }
        
        #endregion
        
        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
            
            if(_model != null)
                _model.OnBlockedStateChanged -= HandleBlockedChanged;
        }
    }
}
