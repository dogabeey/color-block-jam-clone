using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game.GridSystem
{
    [CreateAssetMenu(fileName = "ElementData", menuName = "Scriptable Objects/ElementData")]
    public class ElementData : ScriptableObject
    {
        public Sprite elementSprite;
        public Mesh elementMesh;
        public Material elementMaterial;
    }
}