using UnityEngine;

namespace Fulda1989
{
    internal class EraVisuals : MonoBehaviour
    {
        public MeshRenderer visual;
        public Material destroyed_mat;
        public bool hide_on_detonate = true;
        public string destroyed_target;
    }
}
