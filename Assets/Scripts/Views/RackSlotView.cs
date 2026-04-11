using Data;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    public class RackSlotView : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Image _backgroundImage;

        [SerializeField] private Image _iconImage;

        [Header("Colors")] [SerializeField] private Color _emptyColor = Color.grey;

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
    }
}