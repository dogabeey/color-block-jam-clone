using UnityEngine.UI;

namespace Game
{
    public class LoseScreen : GameScreen
    {
        public override ScreenID PanelID => ScreenID.LoseScreen;

        public Button retryButton;

        private void Start()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(() =>
                {
                    GameManager.Instance.LoadLevel();
                });
            }
        }

        public override void Show()
        {
            if (animator != null && !string.IsNullOrEmpty(playAnimationName))
            {
                animator.Play(playAnimationName);
            }
        }
}