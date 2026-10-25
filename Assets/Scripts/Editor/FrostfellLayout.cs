using UnityEngine;

namespace RealmShift.EditorTools
{
    /// <summary>
    /// Meters, y-up, flat snow at y = 0 inside a terrain bowl about 240 m across.
    /// Tweak these and run RealmShift &gt; Build Frostfell Fair (or Build All).
    /// Image 08 (the postcard) puts the wheel on the left, the carousel in the middle, and the maze on the right.
    /// </summary>
    public static class FrostfellLayout
    {
        public static float TerrainSize = 240f;
        public static float TerrainHeight = 36f;
        public static float FlatRadius = 96f;

        public static Vector3 Spawn = new Vector3(0f, 0.45f, -80f);
        public static Vector3 Gate = new Vector3(0f, 0f, -70f);
        public static Vector3 Exit = new Vector3(12.5f, 0f, -74f);

        public static Vector3 Carousel = new Vector3(0f, 0f, -16f);
        public static float CarouselRadius = 6f;
        public static float IceRingOuter = 10.2f;
        public static float IceRingWidth = 2.8f;

        public static Vector3 Ferris = new Vector3(-50f, 0f, -8f);
        public static float FerrisRadius = 13f;
        public static float FerrisAxleY = 15.2f;

        public static Vector3 MazeOrigin = new Vector3(40f, 0f, 8f);
        public static int MazeCellsX = 10;
        public static int MazeCellsZ = 10;
        public static float MazeCell = 3f;
        public static float MazeWallHeight = 2.5f;

        public static Vector3 Stage = new Vector3(20f, 0f, -50f);
        public static Vector3 Rink = new Vector3(50f, 0f, -36f);
        public static float RinkRadius = 12.5f;

        public static Vector3 Fire = new Vector3(-16f, 0f, 46f);

        public static Vector3[] Luge =
        {
            new Vector3(-30f, 0.3f, 18f),
            new Vector3(-44f, 1.3f, 30f),
            new Vector3(-58f, 2.6f, 44f),
            new Vector3(-64f, 3.4f, 58f),
            new Vector3(-48f, 2.1f, 66f)
        };
    }
}
