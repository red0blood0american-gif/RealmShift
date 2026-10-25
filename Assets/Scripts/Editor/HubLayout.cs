using UnityEngine;

namespace RealmShift.EditorTools
{
    /// <summary>
    /// Meters, y-up. Tweak these and run RealmShift &gt; Build Hub (or Build All).
    /// The site is about 156 by 158 m. Floors sit at y = 0.
    /// </summary>
    public static class HubLayout
    {
        public static float SiteMinX = -78f;
        public static float SiteMaxX = 78f;
        public static float SiteMinZ = -62f;
        public static float SiteMaxZ = 96f;

        public static Vector3 Spawn = new Vector3(0f, 0.4f, -46f);
        public static Vector3 ReturnSpawn = new Vector3(3.2f, 0.4f, 20f);
        public static float ReturnYaw = -90f;

        public static float HallHalfWidth = 8f;
        public static float HallMinZ = 14f;
        public static float HallMaxZ = 56f;
        public static float DoorWidth = 3.4f;
        public static float DoorHeight = 3.5f;
        public static float CeilingY = 5.8f;
        public static float[] DoorZ = { 20f, 30f, 40f, 50f };

        public static Vector3 IceTower = new Vector3(50f, 0f, -14f);
        public static float IceTowerHeight = 36f;
        public static Vector3 GearTower = new Vector3(-48f, 0f, -22f);
        public static float GearTowerHeight = 32f;
        public static Vector3 GreenTower = new Vector3(20f, 0f, -54f);
        public static float GreenTowerHeight = 28f;

        public static Vector3 Pond = new Vector3(-32f, 0f, -36f);
        public static float PondRadius = 8.5f;

        public static Vector3 TrueBodies = new Vector3(0f, 0f, 82f);
        public static float TrueBodiesRadius = 12f;
        public static float DaisRadius = 4f;
    }
}
