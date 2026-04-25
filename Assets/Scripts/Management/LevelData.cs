using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
    public class LevelData : SerializedScriptableObject
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
            Color normalCellColor = new Color(0.8f, 0.8f, 0.8f);
            Color wallCellColor = new Color(0.2f, 0.2f, 0.2f);
            Color exitCellDefaultColor = new Color(0.45f, 0.45f, 0.45f);

            if (value == null)
            {
                value = new CellData { cellType = CellType.Empty };
            }

            bool isEdgeCell = Grid3D.IsEdgeCoordinate(value.position, gridSize);
            bool isCornerCell = (value.position.x == 0 || value.position.x == gridSize.x - 1) && (value.position.y == 0 || value.position.y == gridSize.y - 1);
            bool isExitCell = isEdgeCell && !isCornerCell;
            if (isEdgeCell)
            {
                value.cellType = CellType.Wall;
            }

            if (isCornerCell)
            {
                value.currentElement = null;
            }

            Rect elementRect = rect;

            // Add gui text to indicate current index
            GUIStyle indexStyle = new GUIStyle();
            indexStyle.alignment = TextAnchor.MiddleCenter;
            indexStyle.normal.textColor = Color.black;
            indexStyle.fontStyle = FontStyle.Bold;

            // DRAWING
            // Cell Background
            if (isExitCell)
            {
                EditorGUI.DrawRect(rect, value.currentElement != null ? value.currentElement.color : exitCellDefaultColor);
            }
            else if (isCornerCell)
            {
                EditorGUI.DrawRect(rect, exitCellDefaultColor);
            }
            else
            {
                switch (value.cellType)
                {
                    case CellType.Empty:
                        EditorGUI.DrawRect(rect, normalCellColor);
                        break;
                    case CellType.Wall:
                        EditorGUI.DrawRect(rect, wallCellColor);
                        break;
                }
            }

            // Element Display
            if (!isEdgeCell && value.cellType != CellType.Empty) // Only empty interior cells can have elements
            {
                value.currentElement = null;
            }
            if(value.currentElement != null)
            {
                EditorGUI.DrawRect(elementRect, value.currentElement.color);
                if (!isEdgeCell && value.currentElement.elementSprite != null)
                {
                    GUI.DrawTexture(elementRect, value.currentElement.elementSprite.texture, ScaleMode.ScaleToFit, true, 1, value.currentElement.color, Vector4.zero, Vector4.zero);
                }
                // Grid Indicator
                GUI.Label(elementRect, value.elementGroupIndex == 0 ? "" : value.elementGroupIndex.ToString(), indexStyle);
            }


            // INTERACTION
            if (rect.Contains(Event.current.mousePosition))
            {
                // Cell Type Changes
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.E && !isEdgeCell) // Empty
                    {
                        value.cellType = CellType.Empty;
                    }
                    else if (Event.current.keyCode == KeyCode.W && !isEdgeCell) // Wall
                    {
                        value.cellType = CellType.Wall;
                        value.currentElement = null;
                    }
                    else if (Event.current.keyCode == KeyCode.R) // Remove element
                    {
                        value.currentElement = null;
                    }
                    else if (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow) // Directional Restriction Vertical Only
                    {
                        if (value.movementRestriction == DirectionRestriction.VerticalOnly)
                        {
                            value.movementRestriction = DirectionRestriction.None;
                        }
                        else
                        {
                            value.movementRestriction = DirectionRestriction.VerticalOnly;
                        }
                    }
                    else if (Event.current.keyCode == KeyCode.LeftArrow || Event.current.keyCode == KeyCode.RightArrow) // Directional Restriction Horizontal Only
                    {
                        if (value.movementRestriction == DirectionRestriction.HorizontalOnly)
                        {
                            value.movementRestriction = DirectionRestriction.None;
                        }
                        else
                        {
                            value.movementRestriction = DirectionRestriction.HorizontalOnly;
                        }
                    }
                    // Catch alphanumeric keys for element assignment
                    else if (Event.current.keyCode >= KeyCode.Alpha0 && Event.current.keyCode <= KeyCode.Alpha9 && value.currentElement != null)
                    {
                        int index = Event.current.keyCode - KeyCode.Alpha0;
                        value.elementGroupIndex = index;
                    }
                    MarkDirty();
                    Event.current.Use();
                }

                // Element Assignment
                List<ElementData> elementPool = GameManager.Instance.elementData;
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    if (isExitCell || value.cellType == CellType.Empty)
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
                            genericMenu.ShowAsContext();
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
