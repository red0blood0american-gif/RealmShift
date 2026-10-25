using UnityEngine;

namespace RealmShift
{
    /// <summary>Walk-in volume that loads another scene and requests a spawn id there.</summary>
    public class ScenePortal : MonoBehaviour
    {
        public string sceneName;
        public string spawnId = "default";
        static bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            busy = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AllowNextPortal()
        {
            busy = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (busy || string.IsNullOrEmpty(sceneName))
                return;
            if (other.GetComponentInParent<PlayerController>() == null)
                return;
            busy = true;
            PlayerSession.NextSpawnId = string.IsNullOrEmpty(spawnId) ? "default" : spawnId;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
