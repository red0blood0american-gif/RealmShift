using UnityEngine;

namespace RealmShift
{
    /// <summary>Ordered waypoint used by the blockout walk tests. Not a gameplay object.</summary>
    public class PathMarker : MonoBehaviour
    {
        public string pathId = "hub";
        public int order;
        public string label;
    }
}
