using UnityEngine;

namespace RealmShift
{
    /// <summary>Camera pose for blockout screenshots. Transform is the camera position.</summary>
    public class ShotMarker : MonoBehaviour
    {
        public string shotId;
        public Vector3 lookAt;
        public float fieldOfView = 55f;
    }
}
