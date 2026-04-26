using UnityEngine.UI;

namespace Game
{
    public class WinScreen : GameScreen
    {
        public override ScreenID PanelID => ScreenID.WinScreen;

        public Button nextLevelButton;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(() =>
                {
                    GameManager.Instance.LoadNextLevel();
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
}