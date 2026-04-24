using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
    public class  LevelData : SerializedScriptableObject
    {
        public Vector2Int gridSize;
        [TableMatrix(HorizontalTitle = "X", VerticalTitle = "Y", DrawElementMethod = "DrawCellData", SquareCells = true)]
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
        protected CellData DrawCellData(Rect rect, CellData value)
        {
            // INITIALIZATION
            ConstantManager constantManager = GameManager.Instance.constantManager;
            Color normalCellColor = new Color(0.8f, 0.8f, 0.8f);
            Color emptyCellColor = new Color(0.2f, 0.2f, 0.2f);

            if (value == null)
            {
                value = new CellData { cellType = CellType.Empty };
            }
            Rect elementRect = new Rect(rect.x + rect.width * 0.1f, rect.y + rect.height * 0.1f, rect.width * 0.8f, rect.height * 0.8f);

            // DRAWING
            switch (value.cellType)
            {
                case CellType.Empty:
                    EditorGUI.DrawRect(rect, normalCellColor);
                    break;
                case CellType.Wall:
                    EditorGUI.DrawRect(rect, emptyCellColor);
                    break;
            }

            if (value.cellType != CellType.Empty) // Only empty cells can have elements
            {
                value.currentElement = null;
            }

            if(value.currentElement != null)
            {
                EditorGUI.DrawRect(elementRect, value.currentElement.color);
                if (value.currentElement.elementSprite != null)
                {
                    GUI.DrawTexture(elementRect, value.currentElement.elementSprite.texture, ScaleMode.ScaleToFit, true, 1, value.currentElement.color, Vector4.zero, Vector4.zero);
                }
            }

            // INTERACTION
            if (rect.Contains(Event.current.mousePosition))
            {
                // Cell Type Changes
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.E)
                    {
                        value.cellType = CellType.Empty;
                        MarkDirty();
                        Event.current.Use();
                    }
                    if (Event.current.keyCode == KeyCode.W)
                    {
                        value.cellType = CellType.Wall;
                        value.currentElement = null;
                        MarkDirty();
                        Event.current.Use();
                    }
                }

                // Element Assignment
                List<ElementData> elementPool = GameManager.Instance.elementData;
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    if (value.cellType == CellType.Empty)
                    {
                        if (elementPool.Count > 0)
                        {
                            // Create a dropdown menu
                            GenericMenu genericMenu = new GenericMenu();
                            if (elementPool != null)
                            {
                                for (int i = 0; i < elementPool.Count; i++)
                                {
                                    int index = i; // Capture the current index for the lambda
                                    genericMenu.AddItem(new GUIContent(elementPool[i].Name), false, () =>
                                    {
                                        value.currentElement = elementPool[index];
                                    });
                                }
                            }
                            MarkDirty();
                        }
                    }
                    Event.current.Use();
                }
            }

            return value;
        }
#endif
    }
}
