using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    public class LevelScene : MonoBehaviour
    {
        [InlineEditor]
        public LevelData levelData;
        public GameEvent winTrigger, loseTrigger;
        [Header("Settings")]
        public int timerSeconds = 60;
        [ReadOnly] public float remainingTime = 0;
        [Header("Prefabs")]
        [AssetsOnly]
        public GridCellController gridCellPrefab;
        [AssetsOnly]
        public GridElement gridElementPrefab;
        [AssetsOnly]
        public ExitGateController exitGatePrefab;
        [Header("References")]
        public Grid3D grid3D;

        private bool isWin, isLose, isEnded;

        private void Start()
        {
            BuildGridFromLevelData();
        }

        public IEnumerator StartLevelTimer()
        {
            remainingTime = timerSeconds;
            while (remainingTime > 0f)
            {
                yield return new WaitForSeconds(1f);
                remainingTime -= 1;
                EventManager.TriggerEvent(GameEvent.LEVEL_TIMER_TICK);
            }
            EventManager.TriggerEvent(GameEvent.LEVEL_TIMER_EXPIRE);
            Lose();
        }

        private void BuildGridFromLevelData()
        {
            if (levelData == null || grid3D == null || gridCellPrefab == null)
            {
                return;
            }

            CellData[,] layout = levelData.gridLayout;
            if (layout == null)
            {
                grid3D.gridCellControllers = new GridCellController[0, 0];
                return;
            }

            int width = layout.GetLength(0);
            int height = layout.GetLength(1);

            grid3D.gridCellControllers = new GridCellController[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellData cellData = layout[x, y];
                    Vector2Int position = new Vector2Int(x, y);
                    bool isEdgeCell = Grid3D.IsEdgeCoordinate(position, width, height);
                    bool isCornerCell = (x == 0 || x == width - 1) && (y == 0 || y == height - 1);
                    ElementData exitElement = isEdgeCell && !isCornerCell && cellData != null ? cellData.currentElement : null;
                    CellType runtimeCellType = isEdgeCell
                        ? CellType.Wall
                        : (cellData != null ? cellData.cellType : CellType.Empty);

                    GridCellController cell = Instantiate(gridCellPrefab, grid3D.transform);
                    cell.transform.localPosition = new Vector3(x, 0f, y);
                    cell.Configure(position, runtimeCellType, isEdgeCell, exitElement);
                    cell.exitGate = null;

                    if (isEdgeCell && !isCornerCell && exitGatePrefab != null)
                    {
                        ExitGateController exitGate = Instantiate(exitGatePrefab, cell.transform);
                        exitGate.transform.localPosition = Vector3.zero;

                        Vector3 exitDirection = GetExitDirection(position, width, height);
                        if (exitDirection != Vector3.zero)
                        {
                            exitGate.transform.rotation = Quaternion.LookRotation(grid3D.transform.TransformDirection(exitDirection), grid3D.transform.up);
                        }

                        exitGate.Init(exitElement);
                        cell.exitGate = exitGate;
                    }

                    grid3D.gridCellControllers[x, y] = cell;

                    if (!isEdgeCell && cellData != null && cellData.cellType == CellType.Empty && cellData.currentElement != null && gridElementPrefab != null)
                    {
                        GridElement element = Instantiate(gridElementPrefab, cell.transform);
                        element.transform.localPosition = Vector3.zero;
                        element.elementData = cellData.currentElement;
                        element.groupIndex = cellData.elementGroupIndex;
                        element.movementRestriction = cellData.movementRestriction;
                        element.currentCell = cell;
                        element.Init();
                        cell.currentElement = element;
                    }
                }
            }

            EventManager.TriggerEvent(GameEvent.GRID_INITIALIZED);
        }

        private static Vector3 GetExitDirection(Vector2Int position, int width, int height)
        {
            if (position.x == 0)
            {
                return Vector3.left;
            }

            if (position.x == width - 1)
            {
                return Vector3.right;
            }

            if (position.y == 0)
            {
                return Vector3.back;
            }

            if (position.y == height - 1)
            {
                return Vector3.forward;
            }

            return Vector3.zero;
        }

        private void OnEnable()
        {
            EventManager.StartListening(winTrigger, OnWinTriggered);
            EventManager.StartListening(loseTrigger, OnLoseTriggered);
        }
        private void OnDisable()
        {
            EventManager.StopListening(winTrigger, OnWinTriggered);
            EventManager.StopListening(loseTrigger, OnLoseTriggered);
        }
        private void OnWinTriggered(EventParam param)
        {
            Win();
        }
        private void OnLoseTriggered(EventParam param)
        {
            Lose();
        }

        private void Update()
        {
            if(isEnded) return;

            if(isWin)
            {
                isEnded = true;
                GameManager.Instance.screenManager.Show(ScreenID.WinScreen);
            }
            else if(isLose)
            {
                isEnded = true;
                GameManager.Instance.screenManager.Show(ScreenID.LoseScreen);
            }
        }

        public void Win()
        {
            isWin = true;
        }

        public void Lose()
        {
            isLose = true;
        }
    }
}
