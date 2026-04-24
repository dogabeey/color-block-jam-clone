using UnityEngine;

namespace Game
{
    public class GridCellController : MonoBehaviour, ICameraBoundSetter
    {
        public Vector2 CameraBound => new Vector2(transform.position.x, transform.position.z);

        public GridElement currentElement;
    }
}