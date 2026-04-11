using Data;
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
        [SerializeField] private Color _pendingColor = Color.white;
        [SerializeField] private Color _fulfilledColor = Color.green;

        public void SetRequirement(TileDefinition definition, bool isFulfilled)
        {
            _iconImage.sprite = definition.Icon;
            _backgroundImage.color = definition.TileColor;
            SetFulfilled(isFulfilled);
        }

        public void SetFulfilled(bool isFulfilled)
        {
            _fulfilledOverlay.SetActive(isFulfilled);
            _backgroundImage.color = isFulfilled ? _fulfilledColor : _pendingColor;
        }

        public void SetEmpty()
        {
            _iconImage.sprite = null;
            _backgroundImage.color = _pendingColor;
            _fulfilledOverlay.SetActive(false);
        }
    }
}