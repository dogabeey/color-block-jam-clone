using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Game
{
    public class GridElement : Grid3D // Each element is also a grid, allowing for nested grids if needed (Like stacked blocks or "containers" that can hold other elements)
    {
        internal ElementData elementData; 
        internal int groupIndex; // Same indexed element groups will act as one. They will be moved together and their tiles will be generated accordingly. 0 means no group.
        internal GridCellController currentCell; // The cell this element is currently occupying. This will be used for movement and tile generation.
        internal DirectionRestriction movementRestriction;

        [Header("References")]
        public Renderer elementRenderer;
        public SpriteRenderer verticalArrow;
        public SpriteRenderer horizontalArrow;

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

            UpdateRestrictionArrows();
        }

        private void UpdateRestrictionArrows()
        {
            if (verticalArrow != null)
            {
                verticalArrow.gameObject.SetActive(movementRestriction == DirectionRestriction.VerticalOnly);
            }

            if (horizontalArrow != null)
            {
                horizontalArrow.gameObject.SetActive(movementRestriction == DirectionRestriction.HorizontalOnly);
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

            if (activeDrag != null)
            {
                if (activeDrag.movingElements == null || activeDrag.movingElements.Count == 0)
                {
                    CancelActiveDrag();
                    return;
                }

                if (activeDrag.isSnapping && (activeDrag.snapSequence == null || !activeDrag.snapSequence.IsActive()))
                {
                    CancelActiveDrag();
                    return;
                }
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
            if (Input.touchCount > 0)
            {
                TouchPhase phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }

            if (activeDrag != null && !Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
            {
                return true;
            }

            return Input.GetMouseButtonUp(0);
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

            EventManager.TriggerEvent(GameEvent.ELEMENT_MOVE);
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

            TryMoveTowardsDesiredDelta(desiredDelta, desiredContinuousDelta);
            UpdateVisualOffset(desiredContinuousDelta);

            if (TryExitIfAlignedWithAdjacentExit())
            {
                return;
            }
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

        private static void TryMoveTowardsDesiredDelta(Vector2Int desiredDelta, Vector2 prioritySource)
        {
            if (activeDrag == null)
            {
                return;
            }

            float remainingX = Mathf.Abs(prioritySource.x - activeDrag.appliedDelta.x);
            float remainingY = Mathf.Abs(prioritySource.y - activeDrag.appliedDelta.y);

            const float axisTieThreshold = 0.05f;
            bool prioritizeX;
            if (Mathf.Abs(remainingX - remainingY) <= axisTieThreshold)
            {
                prioritizeX = activeDrag.prioritizeXAxis;
            }
            else
            {
                prioritizeX = remainingX > remainingY;
            }

            activeDrag.prioritizeXAxis = prioritizeX;

            if (prioritizeX)
            {
                MoveAxisTowardsDesired(desiredDelta.x, true);
                if (activeDrag == null)
                {
                    return;
                }
                MoveAxisTowardsDesired(desiredDelta.y, false);
            }
            else
            {
                MoveAxisTowardsDesired(desiredDelta.y, false);
                if (activeDrag == null)
                {
                    return;
                }
                MoveAxisTowardsDesired(desiredDelta.x, true);
            }
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
                    if (TryApplyExitStep(step))
                    {
                        return;
                    }

                    break;
                }

                ApplyDelta(candidateDelta);
                if (activeDrag == null)
                {
                    return;
                }
            }
        }

        private static bool TryExitIfAlignedWithAdjacentExit()
        {
            if (!TryCollectCurrentPositions(out List<Vector2Int> currentPositions))
            {
                return false;
            }

            if (!TryGetExitMatchForCurrentPositions(currentPositions, out Vector2Int exitDirection, out List<GridCellController> exitCells))
            {
                return false;
            }

            BeginExit(exitDirection, exitCells, currentPositions);
            return true;
        }

        private static bool TryApplyExitStep(Vector2Int step)
        {
            if (activeDrag == null || step == Vector2Int.zero)
            {
                return false;
            }

            if (!TryCollectCurrentPositions(out List<Vector2Int> currentPositions))
            {
                return false;
            }

            if (!TryGetExitMatchForCurrentPositions(currentPositions, out Vector2Int exitDirection, out List<GridCellController> exitCells))
            {
                return false;
            }

            if (exitDirection != step)
            {
                return false;
            }

            BeginExit(exitDirection, exitCells, currentPositions);
            return true;
        }

        private static bool TryCollectCurrentPositions(out List<Vector2Int> currentPositions)
        {
            currentPositions = null;

            if (activeDrag == null || activeDrag.movingElements == null || activeDrag.movingElements.Count == 0)
            {
                return false;
            }

            currentPositions = new List<Vector2Int>(activeDrag.movingElements.Count);
            for (int i = 0; i < activeDrag.movingElements.Count; i++)
            {
                GridElement element = activeDrag.movingElements[i];
                if (element == null || element.currentCell == null)
                {
                    return false;
                }

                currentPositions.Add(element.currentCell.gridPosition);
            }

            return true;
        }

        private static bool TryGetExitMatchForCurrentPositions(List<Vector2Int> currentPositions, out Vector2Int exitDirection, out List<GridCellController> exitCells)
        {
            exitDirection = Vector2Int.zero;
            exitCells = null;

            if (activeDrag == null || activeDrag.rootGrid == null || activeDrag.leader == null || activeDrag.leader.elementData == null)
            {
                return false;
            }

            GridCellController[,] cells = activeDrag.rootGrid.gridCellControllers;
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            ExitSide[] candidateSides = { ExitSide.Left, ExitSide.Right, ExitSide.Bottom, ExitSide.Top };
            for (int sideIndex = 0; sideIndex < candidateSides.Length; sideIndex++)
            {
                ExitSide side = candidateSides[sideIndex];
                if (!HasAnyAdjacentElementToExit(currentPositions, side, width, height))
                {
                    continue;
                }

                List<int> projectedValues = GetProjectedValues(currentPositions, side);
                if (projectedValues.Count == 0 || !AreProjectedValuesContiguous(projectedValues))
                {
                    continue;
                }

                int projectionStart = projectedValues[0];
                int projectionEnd = projectedValues[projectedValues.Count - 1];
                if (!HasMatchingContiguousExitSegment(cells, side, projectionStart, projectionEnd, width, height, activeDrag.leader.elementData))
                {
                    continue;
                }

                exitCells = new List<GridCellController>(projectedValues.Count);
                for (int i = 0; i < projectedValues.Count; i++)
                {
                    GridCellController edgeCell = GetEdgeCell(cells, side, projectedValues[i], width, height);
                    if (edgeCell != null)
                    {
                        exitCells.Add(edgeCell);
                    }
                }

                exitDirection = GetExitDirection(side);
                return true;
            }

            return false;
        }

        private static bool HasAnyAdjacentElementToExit(List<Vector2Int> positions, ExitSide side, int width, int height)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int position = positions[i];
                switch (side)
                {
                    case ExitSide.Left:
                        if (position.x == 1)
                        {
                            return true;
                        }
                        break;
                    case ExitSide.Right:
                        if (position.x == width - 2)
                        {
                            return true;
                        }
                        break;
                    case ExitSide.Bottom:
                        if (position.y == 1)
                        {
                            return true;
                        }
                        break;
                    case ExitSide.Top:
                        if (position.y == height - 2)
                        {
                            return true;
                        }
                        break;
                }
            }

            return false;
        }

        private static List<int> GetProjectedValues(List<Vector2Int> positions, ExitSide side)
        {
            HashSet<int> uniqueValues = new HashSet<int>();
            for (int i = 0; i < positions.Count; i++)
            {
                uniqueValues.Add(side == ExitSide.Left || side == ExitSide.Right ? positions[i].y : positions[i].x);
            }

            List<int> values = new List<int>(uniqueValues);
            values.Sort();
            return values;
        }

        private static bool AreProjectedValuesContiguous(List<int> values)
        {
            if (values.Count <= 1)
            {
                return values.Count == 1;
            }

            for (int i = 1; i < values.Count; i++)
            {
                if (values[i] - values[i - 1] != 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasMatchingContiguousExitSegment(GridCellController[,] cells, ExitSide side, int projectionStart, int projectionEnd, int width, int height, ElementData elementData)
        {
            List<int> matchingIndices = new List<int>();
            int edgeLength = side == ExitSide.Left || side == ExitSide.Right ? height : width;

            for (int i = 0; i < edgeLength; i++)
            {
                GridCellController edgeCell = GetEdgeCell(cells, side, i, width, height);
                if (edgeCell != null && edgeCell.isExitCell && edgeCell.exitElementData == elementData)
                {
                    matchingIndices.Add(i);
                }
            }

            if (matchingIndices.Count == 0)
            {
                return false;
            }

            int segmentStart = matchingIndices[0];
            int segmentEnd = matchingIndices[0];

            for (int i = 1; i < matchingIndices.Count; i++)
            {
                int index = matchingIndices[i];
                if (index == segmentEnd + 1)
                {
                    segmentEnd = index;
                    continue;
                }

                if (projectionStart >= segmentStart && projectionEnd <= segmentEnd)
                {
                    return true;
                }

                segmentStart = index;
                segmentEnd = index;
            }

            return projectionStart >= segmentStart && projectionEnd <= segmentEnd;
        }

        private static GridCellController GetEdgeCell(GridCellController[,] cells, ExitSide side, int edgeIndex, int width, int height)
        {
            return side switch
            {
                ExitSide.Left => cells[0, edgeIndex],
                ExitSide.Right => cells[width - 1, edgeIndex],
                ExitSide.Bottom => cells[edgeIndex, 0],
                ExitSide.Top => cells[edgeIndex, height - 1],
                _ => null,
            };
        }

        private static void BeginExit(Vector2Int direction, List<GridCellController> exitCells = null, List<Vector2Int> currentPositions = null)
        {
            if (activeDrag == null)
            {
                return;
            }

            DragContext exitingDrag = activeDrag;
            List<Vector2Int> positionsForDistance = currentPositions;
            if (positionsForDistance == null)
            {
                TryCollectCurrentPositions(out positionsForDistance);
            }

            int travelCellCount = GetBlockLengthAlongDirection(positionsForDistance, direction);
            float exitMoveSpeed = GetExitMoveSpeed();
            float travelDuration = Mathf.Max(0.01f, travelCellCount / Mathf.Max(0.01f, exitMoveSpeed));

            for (int i = 0; i < exitingDrag.movingElements.Count; i++)
            {
                GridElement element = exitingDrag.movingElements[i];
                if (element == null)
                {
                    continue;
                }

                if (element.currentCell != null && element.currentCell.currentElement == element)
                {
                    element.currentCell.currentElement = null;
                }

                element.currentCell = null;
            }

            activeDrag = null;

            if (exitingDrag.rootGrid != null)
            {
                TriggerExitGates(exitCells, travelDuration);
                PlayExitAndDestroy(exitingDrag.movingElements, direction, travelCellCount, travelDuration);
            }
        }

        private static float GetExitMoveSpeed()
        {
            ConstantManager constantManager = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            if (constantManager != null && constantManager.blockExitMoveSpeed > 0f)
            {
                return constantManager.blockExitMoveSpeed;
            }

            return 6f;
        }

        private static int GetBlockLengthAlongDirection(List<Vector2Int> positions, Vector2Int direction)
        {
            if (positions == null || positions.Count == 0)
            {
                return 1;
            }

            bool horizontalExit = direction.x != 0;
            int minAxis = int.MaxValue;
            int maxAxis = int.MinValue;

            for (int i = 0; i < positions.Count; i++)
            {
                int axis = horizontalExit ? positions[i].x : positions[i].y;
                if (axis < minAxis)
                {
                    minAxis = axis;
                }

                if (axis > maxAxis)
                {
                    maxAxis = axis;
                }
            }

            return Mathf.Max(1, (maxAxis - minAxis) + 1);
        }

        private static void PlayExitAndDestroy(List<GridElement> elements, Vector2Int direction, int travelCellCount, float duration)
        {
            if (elements == null || elements.Count == 0)
            {
                return;
            }

            float distance = Mathf.Max(1f, travelCellCount);
            Vector3 localMove = new Vector3(direction.x, 0f, direction.y) * distance;

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < elements.Count; i++)
            {
                GridElement element = elements[i];
                if (element == null)
                {
                    continue;
                }

                sequence.Join(element.transform.DOLocalMove(element.transform.localPosition + localMove, duration).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() =>
            {
                int clearedCount = 0;
                HashSet<GridElement> clearedElements = new HashSet<GridElement>();

                for (int i = 0; i < elements.Count; i++)
                {
                    GridElement element = elements[i];
                    if (element == null)
                    {
                        continue;
                    }

                    SpawnBlockExitParticle(element);
                    clearedElements.Add(element);
                    clearedCount++;
                }

                for (int i = 0; i < elements.Count; i++)
                {
                    GridElement element = elements[i];
                    if (element != null)
                    {
                        Object.Destroy(element.gameObject);
                    }
                }

                if (clearedCount > 0)
                {
                    ElementData clearedType = elements[0] != null ? elements[0].elementData : null;
                    EventManager.TriggerEvent(GameEvent.BLOCK_CLEARED, new EventParam(paramScriptable: clearedType, paramInt: clearedCount));
                }

                Grid3D rootGrid = GetRootGrid();
                int remainingCount = CountRemainingBoardElements(rootGrid, clearedElements);
                if (remainingCount == 0)
                {
                    EventManager.TriggerEvent(GameEvent.BOARD_CLEARED, new EventParam(paramInt: 0));
                }
            });
        }

        private static int CountRemainingBoardElements(Grid3D rootGrid, HashSet<GridElement> excluded)
        {
            if (rootGrid == null)
            {
                return 0;
            }

            GridElement[] allElements = Object.FindObjectsByType<GridElement>(FindObjectsSortMode.None);
            int remaining = 0;

            for (int i = 0; i < allElements.Length; i++)
            {
                GridElement element = allElements[i];
                if (element == null)
                {
                    continue;
                }

                if (excluded != null && excluded.Contains(element))
                {
                    continue;
                }

                if (element.currentCell == null || element.currentCell.transform.parent != rootGrid.transform)
                {
                    continue;
                }

                remaining++;
            }

            return remaining;
        }

        private static void SpawnBlockExitParticle(GridElement element)
        {
            if (element == null)
            {
                return;
            }

            ConstantManager constantManager = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            if (constantManager == null || constantManager.blockExitParticlePrefab == null)
            {
                return;
            }

            ParticleSystem particle = Object.Instantiate(constantManager.blockExitParticlePrefab, element.transform.position, Quaternion.identity);
            if (particle == null)
            {
                return;
            }

            ParticleSystem.MainModule main = particle.main;
            if (element.elementData != null)
            {
                main.startColor = element.elementData.color;
            }

            particle.Play();

            float lifetime = main.duration + main.startLifetime.constantMax + 0.25f;
            Object.Destroy(particle.gameObject, lifetime);
        }

        private static void TriggerExitGates(List<GridCellController> exitCells, float duration)
        {
            if (exitCells == null || exitCells.Count == 0)
            {
                return;
            }

            HashSet<ExitGateController> uniqueGates = new HashSet<ExitGateController>();
            for (int i = 0; i < exitCells.Count; i++)
            {
                GridCellController cell = exitCells[i];
                if (cell == null || cell.exitGate == null)
                {
                    continue;
                }

                if (uniqueGates.Add(cell.exitGate))
                {
                    cell.exitGate.PlayTransit(duration);
                }
            }
        }

        private static Vector2Int GetExitDirection(ExitSide side)
        {
            return side switch
            {
                ExitSide.Left => Vector2Int.left,
                ExitSide.Right => Vector2Int.right,
                ExitSide.Bottom => Vector2Int.down,
                ExitSide.Top => Vector2Int.up,
                _ => Vector2Int.zero,
            };
        }


        private static bool CanApplyDelta(Vector2Int delta)
        {
            if (activeDrag == null)
            {
                return false;
            }

            Vector2Int movementDelta = delta - activeDrag.appliedDelta;

            GridCellController[,] cells = activeDrag.rootGrid.gridCellControllers;
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            HashSet<Vector2Int> targetPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < activeDrag.movingElements.Count; i++)
            {
                GridElement element = activeDrag.movingElements[i];
                if (!AllowsMovementForRestriction(element, movementDelta))
                {
                    return false;
                }

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

        private static bool AllowsMovementForRestriction(GridElement element, Vector2Int movementDelta)
        {
            if (element == null || movementDelta == Vector2Int.zero)
            {
                return true;
            }

            return element.movementRestriction switch
            {
                DirectionRestriction.HorizontalOnly => movementDelta.y == 0,
                DirectionRestriction.VerticalOnly => movementDelta.x == 0,
                _ => true,
            };
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

            TryMoveTowardsDesiredDelta(roundedTarget, roundedTarget);

            if (TryExitIfAlignedWithAdjacentExit())
            {
                return;
            }

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

            if (activeDrag.movingElements == null || activeDrag.movingElements.Count == 0)
            {
                activeDrag = null;
                return;
            }

            activeDrag.isSnapping = true;
            StartSnapRelease(targetVisualOffset);
        }

        private static void CancelActiveDrag()
        {
            if (activeDrag == null)
            {
                return;
            }

            DragContext drag = activeDrag;

            if (drag.snapSequence != null && drag.snapSequence.IsActive())
            {
                drag.snapSequence.Kill(false);
                drag.snapSequence = null;
            }

            if (drag.movingElements != null)
            {
                for (int i = 0; i < drag.movingElements.Count; i++)
                {
                    GridElement element = drag.movingElements[i];
                    if (element != null)
                    {
                        element.transform.localPosition = Vector3.zero;
                    }
                }
            }

            activeDrag = null;
        }

        private static void StartSnapRelease(Vector2 targetVisualOffset)
        {
            if (activeDrag == null)
            {
                return;
            }

            DragContext drag = activeDrag;

            float duration = 0.1f;
            if (drag.leader != null)
            {
                duration = Mathf.Max(0.01f, drag.leader.snapDuration);
            }

            if (drag.snapSequence != null && drag.snapSequence.IsActive())
            {
                drag.snapSequence.Kill(false);
            }

            Sequence snapSequence = DOTween.Sequence();
            for (int i = 0; i < drag.movingElements.Count; i++)
            {
                GridElement element = drag.movingElements[i];
                if (element == null)
                {
                    continue;
                }

                Vector3 snapTarget = new Vector3(targetVisualOffset.x, 0f, targetVisualOffset.y);
                snapSequence.Join(element.transform.DOLocalMove(snapTarget, duration).SetEase(Ease.OutQuad));
            }

            drag.snapSequence = snapSequence;
            snapSequence.OnComplete(() =>
            {
                if (activeDrag != drag)
                {
                    return;
                }

                drag.visualOffset = targetVisualOffset;

                Vector2Int extraSnap = new Vector2Int(
                    Mathf.RoundToInt(targetVisualOffset.x),
                    Mathf.RoundToInt(targetVisualOffset.y));

                if (extraSnap != Vector2Int.zero)
                {
                    Vector2Int finalDelta = drag.appliedDelta + extraSnap;
                    if (CanApplyDelta(finalDelta))
                    {
                        ApplyDelta(finalDelta);
                        if (activeDrag == null)
                        {
                            return;
                        }
                    }
                }

                if (TryExitIfAlignedWithAdjacentExit())
                {
                    return;
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
            });
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
            public bool prioritizeXAxis = true;
            public Sequence snapSequence;
        }

        private enum ExitSide
        {
            None,
            Left,
            Right,
            Bottom,
            Top,
        }
        
    }

    [System.Serializable]
    public class CellData // This is used by Level Data to define the layout of the grid and the elements within it. Each cell can be empty, a wall, or contain an element, etc.
    {
        public Vector2Int position;
        public CellType cellType;
        public ElementData currentElement; // Optional: If the cell contains an element, this will hold its data
        public DirectionRestriction movementRestriction; // Optional: If the cell has movement restrictions for elements on it 
        public int elementGroupIndex; // Same indexed element groups will act as one. They will be moved together and their tiles will be generated accordingly. 0 means no group.

    }

    public enum CellType
    {
        Empty,
        Wall,
    }
        public enum DirectionRestriction
        {
            None,
            HorizontalOnly,
            VerticalOnly,
        }
}