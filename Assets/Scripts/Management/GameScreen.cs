using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public abstract class GameScreen : SerializedMonoBehaviour
    {
        public ScreenID panelID;
        public Animator animator;
        public string playAnimationName;

        private void OnValidate()
        {
            if (animator == null)
            {
                TryGetComponent(out animator);
            }
        }

        public abstract void Show();
    }

    public enum ScreenID
    {
        WinScreen,
        LoseScreen
    }
}