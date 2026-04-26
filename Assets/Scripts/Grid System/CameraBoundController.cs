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

            MonoBehaviour[] components = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

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

            float centerA = (MinBounds.x + MaxBounds.x) * 0.5f;
            float centerB = (MinBounds.y + MaxBounds.y) * 0.5f;

            float fixedAxis = GetPlaneFixedAxis(mainCamera.transform.position, plane);
            if (TryGetPlaneFixedAxisValue(plane, out float sampledAxis))
            {
                fixedAxis = sampledAxis;
            }

            Vector3 planeCenter = GetWorldPointOnPlane(plane, centerA, centerB, fixedAxis);
            if (TryGetRayDistanceToPlane(mainCamera.transform.position, mainCamera.transform.forward, plane, fixedAxis, out float rayDistance))
            {
                mainCamera.transform.position = planeCenter - (mainCamera.transform.forward * rayDistance);
            }
            else
            {
                Vector3 cameraPosition = mainCamera.transform.position;
                ApplyPlaneCenterToPosition(ref cameraPosition, plane, centerA, centerB);
                mainCamera.transform.position = cameraPosition;
            }

            if (TryGetPlaneWorldCorners(plane, fixedAxis, MinBounds, MaxBounds, out Vector3 c0, out Vector3 c1, out Vector3 c2, out Vector3 c3))
            {
                Vector3 right = mainCamera.transform.right;
                Vector3 up = mainCamera.transform.up;

                float minRight = Mathf.Min(Vector3.Dot(c0, right), Vector3.Dot(c1, right), Vector3.Dot(c2, right), Vector3.Dot(c3, right));
                float maxRight = Mathf.Max(Vector3.Dot(c0, right), Vector3.Dot(c1, right), Vector3.Dot(c2, right), Vector3.Dot(c3, right));
                float minUp = Mathf.Min(Vector3.Dot(c0, up), Vector3.Dot(c1, up), Vector3.Dot(c2, up), Vector3.Dot(c3, up));
                float maxUp = Mathf.Max(Vector3.Dot(c0, up), Vector3.Dot(c1, up), Vector3.Dot(c2, up), Vector3.Dot(c3, up));

                float projectedWidth = Mathf.Max(0.0001f, maxRight - minRight);
                float projectedHeight = Mathf.Max(0.0001f, maxUp - minUp);
                float aspect = Mathf.Max(0.0001f, mainCamera.aspect);

                float sizeFromHeight = projectedHeight * 0.5f;
                float sizeFromWidth = (projectedWidth * 0.5f) / aspect;

                mainCamera.orthographicSize = Mathf.Max(sizeFromHeight, sizeFromWidth);
            }
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

        private static float GetPlaneFixedAxis(Vector3 position, Plane plane)
        {
            switch (plane)
            {
                case Plane.XY:
                    return position.z;
                case Plane.XZ:
                    return position.y;
                case Plane.YZ:
                    return position.x;
                default:
                    return position.z;
            }
        }

        private static bool TryGetPlaneFixedAxisValue(Plane plane, out float axisValue)
        {
            axisValue = 0f;

            LevelScene level = GameManager.Instance.currentLoadedLevel;
            if (level == null || level.grid3D == null || level.grid3D.gridCellControllers == null)
                return false;

            GridCellController[,] cells = level.grid3D.gridCellControllers;
            int width = cells.GetLength(0);
            int height = cells.GetLength(1);

            float sum = 0f;
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCellController cell = cells[x, y];
                    if (cell == null)
                        continue;

                    sum += GetPlaneFixedAxis(cell.transform.position, plane);
                    count++;
                }
            }

            if (count == 0)
                return false;

            axisValue = sum / count;
            return true;
        }

        private static Vector3 GetWorldPointOnPlane(Plane plane, float axisA, float axisB, float fixedAxis)
        {
            switch (plane)
            {
                case Plane.XY:
                    return new Vector3(axisA, axisB, fixedAxis);
                case Plane.XZ:
                    return new Vector3(axisA, fixedAxis, axisB);
                case Plane.YZ:
                    return new Vector3(fixedAxis, axisA, axisB);
                default:
                    return new Vector3(axisA, axisB, fixedAxis);
            }
        }

        private static Vector3 GetPlaneNormal(Plane plane)
        {
            switch (plane)
            {
                case Plane.XY:
                    return Vector3.forward;
                case Plane.XZ:
                    return Vector3.up;
                case Plane.YZ:
                    return Vector3.right;
                default:
                    return Vector3.forward;
            }
        }

        private static bool TryGetRayDistanceToPlane(Vector3 rayOrigin, Vector3 rayDirection, Plane plane, float fixedAxis, out float distance)
        {
            distance = 0f;

            Vector3 normal = GetPlaneNormal(plane);
            float denominator = Vector3.Dot(rayDirection, normal);
            if (Mathf.Abs(denominator) < 0.00001f)
                return false;

            Vector3 planePoint = GetWorldPointOnPlane(plane, 0f, 0f, fixedAxis);
            distance = Vector3.Dot(planePoint - rayOrigin, normal) / denominator;
            return true;
        }

        private static bool TryGetPlaneWorldCorners(Plane plane, float fixedAxis, Vector2 min, Vector2 max, out Vector3 c0, out Vector3 c1, out Vector3 c2, out Vector3 c3)
        {
            c0 = GetWorldPointOnPlane(plane, min.x, min.y, fixedAxis);
            c1 = GetWorldPointOnPlane(plane, max.x, min.y, fixedAxis);
            c2 = GetWorldPointOnPlane(plane, max.x, max.y, fixedAxis);
            c3 = GetWorldPointOnPlane(plane, min.x, max.y, fixedAxis);
            return true;
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