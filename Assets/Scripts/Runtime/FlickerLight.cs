using UnityEngine;

namespace RealmShift
{
    /// <summary>Cheap intensity wobble so the flickering hall reads as flickering. Not gameplay.</summary>
    public class FlickerLight : MonoBehaviour
    {
        public float baseIntensity = 2500f;
        public float amount = 0.28f;
        public float speed = 5.5f;
        Light lamp;
        float seed;

        void Awake()
        {
            lamp = GetComponent<Light>();
            seed = Random.value * 20f;
        }

        void Update()
        {
            if (lamp == null)
                return;
            float n = Mathf.PerlinNoise(seed, Time.time * speed);
            lamp.intensity = baseIntensity * Mathf.Lerp(1f - amount, 1f + amount, n);
        }
    }
}
