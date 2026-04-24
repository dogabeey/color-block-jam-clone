using UnityEngine;

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