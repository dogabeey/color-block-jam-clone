using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class ScreenManager : MonoBehaviour
    {
        internal List<GameScreen> screens = new List<GameScreen>();

        public void Init()
        {
            screens.AddRange(FindObjectsByType<GameScreen>(FindObjectsSortMode.None));

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
            GameScreen gameScreen = screens.Find(screen => screen.panelID == screenID);
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