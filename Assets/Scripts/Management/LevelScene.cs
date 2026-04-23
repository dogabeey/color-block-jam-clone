using UnityEngine;

namespace Game.Management
{
    public class LevelScene : MonoBehaviour
    {
        public LevelData levelData;
        public GameEvent winTrigger, loseTrigger;

        private bool isWin, isLose, isEnded;

        private void OnEnable()
        {
            EventManager.StartListening(winTrigger, OnWinTriggered);
            EventManager.StartListening(loseTrigger, OnLoseTriggered);
        }
        private void OnDisable()
        {
            EventManager.StopListening(winTrigger, OnWinTriggered);
            EventManager.StopListening(loseTrigger, OnLoseTriggered);
        }
        private void OnWinTriggered(EventParam param)
        {
            Win();
        }
        private void OnLoseTriggered(EventParam param)
        {
            Lose();
        }

        private void Update()
        {
            if(isEnded) return;

            if(isWin)
            {
                isEnded = true;
                GameManager.Instance.screenManager.Show(ScreenID.WinScreen);
            }
            else if(isLose)
            {
                isEnded = true;
                GameManager.Instance.screenManager.Show(ScreenID.LoseScreen);
            }
        }

        public void Win()
        {
            isWin = true;
        }

        public void Lose()
        {
            isLose = true;
        }
    }
}
