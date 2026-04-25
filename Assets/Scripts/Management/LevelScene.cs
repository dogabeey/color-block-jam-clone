using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    public class LevelScene : MonoBehaviour
    {
        [InlineEditor]
        public LevelData levelData;
        public GameEvent winTrigger, loseTrigger;
        [Header("Prefabs")]
        [AssetsOnly]
        public GridCellController gridCellPrefab;
        [AssetsOnly]
        public GridElement gridElementPrefab;
        [Header("References")]
        public Grid3D grid3D;

        private bool isWin, isLose, isEnded;

        private void Start()
        {
            BuildGridFromLevelData();
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

                    GridCellController cell = Instantiate(gridCellPrefab, grid3D.transform);
                    cell.transform.localPosition = new Vector3(x, 0f, y);
                    cell.gridPosition = new Vector2Int(x, y);
                    cell.cellType = cellData != null ? cellData.cellType : CellType.Empty;
                    grid3D.gridCellControllers[x, y] = cell;

                    if (cellData != null && cellData.cellType == CellType.Empty && cellData.currentElement != null && gridElementPrefab != null)
                    {
                        GridElement element = Instantiate(gridElementPrefab, cell.transform);
                        element.transform.localPosition = Vector3.zero;
                        element.elementData = cellData.currentElement;
                        element.groupIndex = cellData.elementGroupIndex;
                        element.currentCell = cell;
                        element.Init();
                        cell.currentElement = element;
                    }
                }
            }

            EventManager.TriggerEvent(GameEvent.GRID_INITIALIZED);
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
