using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class ScreenManager : MonoBehaviour
    {
        public List<GameScreen> screens = new List<GameScreen>();

        public void Init()
        {
            //Show(firstScreen);
        }

        public void Show(GameScreen gameScreen)
        {
            screens.ForEach(screen => screen.gameObject.SetActive(false));
            ShowScreen(gameScreen);
        }

        public void Show(ScreenID screenID)
        {
            screens.ForEach(screen => screen.gameObject.SetActive(false));
            GameScreen gameScreen = screens.Find(screen => screen.PanelID == screenID);
            ShowScreen(gameScreen);
        }
        public void CloseAllScreens()
        {
            screens.ForEach(screen => screen.gameObject.SetActive(false));
        }

        private static void ShowScreen(GameScreen gameScreen)
        {
            gameScreen.gameObject.SetActive(true);
            gameScreen.Show();
        }
    }

}