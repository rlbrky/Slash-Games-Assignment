using System;
using Data;
using DG.Tweening;
using Models;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private float _flyDuration = 0.5f;
        [SerializeField] private float _spawnDuration = 0.35f;
        [SerializeField] private float _blockedFadeDuration = 0.2f;
        [SerializeField] private float _flyScaleUpAmount = 1.25f;
        [SerializeField] private float _flyScaleUpDurationRatio = 0.2f;
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
            
            _rect.DOScale(Vector3.one * _flyScaleUpAmount, _flyDuration * _flyScaleUpDurationRatio)
                .OnComplete(() =>
                {
                    _rect.DOMove(targetWorldPos, _flyDuration)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            _isAnimating = false;
                            onComplete?.Invoke();
                        });
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
            Vector3 targetScale = Vector3.one * (1f + _rect.localScale.x * 0.04f);
            _rect.localScale = Vector3.zero;
            transform.DOScale(targetScale, _spawnDuration)
                .SetEase(Ease.OutBack);
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
