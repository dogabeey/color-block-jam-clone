using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game
{
    [CreateAssetMenu(fileName = "ElementData", menuName = "Game/Element Data")]
    public class ElementData : ScriptableObject
    {
        public Sprite elementSprite;
        public Mesh elementMesh;
        public Material elementMaterial;
    }
}