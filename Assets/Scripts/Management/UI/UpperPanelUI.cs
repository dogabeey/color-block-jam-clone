using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class UpperPanelUI : UIBehaviour
    {
        public TMP_Text levelText;
        public TMP_Text timerText;
        public Button restartButton;
        [Header("Config")]
        public string timerTextFormat = "{0:00}:{1:00}";

        private void Start()
        {
            restartButton.onClick.AddListener(() =>
            {
                GameManager.Instance.LoadLevel();
            });
        }
        public override void UpdateUI()
        {
            levelText.text = (GameManager.Instance.CurrentLevelIndex + 1).ToString();
            timerText.text = string.Format(timerTextFormat, GameManager.Instance.currentLoadedLevel.remainingTime / 60, GameManager.Instance.currentLoadedLevel.remainingTime % 60);

        }
    }
}
