using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("GameManager");
                        instance = obj.AddComponent<GameManager>();
                    }
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        [FoldoutGroup("Managers"), InlineEditor]
        public EventManager eventManager;
        [FoldoutGroup("Managers"), InlineEditor]
        public ScreenManager screenManager;
        [FoldoutGroup("Managers"), InlineEditor]
        public ConstantManager constantManager;

        public List<LevelScene> levelScenes;
        public List<ElementData> elementData;
        public int startingLevelIndex = 0;

        private LevelScene currentLoadedLevel;

        public int CurrentLevelIndex
        {
            get => PlayerPrefs.GetInt("CurrentLevelIndex", startingLevelIndex);
            set => PlayerPrefs.SetInt("CurrentLevelIndex", value);
        }

        private void Start()
        {
            Instance = this;
            InitManagers();


            LoadLevel();
        }

        private void InitManagers()
        {
            screenManager.Init();
        }

        public void LoadLevel()
        {
            if (levelScenes.Count == 0)
            {
                Debug.LogError("No level scenes assigned in GameManager.", this);
                return;
            }
            int levelIndex = CurrentLevelIndex % levelScenes.Count;
            LevelScene levelScene = levelScenes[levelIndex];
            currentLoadedLevel = Instantiate(levelScene);
        }

        public static void WinLevel()
        {
            if (Instance.currentLoadedLevel != null)
            {
                Instance.currentLoadedLevel.Win();
            }
        }
        public static void LoseLevel()
        {
            if (Instance.currentLoadedLevel != null)
            {
                Instance.currentLoadedLevel.Lose();
            }
        }
    }
}

namespace Game
{
}