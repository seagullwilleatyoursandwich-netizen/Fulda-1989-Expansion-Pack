using GHPC.Weapons;
using Reticle;
using UnityEngine;

namespace Fulda1989
{
    public class UpdateVerticalRangeScale : MonoBehaviour
    { 
        public FireControlSystem fcs;
        public ReticleMesh reticle;

        void Update()
        {
            reticle.CurrentAmmo = fcs.CurrentAmmoType;

            if (reticle.curReticleRange != fcs.CurrentRange)
            {
                reticle.targetReticleRange = fcs.CurrentRange;
            }
        }
    }
}
