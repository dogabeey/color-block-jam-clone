using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Game
{
    public class GridElement : Grid3D // Each element is also a grid, allowing for nested grids if needed (Like stacked blocks or "containers" that can hold other elements)
    {
        [Header("Element Data")]
        public ElementData elementData; 
        public int groupIndex; // Same indexed element groups will act as one. They will be moved together and their tiles will be generated accordingly. 0 means no group.

        [Header("References")]
        public GridCellController currentCell; // The cell this element is currently occupying. This will be used for movement and tile generation.
        public Renderer elementRenderer;

        [Header("Input")]
        public bool enableInput = true;
        public float dragLiftHeight = 0.15f;
        public float snapDuration = 0.12f;

        private static GridElement inputDriver;
        private static DragContext activeDrag;
        
        public void Init()
        {
            if(elementData != null)
            {
                if(elementRenderer is MeshRenderer meshRenderer)
                {
                    if (elementData.elementMesh != null)
                    {
                        meshRenderer.GetComponent<MeshFilter>().mesh = elementData.elementMesh;
                    }
                    meshRenderer.material.color = elementData.color;
                }
                else if(elementRenderer is SpriteRenderer spriteRenderer)
                {
                    if (elementData.elementSprite != null)
                    {
                        spriteRenderer.sprite = elementData.elementSprite;
                    }
                    spriteRenderer.color = elementData.color;
                }
                else if(elementRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    if (elementData.elementMesh != null)
                    {
                        skinnedMeshRenderer.sharedMesh = elementData.elementMesh;
                    }
                    skinnedMeshRenderer.material.color = elementData.color;
                }
            }
        }

        private void OnEnable()
        {
            if (inputDriver == null)
            {
                inputDriver = this;
            }
        }

        private void OnDisable()
        {
            if (activeDrag != null && activeDrag.movingElements.Contains(this))
            {
                EndDrag();
            }

            if (inputDriver == this)
            {
                inputDriver = null;
                GridElement[] allElements = Object.FindObjectsByType<GridElement>(FindObjectsSortMode.None);
                for (int i = 0; i < allElements.Length; i++)
                {
                    if (allElements[i] != null && allElements[i] != this)
                    {
                        inputDriver = allElements[i];
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (!enableInput || inputDriver != this)
            {
                return;
            }

            if (activeDrag == null)
            {
                if (IsPointerDownThisFrame() && TryGetPointerScreenPosition(out Vector2 pointerScreenPosition))
                {
                    TryBeginDrag(pointerScreenPosition);
                }

                return;
            }

            if (IsPointerHeld() && TryGetPointerScreenPosition(out Vector2 dragScreenPosition))
            {
                ContinueDrag(dragScreenPosition);
            }

            if (IsPointerReleasedThisFrame())
            {
                EndDrag();
            }
        }

        private static bool IsPointerDownThisFrame()
        {
            return Input.touchCount > 0
                ? Input.GetTouch(0).phase == TouchPhase.Began
                : Input.GetMouseButtonDown(0);
        }

        private static bool IsPointerHeld()
        {
            if (Input.touchCount > 0)
            {
                TouchPhase phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
            }

            return Input.GetMouseButton(0);
        }

        private static bool IsPointerReleasedThisFrame()
        {
            return Input.touchCount > 0
                ? Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled
                : Input.GetMouseButtonUp(0);
        }

        private static bool TryGetPointerScreenPosition(out Vector2 position)
        {
            if (Input.touchCount > 0)
            {
                position = Input.GetTouch(0).position;
                return true;
            }

            position = Input.mousePosition;
            return true;
        }

        private void TryBeginDrag(Vector2 pointerScreenPosition)
        {
            Grid3D rootGrid = GetRootGrid();
            if (rootGrid == null)
            {
                return;
            }

            if (!TryScreenToGrid(rootGrid, pointerScreenPosition, out Vector2Int pointerGridPosition, out Vector3 pointerWorldPosition))
            {
                return;
            }

            GridCellController sourceCell = rootGrid.GetCellControllerAt(pointerGridPosition);
            if (sourceCell == null || sourceCell.currentElement == null)
            {
                return;
            }

            GridElement leader = sourceCell.currentElement;
            List<GridElement> groupedElements = CollectGroupedElements(leader, rootGrid);
            if (groupedElements.Count == 0)
            {
                return;
            }

            activeDrag = new DragContext
            {
                rootGrid = rootGrid,
                leader = leader,
                movingElements = groupedElements,
                movingElementsSet = new HashSet<GridElement>(groupedElements),
                startCoordinates = new Dictionary<GridElement, Vector2Int>(groupedElements.Count),
                appliedDelta = Vector2Int.zero,
                pointerStartWorld = pointerWorldPosition,
            };

            for (int i = 0; i < groupedElements.Count; i++)
            {
                GridElement element = groupedElements[i];
                if (element == null || element.currentCell == null)
                {
                    continue;
                }

                activeDrag.startCoordinates[element] = element.currentCell.gridPosition;
                element.transform.localPosition = Vector3.up * element.dragLiftHeight;
            }
        }

        private static List<GridElement> CollectGroupedElements(GridElement leader, Grid3D rootGrid)
        {
            List<GridElement> result = new List<GridElement>();
            if (leader == null)
            {
                return result;
            }

            GridElement[] allElements = Object.FindObjectsByType<GridElement>(FindObjectsSortMode.None);
            for (int i = 0; i < allElements.Length; i++)
            {
                GridElement candidate = allElements[i];
                if (candidate == null || candidate.currentCell == null)
                {
                    continue;
                }

                if (candidate.currentCell.transform.parent != rootGrid.transform)
                {
                    continue;
                }

                if (candidate.groupIndex == leader.groupIndex && candidate.elementData == leader.elementData)
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        private void ContinueDrag(Vector2 pointerScreenPosition)
        {
            if (activeDrag == null)
            {
                return;
            }

            if (!TryGetPointerWorldOnGridPlane(activeDrag.rootGrid, pointerScreenPosition, out Vector3 pointerWorldPosition))
            {
                return;
            }

            Vector3 worldDragDelta = pointerWorldPosition - activeDrag.pointerStartWorld;
            Vector3 localDragDelta = activeDrag.rootGrid.transform.InverseTransformVector(worldDragDelta);

            Vector2 desiredContinuousDelta = new Vector2(localDragDelta.x, localDragDelta.z);
            activeDrag.desiredContinuousDelta = desiredContinuousDelta;

            Vector2Int desiredDelta = new Vector2Int(
                desiredContinuousDelta.x >= 0f ? Mathf.FloorToInt(desiredContinuousDelta.x) : Mathf.CeilToInt(desiredContinuousDelta.x),
                desiredContinuousDelta.y >= 0f ? Mathf.FloorToInt(desiredContinuousDelta.y) : Mathf.CeilToInt(desiredContinuousDelta.y));

            TryMoveTowardsDesiredDelta(desiredDelta);
            UpdateVisualOffset(desiredContinuousDelta);
        }

        private static void UpdateVisualOffset(Vector2 desiredContinuousDelta)
        {
            if (activeDrag == null)
            {
                return;
            }

            Vector2 visualOffset = desiredContinuousDelta - new Vector2(activeDrag.appliedDelta.x, activeDrag.appliedDelta.y);

            if (visualOffset.x > 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.right))
            {
                visualOffset.x = 0f;
            }
            else if (visualOffset.x < 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.left))
            {
                visualOffset.x = 0f;
            }

            if (visualOffset.y > 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.up))
            {
                visualOffset.y = 0f;
            }
            else if (visualOffset.y < 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.down))
            {
                visualOffset.y = 0f;
            }

            visualOffset.x = Mathf.Clamp(visualOffset.x, -0.999f, 0.999f);
            visualOffset.y = Mathf.Clamp(visualOffset.y, -0.999f, 0.999f);

            activeDrag.visualOffset = visualOffset;
            ApplyVisualOffset(visualOffset);
        }

        private static void ApplyVisualOffset(Vector2 visualOffset)
        {
            if (activeDrag == null)
            {
                return;
            }

            for (int i = 0; i < activeDrag.movingElements.Count; i++)
            {
                GridElement element = activeDrag.movingElements[i];
                if (element == null)
                {
                    continue;
                }

                element.transform.localPosition = new Vector3(visualOffset.x, element.dragLiftHeight, visualOffset.y);
            }
        }

        private static void TryMoveTowardsDesiredDelta(Vector2Int desiredDelta)
        {
            if (activeDrag == null)
            {
                return;
            }

            MoveAxisTowardsDesired(desiredDelta.x, true);
            MoveAxisTowardsDesired(desiredDelta.y, false);
        }

        private static void MoveAxisTowardsDesired(int desiredAxisValue, bool isXAxis)
        {
            if (activeDrag == null)
            {
                return;
            }

            int safety = 0;
            while (safety < 128)
            {
                int currentAxisValue = isXAxis ? activeDrag.appliedDelta.x : activeDrag.appliedDelta.y;
                if (currentAxisValue == desiredAxisValue)
                {
                    break;
                }

                safety++;

                int stepDirection = desiredAxisValue > currentAxisValue ? 1 : -1;
                Vector2Int step = isXAxis
                    ? new Vector2Int(stepDirection, 0)
                    : new Vector2Int(0, stepDirection);

                Vector2Int candidateDelta = activeDrag.appliedDelta + step;
                if (!CanApplyDelta(candidateDelta))
                {
                    break;
                }

                ApplyDelta(candidateDelta);
            }
        }

        private static bool CanApplyDelta(Vector2Int delta)
        {
            if (activeDrag == null)
            {
                return false;
            }

            GridCellController[,] cells = activeDrag.rootGrid.gridCellControllers;
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            HashSet<Vector2Int> targetPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < activeDrag.movingElements.Count; i++)
            {
                GridElement element = activeDrag.movingElements[i];
                if (!activeDrag.startCoordinates.TryGetValue(element, out Vector2Int startPosition))
                {
                    return false;
                }

                Vector2Int targetPosition = startPosition + delta;
                if (targetPosition.x < 0 || targetPosition.y < 0 || targetPosition.x >= width || targetPosition.y >= height)
                {
                    return false;
                }

                GridCellController targetCell = cells[targetPosition.x, targetPosition.y];
                if (targetCell == null || targetCell.cellType == CellType.Wall)
                {
                    return false;
                }

                targetPositions.Add(targetPosition);
            }

            for (int i = 0; i < activeDrag.movingElements.Count; i++)
            {
                GridElement element = activeDrag.movingElements[i];
                Vector2Int targetPosition = activeDrag.startCoordinates[element] + delta;
                GridCellController targetCell = cells[targetPosition.x, targetPosition.y];

                GridElement occupant = targetCell.currentElement;
                if (occupant != null && !activeDrag.movingElementsSet.Contains(occupant))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ApplyDelta(Vector2Int delta)
        {
            if (activeDrag == null)
            {
                return;
            }

            GridCellController[,] cells = activeDrag.rootGrid.gridCellControllers;

            for (int i = 0; i < activeDrag.movingElements.Count; i++)
            {
                GridElement element = activeDrag.movingElements[i];
                if (element.currentCell != null && element.currentCell.currentElement == element)
                {
                    element.currentCell.currentElement = null;
                }
            }

            for (int i = 0; i < activeDrag.movingElements.Count; i++)
            {
                GridElement element = activeDrag.movingElements[i];
                Vector2Int targetPosition = activeDrag.startCoordinates[element] + delta;
                GridCellController targetCell = cells[targetPosition.x, targetPosition.y];

                element.currentCell = targetCell;
                targetCell.currentElement = element;

                element.transform.SetParent(targetCell.transform, false);
                element.transform.localPosition = Vector3.up * element.dragLiftHeight;
            }

            activeDrag.appliedDelta = delta;
        }

        private static void EndDrag()
        {
            if (activeDrag == null)
            {
                return;
            }

            if (activeDrag.isSnapping)
            {
                return;
            }

            Vector2Int roundedTarget = new Vector2Int(
                Mathf.RoundToInt(activeDrag.desiredContinuousDelta.x),
                Mathf.RoundToInt(activeDrag.desiredContinuousDelta.y));

            TryMoveTowardsDesiredDelta(roundedTarget);

            Vector2 targetVisualOffset = new Vector2(
                roundedTarget.x - activeDrag.appliedDelta.x,
                roundedTarget.y - activeDrag.appliedDelta.y);

            targetVisualOffset.x = Mathf.Clamp(targetVisualOffset.x, -1f, 1f);
            targetVisualOffset.y = Mathf.Clamp(targetVisualOffset.y, -1f, 1f);

            if (targetVisualOffset.x > 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.right))
            {
                targetVisualOffset.x = 0f;
            }
            else if (targetVisualOffset.x < 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.left))
            {
                targetVisualOffset.x = 0f;
            }

            if (targetVisualOffset.y > 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.up))
            {
                targetVisualOffset.y = 0f;
            }
            else if (targetVisualOffset.y < 0f && !CanApplyDelta(activeDrag.appliedDelta + Vector2Int.down))
            {
                targetVisualOffset.y = 0f;
            }

            GridElement coroutineOwner = inputDriver != null ? inputDriver : activeDrag.leader;
            if (coroutineOwner == null)
            {
                for (int i = 0; i < activeDrag.movingElements.Count; i++)
                {
                    GridElement element = activeDrag.movingElements[i];
                    if (element != null)
                    {
                        coroutineOwner = element;
                        break;
                    }
                }
            }

            if (coroutineOwner == null)
            {
                activeDrag = null;
                return;
            }

            activeDrag.isSnapping = true;
            coroutineOwner.StartCoroutine(SnapAndRelease(targetVisualOffset));
        }

        private static IEnumerator SnapAndRelease(Vector2 targetVisualOffset)
        {
            if (activeDrag == null)
            {
                yield break;
            }

            DragContext drag = activeDrag;

            Vector2 startOffset = drag.visualOffset;
            float duration = 0.1f;
            if (drag.leader != null)
            {
                duration = Mathf.Max(0.01f, drag.leader.snapDuration);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (activeDrag != drag)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector2 offset = Vector2.Lerp(startOffset, targetVisualOffset, t);
                drag.visualOffset = offset;

                for (int i = 0; i < drag.movingElements.Count; i++)
                {
                    GridElement element = drag.movingElements[i];
                    if (element == null)
                    {
                        continue;
                    }

                    element.transform.localPosition = new Vector3(offset.x, element.dragLiftHeight * (1f - t), offset.y);
                }

                yield return null;
            }

            Vector2Int extraSnap = new Vector2Int(
                Mathf.RoundToInt(targetVisualOffset.x),
                Mathf.RoundToInt(targetVisualOffset.y));

            if (extraSnap != Vector2Int.zero)
            {
                Vector2Int finalDelta = drag.appliedDelta + extraSnap;
                if (CanApplyDelta(finalDelta))
                {
                    ApplyDelta(finalDelta);
                }
            }

            for (int i = 0; i < drag.movingElements.Count; i++)
            {
                GridElement element = drag.movingElements[i];
                if (element != null)
                {
                    element.transform.localPosition = Vector3.zero;
                }
            }

            if (activeDrag == drag)
            {
                activeDrag = null;
            }
        }

        private static Grid3D GetRootGrid()
        {
            LevelScene currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLoadedLevel : null;
            return currentLevel != null ? currentLevel.grid3D : null;
        }

        private static bool TryScreenToGrid(Grid3D grid, Vector2 screenPosition, out Vector2Int gridPosition, out Vector3 worldPoint)
        {
            gridPosition = Vector2Int.zero;
            worldPoint = Vector3.zero;

            if (!TryGetPointerWorldOnGridPlane(grid, screenPosition, out worldPoint))
            {
                return false;
            }

            Vector3 localPoint = grid.transform.InverseTransformPoint(worldPoint);
            gridPosition = new Vector2Int(Mathf.RoundToInt(localPoint.x), Mathf.RoundToInt(localPoint.z));

            GridCellController[,] cells = grid.gridCellControllers;
            if (cells == null)
            {
                return false;
            }

            int width = cells.GetLength(0);
            int height = cells.GetLength(1);
            return gridPosition.x >= 0 && gridPosition.y >= 0 && gridPosition.x < width && gridPosition.y < height;
        }

        private static bool TryGetPointerWorldOnGridPlane(Grid3D grid, Vector2 screenPosition, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;

            Camera mainCamera = Camera.main;
            if (grid == null || mainCamera == null)
            {
                return false;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(Vector3.up, grid.transform.position);
            if (!plane.Raycast(ray, out float enter))
            {
                return false;
            }

            worldPoint = ray.GetPoint(enter);
            return true;
        }

        private sealed class DragContext
        {
            public Grid3D rootGrid;
            public GridElement leader;
            public List<GridElement> movingElements;
            public HashSet<GridElement> movingElementsSet;
            public Dictionary<GridElement, Vector2Int> startCoordinates;
            public Vector2Int appliedDelta;
            public Vector3 pointerStartWorld;
            public Vector2 desiredContinuousDelta;
            public Vector2 visualOffset;
            public bool isSnapping;
        }
    }

    [System.Serializable]
    public class CellData // This is used by Level Data to define the layout of the grid and the elements within it. Each cell can be empty, a wall, or contain an element, etc.
    {
        public Vector2Int position;
        public CellType cellType;
        public ElementData currentElement; // Optional: If the cell contains an element, this will hold its data
        public int elementGroupIndex; // Same indexed element groups will act as one. They will be moved together and their tiles will be generated accordingly. 0 means no group.
    }

    public enum CellType
    {
        Empty,
        Wall,
    }
}