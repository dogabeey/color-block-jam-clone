using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public interface ICameraBoundSetter
    {
        Vector2 CameraBound { get; }
    }
    public class CameraBoundController : MonoBehaviour
    {
        public enum Plane
        {
            XY,
            XZ,
            YZ,
        }

        [SerializeField] private float initDelay = 0.1f;
        [SerializeField] private Vector4 boundsOffset;
        [SerializeField] private Plane plane;

        public Vector2 MinBounds { get; private set; }
        public Vector2 MaxBounds { get; private set; }

        private Coroutine refreshRoutine;
        private bool hasCalibrated;
        private int lastGridCellCount = -1;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.GRID_INITIALIZED, OnGridInitialized);
            EventManager.StartListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.GRID_INITIALIZED, OnGridInitialized);
            EventManager.StopListening(GameEvent.LEVEL_STARTED, OnLevelStarted);

            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }
        }

        private void OnLevelStarted(EventParam _)
        {
            if (!hasCalibrated)
            {
                RequestRefresh();
            }
        }

        private void OnGridInitialized(EventParam param)
        {
            int gridCellCount = param != null ? param.paramInt : -1;
            if (hasCalibrated && gridCellCount > 0 && gridCellCount == lastGridCellCount)
            {
                return;
            }

            if (gridCellCount > 0)
            {
                lastGridCellCount = gridCellCount;
            }

            RequestRefresh();
        }

        private void RequestRefresh()
        {
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
            }

            refreshRoutine = StartCoroutine(RefreshBoundsAfterDelay());
        }

        private IEnumerator RefreshBoundsAfterDelay()
        {
            yield return new WaitForSeconds(initDelay);

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                yield break;
            }

            mainCamera.orthographic = true;

            MonoBehaviour[] components = FindObjectsOfType<MonoBehaviour>(false);

            bool hasAny = false;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;

            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component is not ICameraBoundSetter boundSetter)
                {
                    continue;
                }

                Vector2 point = boundSetter.CameraBound;
                if (!hasAny)
                {
                    min = point;
                    max = point;
                    hasAny = true;
                }
                else
                {
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }

            if (!hasAny)
            {
                if (!TryGetLogicalGridBounds(plane, out min, out max))
                    yield break;
                hasAny = true;
            }
            else if (TryGetLogicalGridBounds(plane, out Vector2 logicalMin, out Vector2 logicalMax))
            {
                min = Vector2.Min(min, logicalMin);
                max = Vector2.Max(max, logicalMax);
            }

            min.x -= boundsOffset.x;
            max.x += boundsOffset.y;
            min.y -= boundsOffset.z;
            max.y += boundsOffset.w;

            MinBounds = min;
            MaxBounds = max;

            Vector3 cameraPosition = mainCamera.transform.position;
            ApplyPlaneCenterToPosition(ref cameraPosition, plane, (MinBounds.x + MaxBounds.x) * 0.5f, (MinBounds.y + MaxBounds.y) * 0.5f);
            mainCamera.transform.position = cameraPosition;

            float boundsHeight = Mathf.Max(0.0001f, MaxBounds.y - MinBounds.y);
            float boundsWidth = Mathf.Max(0.0001f, MaxBounds.x - MinBounds.x);
            float aspect = Mathf.Max(0.0001f, mainCamera.aspect);

            float sizeFromHeight = boundsHeight * 0.5f;
            float sizeFromWidth = (boundsWidth * 0.5f) / aspect;

            mainCamera.orthographicSize = Mathf.Max(sizeFromHeight, sizeFromWidth);
            hasCalibrated = true;
        }

        private static bool TryGetLogicalGridBounds(Plane plane, out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;

            LevelScene level = GameManager.Instance.currentLoadedLevel;

            Grid3D grid = level.grid3D;

            Vector2Int size = grid.GridSize;
            if (size.x <= 0 || size.y <= 0)
                return false;

            if (!TryEstimateGridTransform(grid, size, plane, out Vector2 origin, out Vector2 stepX, out Vector2 stepY))
                return false;

            bool hasAny = false;
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector2 p = origin + stepX * x + stepY * y;
                    if (!hasAny)
                    {
                        min = p;
                        max = p;
                        hasAny = true;
                    }
                    else
                    {
                        min = Vector2.Min(min, p);
                        max = Vector2.Max(max, p);
                    }
                }
            }

            return hasAny;
        }

        private static void ApplyPlaneCenterToPosition(ref Vector3 position, Plane plane, float axisA, float axisB)
        {
            switch (plane)
            {
                case Plane.XY:
                    position.x = axisA;
                    position.y = axisB;
                    break;
                case Plane.XZ:
                    position.x = axisA;
                    position.z = axisB;
                    break;
                case Plane.YZ:
                    position.y = axisA;
                    position.z = axisB;
                    break;
            }
        }

        private static bool TryEstimateGridTransform(Grid3D grid, Vector2Int size, Plane plane, out Vector2 origin, out Vector2 stepX, out Vector2 stepY)
        {
            origin = Vector2.zero;
            stepX = Vector2.zero;
            stepY = Vector2.zero;

            List<(Vector2Int coord, Vector2 pos)> samples = new List<(Vector2Int, Vector2)>();

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    GridCellController tile = grid.GetCellControllerAt(new Vector2Int(x, y));
                    if (tile != null)
                        samples.Add((new Vector2Int(x, y), ProjectToPlane(tile.transform.position, plane)));
                }
            }

            if (samples.Count == 0)
                return false;

            int xCount = 0;
            int yCount = 0;

            for (int i = 0; i < samples.Count; i++)
            {
                for (int j = i + 1; j < samples.Count; j++)
                {
                    Vector2Int aCoord = samples[i].coord;
                    Vector2Int bCoord = samples[j].coord;
                    Vector2 aPos = samples[i].pos;
                    Vector2 bPos = samples[j].pos;

                    if (aCoord.y == bCoord.y && aCoord.x != bCoord.x)
                    {
                        float dx = bCoord.x - aCoord.x;
                        stepX += (bPos - aPos) / dx;
                        xCount++;
                    }

                    if (aCoord.x == bCoord.x && aCoord.y != bCoord.y)
                    {
                        float dy = bCoord.y - aCoord.y;
                        stepY += (bPos - aPos) / dy;
                        yCount++;
                    }
                }
            }

            if (xCount == 0 || yCount == 0)
                return false;

            stepX /= xCount;
            stepY /= yCount;

            Vector2 originSum = Vector2.zero;
            for (int i = 0; i < samples.Count; i++)
            {
                Vector2Int c = samples[i].coord;
                originSum += samples[i].pos - (stepX * c.x) - (stepY * c.y);
            }

            origin = originSum / samples.Count;
            return true;
        }

        private static Vector2 ProjectToPlane(Vector3 position, Plane plane)
        {
            switch (plane)
            {
                case Plane.XY:
                    return new Vector2(position.x, position.y);
                case Plane.XZ:
                    return new Vector2(position.x, position.z);
                case Plane.YZ:
                    return new Vector2(position.y, position.z);
                default:
                    return new Vector2(position.x, position.y);
            }
        }
    }
}