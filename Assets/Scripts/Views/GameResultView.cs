using Controllers;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Views
{
    public class GameResultView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subTitleText;
        [SerializeField] private Button _restartButton;
        
        [Header("Win Settings")]
        [SerializeField] private string _winTitle = "You Win!";
        [SerializeField] private string _winSubTitle = "All tiles cleared!";
        [SerializeField] private Color _winColor = Color.green;
        
        [Header("Lose Settings")]
        [SerializeField] private string _loseTitle = "You Lose!";
        [SerializeField] private string _loseSubTitle = "The rack is full!";
        [SerializeField] private Color _loseColor = Color.red;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.4f;
        [SerializeField] private float _punchScale = 0.2f;
        [SerializeField] private float _punchDuration = 0.5f;

        private void Awake()
        {
            Hide(true);
            _restartButton.onClick.AddListener(HandleRestart);
        }

        public void BindToGameStateManager(GameStateManager gameStateManager)
        {
            gameStateManager.OnGameWon += ShowWin;
            gameStateManager.OnGameLost += ShowLose;
        }

        public void ShowWin()
        {
            _titleText.text = _winTitle;
            _subTitleText.text = _winSubTitle;
            _titleText.color = _winColor;
            Show();
        }

        private void ShowLose()
        {
            _titleText.text = _loseTitle;
            _subTitleText.text = _loseSubTitle;
            _titleText.color = _loseColor;
            Show();
        }

        private void Show()
        {
            gameObject.SetActive(true);
            
            _canvasGroup.alpha = 1f;
            _panel.localScale = Vector3.one * 0.8f;

            _canvasGroup.DOFade(1f, _fadeDuration);
            _panel.DOScale(Vector3.one, _fadeDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _panel.DOPunchScale(Vector3.one * _punchScale, _punchDuration);
                });
        }

        private void Hide(bool instant = false)
        {
            if (instant)
            {
                _canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                return;
            }
            
            _canvasGroup.DOFade(0f, _fadeDuration)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void HandleRestart()
        {
            Hide();
            DOVirtual.DelayedCall(_fadeDuration, () =>
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        }

        private void OnDestroy()
        {
            _restartButton.onClick.RemoveAllListeners();
        }
    }
}
