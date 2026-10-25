using UnityEngine;

namespace RealmShift
{
    /// <summary>Named standing spot. "default" is where Play starts. Portals can request another id.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public string spawnId = "default";
    }
}
