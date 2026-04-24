using System;
using UnityEngine;

namespace Game
{
    public class Grid3D : MonoBehaviour
    {
        [Header("Grid Settings")]
        public GridCellController[,] gridCellControllers;

        public Vector2Int  GridSize => new Vector2Int(gridCellControllers.GetLength(0), gridCellControllers.GetLength(1));

        internal GridCellController GetCellControllerAt(Vector2Int vector2Int)
        {
            if (vector2Int.x < 0 || vector2Int.y < 0 || vector2Int.x >= gridCellControllers.GetLength(0) || vector2Int.y >= gridCellControllers.GetLength(1))
                throw new ArgumentOutOfRangeException(nameof(vector2Int), $"Coordinates {vector2Int} are out of bounds for grid size {GridSize}.");
            return gridCellControllers[vector2Int.x, vector2Int.y];
        }
    }
}