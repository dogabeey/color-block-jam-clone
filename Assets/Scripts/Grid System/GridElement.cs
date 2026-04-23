using UnityEngine;

namespace Game
{
    public class GridElement : Grid3D // Each element is also a grid, allowing for nested grids if needed (Like stacked blocks or "containers" that can hold other elements)
    {
        [Header("Element Data")]
        public ElementData elementData;
    }

    [System.Serializable]
    public class CellData // This is used by Level Data to define the layout of the grid and the elements within it. Each cell can be empty, a wall, or contain an element, etc.
    {
        public Vector2Int position;
        public CellType cellType;
        public ElementData currentElement; // Optional: If the cell contains an element, this will hold its data
        public int elementGroupIndex; // Same indexed element groups will act as one. They will be moved together and their tiles will be generated accordingly.
    }

    public enum CellType
    {
        Empty,
        Wall,
    }
}