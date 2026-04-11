using System;
using Data;
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
        
        private TileModel _model;

        public event Action<TileModel> OnTileClicked;

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
            if(_model.IsBlocked) return;
            OnTileClicked?.Invoke(_model);
        }

        private void HandleBlockedChanged(TileModel model)
        {
            RefreshBlockedVisual();
        }

        private void RefreshBlockedVisual()
        {
            bool blocked = _model.IsBlocked;
            _blockedOverlay.gameObject.SetActive(blocked);
            
            _iconImage.color = blocked ? _blockedTint : _normalTint;
            _button.interactable = !blocked;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
            
            if(_model != null)
                _model.OnBlockedStateChanged -= HandleBlockedChanged;
        }
    }
}
