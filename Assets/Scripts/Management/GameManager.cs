using UnityEngine;

namespace Game.Management
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
        }

        [Header("Managers")]
        public EventManager eventManager;
    }
}
