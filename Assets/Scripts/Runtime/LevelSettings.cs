using UnityEngine;

namespace RealmShift
{
    /// <summary>Per-scene safety settings. The player respawns when they fall below killHeight.</summary>
    public class LevelSettings : MonoBehaviour
    {
        public float killHeight = -12f;
    }
}
