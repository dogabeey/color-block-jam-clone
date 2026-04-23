using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "ConstantManager", menuName = "Game/Managers/Constant Manager...", order = 1)]
    public class ConstantManager : ScriptableObject
    {
        [Header("Level Editor Configuration")]
        public Color emptyCell = new Color(1f, 1f, 1f, 0.5f);
        public Color wallCell = new Color(0f, 0f, 0f, 0.5f);
        
    }
}