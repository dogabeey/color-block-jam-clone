using Sirenix.OdinInspector;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
    public class  LevelData : SerializedScriptableObject
    {
        public Vector2Int gridSize;
        [TableMatrix(HorizontalTitle = "X", VerticalTitle = "Y", DrawElementMethod = "DrawCellData")]
        public CellData[,] gridLayout;

#if UNITY_EDITOR
        [Button]
        public void InitializeArray()
        {
            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                gridLayout = new CellData[0, 0];
                MarkDirty();
                return;
            }

            gridLayout = new CellData[gridSize.x, gridSize.y];
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    gridLayout[x, y] = new CellData
                    {
                        position = new Vector2Int(x, y),
                        cellType = CellType.Empty
                    };
                }
            }

            MarkDirty();
        }
        private void MarkDirty()
        {
            EditorUtility.SetDirty(this);
        }
        private CellData DrawCellData(CellData value)
        {
            // INITIALIZATION
            if (value == null)
            {
                value = new CellData { cellType = CellType.Empty };
            }
            return value;
        }
#endif
    }
}
