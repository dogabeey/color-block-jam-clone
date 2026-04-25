using UnityEngine;

namespace Game
{
    public class GridCellController : MonoBehaviour, ICameraBoundSetter
    {
        public Vector2 CameraBound => new Vector2(transform.position.x, transform.position.z);

        [Header("Visual")]
        [SerializeField] private Renderer cellRenderer;
        [SerializeField] private Color wallColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color emptyColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        public Vector2Int gridPosition;
        public CellType cellType;
        public bool isExitCell;
        public ElementData exitElementData;
        public ExitGateController exitGate;
        public GridElement currentElement;

        private void Awake()
        {
            if (cellRenderer == null)
            {
                cellRenderer = GetComponentInChildren<Renderer>();
            }
        }

        public void Configure(Vector2Int position, CellType type, bool isExit, ElementData exitData)
        {
            gridPosition = position;
            cellType = type;
            isExitCell = isExit;
            exitElementData = exitData;
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (cellRenderer == null)
            {
                return;
            }

            // Edge cells are logical walls/exits only; keep the tile mesh hidden.
            if (isExitCell)
            {
                cellRenderer.enabled = false;
                return;
            }

            cellRenderer.enabled = true;

            Color targetColor = cellType == CellType.Wall ? wallColor : emptyColor;

            cellRenderer.material.color = targetColor;
        }
    }
}