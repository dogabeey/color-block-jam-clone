using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game
{
    public abstract class GameScreen : SerializedMonoBehaviour
    {
        public abstract ScreenID PanelID { get; }

        public Animator animator;
        public string playAnimationName;

        private void OnValidate()
        {
            if (animator == null)
            {
                TryGetComponent(out animator);
            }
        }

        protected virtual void Awake()
        {

        }
        private void OnDestroy()
        {
            GameManager.Instance.screenManager.screens.Remove(this);
        }

        public abstract void Show();
    }

    public enum ScreenID
    {
        WinScreen,
        LoseScreen
    }
}