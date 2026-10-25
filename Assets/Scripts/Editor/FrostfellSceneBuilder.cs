using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealmShift.EditorTools
{
    /// <summary>
    /// Snowy fair inside a terrain bowl. Image 05 (the night carnival) is not built.
    /// Image 08 is the overview: wheel on the left, carousel in the middle, maze on the right.
    /// </summary>
    public static class FrostfellSceneBuilder
    {
        public static void Build()
        {
            BlockoutFactory.EnsureFolder("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = BlockoutFactory.Group(null, "FrostfellFair");
            var level = root.gameObject.AddComponent<LevelSettings>();
            level.killHeight = -8f;

            var snow = BlockoutFactory.Mat("Snow", new Color(0.90f, 0.93f, 0.97f), 0.08f);
            var ice = BlockoutFactory.Mat("Ice", new Color(0.62f, 0.86f, 0.96f), 0.82f, 0.02f);
            var wood = BlockoutFactory.Mat("FairWood", new Color(0.52f, 0.34f, 0.18f), 0.18f);
            var darkWood = BlockoutFactory.Mat("DarkWood", new Color(0.32f, 0.2f, 0.12f), 0.16f);
            var tentRed = BlockoutFactory.Mat("TentRed", new Color(0.72f, 0.16f, 0.18f), 0.12f);
            var tentBlue = BlockoutFactory.Mat("TentBlue", new Color(0.18f, 0.34f, 0.68f), 0.12f);
            var tentGold = BlockoutFactory.Mat("TentGold", new Color(0.78f, 0.58f, 0.16f), 0.2f);
            var stripe = BlockoutFactory.Mat("Stripe", new Color(0.86f, 0.88f, 0.92f), 0.15f);
            var iron = BlockoutFactory.Mat("Iron", new Color(0.35f, 0.4f, 0.46f), 0.4f, 0.55f);
            var pine = BlockoutFactory.Mat("Pine", new Color(0.16f, 0.38f, 0.26f), 0.1f);
            var warm = BlockoutFactory.Mat("WarmLight", new Color(1f, 0.72f, 0.35f), 0.3f, 0f, new Color(1.4f, 0.55f, 0.15f));

            BuildTerrain(root);
            BuildApproach(root, wood, darkWood, tentRed, tentBlue, tentGold, stripe, snow);
            BuildCarousel(root, wood, tentRed, tentGold, ice, stripe, warm);
            BuildFerris(root, iron, tentRed, stripe, wood);
            BuildLuge(root, ice, wood);
            BuildMaze(root, ice, snow);
            BuildStage(root, wood, tentRed, stripe, darkWood);
            BuildRink(root, ice, wood);
            BuildGathering(root, wood, tentRed, tentBlue, tentGold, stripe, warm, pine);
            BuildTrees(root, pine, snow);
            BuildLighting(root);
            BuildRoutes(root);

            var terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null)
                terrain.Flush();
            Physics.SyncTransforms();

            BlockoutFactory.ValidatePath("frostfell");
            int layer = RealmShiftBuild.PlayerLayer;
            var player = BlockoutFactory.CreatePlayer(FrostfellLayout.Spawn, Quaternion.identity, layer);
            player.transform.SetParent(root, true);
            BlockoutFactory.CreateCamera(player.transform, layer);

            EditorSceneManager.SaveScene(scene, RealmShiftScenes.FrostfellPath);
            Debug.Log("RealmShift: built " + RealmShiftScenes.FrostfellPath);
        }

        public static float BowlHeight(float x, float z)
        {
            float d = Mathf.Sqrt(x * x + z * z);
            float edge = FrostfellLayout.FlatRadius;
            float h = 0f;
            if (d >= edge)
            {
                float gentle = edge + 16f;
                if (d < gentle)
                {
                    float t = (d - edge) / 16f;
                    h = t * t * 5f;
                }
                else
                {
                    h = 5f + (d - gentle) * 1.8f;
                }
            }

            // Ridges only, so the bowl never gains a pit inside the fair.
            if (d > 100f)
            {
                float n = Mathf.PerlinNoise(x * 0.037f + 19f, z * 0.037f + 7f);
                h += Mathf.Max(0f, n - 0.4f) * 3.2f;
            }

            return Mathf.Min(h, FrostfellLayout.TerrainHeight - 1.5f);
        }

        static void BuildTerrain(Transform root)
        {
            float spawnH = BowlHeight(FrostfellLayout.Spawn.x, FrostfellLayout.Spawn.z);
            if (spawnH > 0.02f)
                throw new System.InvalidOperationException("Frostfell spawn is not on the flat floor (height " + spawnH + ").");

            const int res = 257;
            float size = FrostfellLayout.TerrainSize;
            float half = size * 0.5f;
            var heights = new float[res, res];
            for (int z = 0; z < res; z++)
            {
                float wz = -half + size * z / (res - 1f);
                for (int x = 0; x < res; x++)
                {
                    float wx = -half + size * x / (res - 1f);
                    heights[z, x] = BowlHeight(wx, wz) / FrostfellLayout.TerrainHeight;
                }
            }

            BlockoutFactory.EnsureFolder("Assets/Terrain");
            BlockoutFactory.EnsureFolder("Assets/Textures");
            const string dataPath = "Assets/Terrain/FrostfellBowl.asset";
            const string layerPath = "Assets/Terrain/Snow.terrainlayer";
            const string texPath = "Assets/Textures/Snow.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(dataPath) != null)
                AssetDatabase.DeleteAsset(dataPath);
            if (AssetDatabase.LoadAssetAtPath<Object>(layerPath) != null)
                AssetDatabase.DeleteAsset(layerPath);

            var data = new TerrainData();
            data.heightmapResolution = res;
            data.size = new Vector3(size, FrostfellLayout.TerrainHeight, size);
            data.baseMapResolution = 256;
            data.alphamapResolution = 128;
            data.SetDetailResolution(0, 8);
            AssetDatabase.CreateAsset(data, dataPath);
            data.SetHeights(0, 0, heights);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(0.9f, 0.93f, 0.97f);
                tex.SetPixels(pixels);
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.Apply();
                AssetDatabase.CreateAsset(tex, texPath);
            }

            var layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(12f, 12f), name = "Snow" };
            AssetDatabase.CreateAsset(layer, layerPath);
            data.terrainLayers = new[] { layer };
            EditorUtility.SetDirty(data);

            var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (shader == null)
                throw new System.InvalidOperationException("URP Terrain/Lit shader was not found.");
            BlockoutFactory.EnsureFolder("Assets/Materials/Blockout");
            const string matPath = "Assets/Materials/Blockout/TerrainSnow.mat";
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (terrainMat == null)
            {
                terrainMat = new Material(shader) { name = "TerrainSnow" };
                AssetDatabase.CreateAsset(terrainMat, matPath);
            }
            else
            {
                terrainMat.shader = shader;
            }

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "MountainBowl";
            terrainGo.transform.SetParent(root, true);
            terrainGo.transform.position = new Vector3(-half, 0f, -half);
            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMat;
            terrain.drawInstanced = false;
            terrain.basemapDistance = 400f;
            terrain.heightmapPixelError = 6f;
            terrain.Flush();
            var collider = terrainGo.GetComponent<TerrainCollider>();
            collider.terrainData = data;
        }

        static void BuildApproach(Transform root, Material wood, Material darkWood, Material tentRed, Material tentBlue, Material tentGold, Material stripe, Material snow)
        {
            var area = BlockoutFactory.Group(root, "FairgroundApproach");
            Vector3 gate = FrostfellLayout.Gate;
            BlockoutFactory.Cube(area, "GatePostL", gate + new Vector3(-4.3f, 2.3f, 0f), new Vector3(1.15f, 4.6f, 1.15f), darkWood);
            BlockoutFactory.Cube(area, "GatePostR", gate + new Vector3(4.3f, 2.3f, 0f), new Vector3(1.15f, 4.6f, 1.15f), darkWood);
            BlockoutFactory.Cube(area, "GateBeam", gate + new Vector3(0f, 4.7f, 0f), new Vector3(9.6f, 0.45f, 1.2f), darkWood);
            BlockoutFactory.Arch(area, "GateArch", gate, 4.4f, 0.55f, 1.3f, wood, 0f, false);
            BlockoutFactory.Sign(area, "FROSTFELL FAIR", gate + new Vector3(0f, 6.1f, 0.2f), Quaternion.LookRotation(Vector3.back), 8f, new Color(0.12f, 0.16f, 0.28f), new Color(0.85f, 0.94f, 1f));

            // Exit sits beside the gate, off the center walk back to the carousel.
            Vector3 exit = FrostfellLayout.Exit;
            BlockoutFactory.Cube(area, "ExitPostL", exit + new Vector3(-1.7f, 1.8f, 0f), new Vector3(0.7f, 3.6f, 0.7f), wood);
            BlockoutFactory.Cube(area, "ExitPostR", exit + new Vector3(1.7f, 1.8f, 0f), new Vector3(0.7f, 3.6f, 0.7f), wood);
            BlockoutFactory.Sign(area, "TO PALACE", exit + new Vector3(0f, 3.5f, 0.4f), Quaternion.LookRotation(Vector3.back), 3.2f, new Color(0.12f, 0.1f, 0.16f), Color.white);
            BlockoutFactory.Portal(area, "ExitPortal", exit + Vector3.up * 1.6f, new Vector3(2.2f, 3.2f, 2.2f), RealmShiftScenes.Hub, "from-frostfell");

            Tent(area, new Vector3(-16f, 0f, -78f), 20f, tentRed, stripe, wood);
            Tent(area, new Vector3(18f, 0f, -84f), -15f, tentBlue, stripe, wood);
            Tent(area, new Vector3(-18f, 0f, -62f), 10f, tentGold, stripe, wood);
            Tent(area, new Vector3(30f, 0f, -80f), -8f, tentRed, stripe, wood);
            Tent(area, new Vector3(-22f, 0f, -48f), 25f, tentBlue, stripe, wood);
            BlockoutFactory.Cube(area, "TicketBooth", new Vector3(8.5f, 1.1f, -60f), new Vector3(3.2f, 2.2f, 2.4f), wood);
            BlockoutFactory.Sign(area, "TICKETS", new Vector3(8.5f, 2.7f, -58.6f), Quaternion.LookRotation(Vector3.back), 2.4f, tentRed.color, Color.white);

            for (int i = 0; i < 4; i++)
            {
                float z = -66f + i * 8f;
                BlockoutFactory.Cylinder(area, "MidLampL", new Vector3(-8f, 2f, z), 0.12f, 4f, wood);
                BlockoutFactory.Cylinder(area, "MidLampR", new Vector3(8f, 2f, z), 0.12f, 4f, wood);
                BlockoutFactory.Sphere(area, "MidGlowL", new Vector3(-8f, 4.15f, z), 0.22f, snow, false);
                BlockoutFactory.Sphere(area, "MidGlowR", new Vector3(8f, 4.15f, z), 0.22f, snow, false);
            }
        }

        static void Tent(Transform parent, Vector3 pos, float yaw, Material cloth, Material stripe, Material wood)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            BlockoutFactory.Cube(parent, "TentBody", pos + Vector3.up * 1.15f, rot, new Vector3(4.2f, 2.3f, 3.4f), cloth);
            BlockoutFactory.Cube(parent, "TentStripe", pos + Vector3.up * 1.7f, rot, new Vector3(4.3f, 0.35f, 3.5f), stripe);
            BlockoutFactory.Cone(parent, "TentRoof", pos + Vector3.up * 2.3f, 2.7f, 1.8f, cloth, 8, true);
            BlockoutFactory.Cube(parent, "TentPole", pos + rot * new Vector3(0f, 1.2f, 1.8f), new Vector3(0.15f, 2.4f, 0.15f), wood);
        }

        static void BuildCarousel(Transform root, Material wood, Material roof, Material gold, Material ice, Material stripe, Material warm)
        {
            var area = BlockoutFactory.Group(root, "Carousel");
            Vector3 c = FrostfellLayout.Carousel;
            var platform = BlockoutFactory.Cylinder(area, "CarouselPlatform", c + Vector3.up * 0.16f, FrostfellLayout.CarouselRadius, 0.32f, wood);
            MarkIce(platform);
            BlockoutFactory.Cylinder(area, "CarouselPole", c + Vector3.up * 3.2f, 0.35f, 6.4f, gold);
            BlockoutFactory.Cone(area, "CarouselRoof", c + Vector3.up * 5.6f, 7.2f, 2.6f, roof, 14, true);
            BlockoutFactory.Torus(area, "CarouselTrim", c + Vector3.up * 5.5f, Quaternion.identity, 6.4f, 0.22f, gold, 20, false);

            var horse = BlockoutFactory.Mat("Horse", new Color(0.85f, 0.82f, 0.75f), 0.2f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 pos = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 4.1f;
                BlockoutFactory.Cylinder(area, "HorsePole", pos + Vector3.up * 2.4f, 0.08f, 4.2f, stripe);
                BlockoutFactory.Sphere(area, "HorseBody", pos + Vector3.up * (1.15f + (i % 2) * 0.25f), 0.45f, horse);
                BlockoutFactory.Sphere(area, "HorseHead", pos + new Vector3(Mathf.Cos(a) * 0.35f, 1.55f, Mathf.Sin(a) * 0.35f), 0.22f, horse);
            }

            var ring = BlockoutFactory.Pipe(area, "IceRing", c + Vector3.up * 0.12f, FrostfellLayout.IceRingOuter, 0.22f, FrostfellLayout.IceRingWidth, ice, true);
            MarkIce(ring);
            BlockoutFactory.Sign(area, "CAROUSEL", c + new Vector3(8f, 3.4f, -11.5f), Quaternion.LookRotation(Vector3.back), 4f, new Color(0.15f, 0.18f, 0.28f), Color.white);
            BlockoutFactory.Lamp(area, "CarouselLamp", c + Vector3.up * 4.2f, new Color(1f, 0.86f, 0.65f), 3500f, 16f, false);
            BlockoutFactory.Sphere(area, "CarouselGlow", c + Vector3.up * 4.2f, 0.25f, warm, false);
        }

        static void BuildFerris(Transform root, Material iron, Material red, Material cream, Material wood)
        {
            var area = BlockoutFactory.Group(root, "FerrisWheel");
            Vector3 c = FrostfellLayout.Ferris;
            float axle = FrostfellLayout.FerrisAxleY;
            float radius = FrostfellLayout.FerrisRadius;
            BlockoutFactory.Cube(area, "WheelDeck", c + new Vector3(0f, 0.12f, 0f), new Vector3(10f, 0.24f, 8f), wood);
            BlockoutFactory.Torus(area, "Wheel", c + Vector3.up * axle, Quaternion.Euler(90f, 0f, 0f), radius, 0.42f, iron, 28, false);
            BlockoutFactory.Torus(area, "WheelInner", c + Vector3.up * axle, Quaternion.Euler(90f, 0f, 0f), radius * 0.55f, 0.18f, red, 20, false);
            BlockoutFactory.Cube(area, "Axle", c + Vector3.up * axle, new Vector3(0.55f, 0.55f, 3.4f), iron);

            Vector3 axlePos = c + Vector3.up * axle;
            BlockoutFactory.Beam(area, "AFrameL", c + new Vector3(-4.5f, 0.2f, -3.2f), axlePos + new Vector3(0f, 0f, -1.4f), 0.45f, iron);
            BlockoutFactory.Beam(area, "AFrameR", c + new Vector3(4.5f, 0.2f, -3.2f), axlePos + new Vector3(0f, 0f, -1.4f), 0.45f, iron);
            BlockoutFactory.Beam(area, "AFrameL2", c + new Vector3(-4.5f, 0.2f, 3.2f), axlePos + new Vector3(0f, 0f, 1.4f), 0.45f, iron);
            BlockoutFactory.Beam(area, "AFrameR2", c + new Vector3(4.5f, 0.2f, 3.2f), axlePos + new Vector3(0f, 0f, 1.4f), 0.45f, iron);
            BlockoutFactory.Cylinder(area, "TowerS", c + new Vector3(0f, axle * 0.5f, -3.4f), 0.4f, axle, iron);
            BlockoutFactory.Cylinder(area, "TowerN", c + new Vector3(0f, axle * 0.5f, 3.4f), 0.4f, axle, iron);

            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Vector3 pos = c + new Vector3(Mathf.Cos(a) * (radius - 1.3f), axle + Mathf.Sin(a) * (radius - 1.3f), 0f);
                var gondola = BlockoutFactory.Cube(area, "Gondola" + i, pos, new Vector3(1.3f, 1.1f, 1.1f), i % 2 == 0 ? red : cream, false);
                gondola.transform.rotation = Quaternion.identity;
            }

            BlockoutFactory.Sign(area, "FERRIS", c + new Vector3(8f, 3.2f, 0f), Quaternion.LookRotation(Vector3.right), 3.4f, new Color(0.2f, 0.08f, 0.1f), Color.white);
        }

        static void BuildLuge(Transform root, Material ice, Material wood)
        {
            var area = BlockoutFactory.Group(root, "FrozenRides");
            var points = FrostfellLayout.Luge;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var deck = BlockoutFactory.Beam(area, "LugeTrack" + i, points[i], points[i + 1], 2.1f, ice);
                if (deck != null)
                    MarkIce(deck);
            }

            for (int i = 1; i < points.Length; i++)
            {
                Vector3 p = points[i];
                BlockoutFactory.Cylinder(area, "LugeSupport" + i, new Vector3(p.x, p.y * 0.5f, p.z), 0.32f, Mathf.Max(0.4f, p.y), wood);
            }

            BlockoutFactory.Sign(area, "ICE LUGE", points[0] + new Vector3(0f, 2.4f, -2f), Quaternion.LookRotation(Vector3.back), 3.6f, new Color(0.1f, 0.18f, 0.28f), new Color(0.8f, 0.93f, 1f));
        }

        static void BuildMaze(Transform root, Material ice, Material snow)
        {
            var area = BlockoutFactory.Group(root, "IceMaze");
            int w = FrostfellLayout.MazeCellsX;
            int h = FrostfellLayout.MazeCellsZ;
            float cell = FrostfellLayout.MazeCell;
            Vector3 origin = FrostfellLayout.MazeOrigin;
            Carve(w, h, out bool[,] vertical, out bool[,] horizontal);
            vertical[0, 5] = false;

            float thickness = 0.4f;
            float height = FrostfellLayout.MazeWallHeight;
            var floor = BlockoutFactory.Cube(area, "MazeFloor", origin + new Vector3(w * cell * 0.5f, 0.03f, h * cell * 0.5f), new Vector3(w * cell, 0.06f, h * cell), snow);
            MarkIce(floor);

            for (int x = 0; x <= w; x++)
            {
                for (int z = 0; z < h; z++)
                {
                    if (!vertical[x, z])
                        continue;
                    Vector3 pos = origin + new Vector3(x * cell, height * 0.5f, (z + 0.5f) * cell);
                    BlockoutFactory.Cube(area, "MazeWallV", pos, new Vector3(thickness, height, cell + thickness), ice);
                }
            }

            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z <= h; z++)
                {
                    if (!horizontal[x, z])
                        continue;
                    Vector3 pos = origin + new Vector3((x + 0.5f) * cell, height * 0.5f, z * cell);
                    BlockoutFactory.Cube(area, "MazeWallH", pos, new Vector3(cell + thickness, height, thickness), ice);
                }
            }

            Vector3 mouth = origin + new Vector3(-5.5f, 3.6f, 5.5f * cell + 4.2f);
            BlockoutFactory.Sign(area, "ICE MAZE", mouth, Quaternion.LookRotation(Vector3.left), 3.8f, new Color(0.1f, 0.2f, 0.32f), Color.white);
        }

        static void Carve(int w, int h, out bool[,] vertical, out bool[,] horizontal)
        {
            vertical = new bool[w + 1, h];
            horizontal = new bool[w, h + 1];
            for (int x = 0; x <= w; x++)
                for (int z = 0; z < h; z++)
                    vertical[x, z] = true;
            for (int x = 0; x < w; x++)
                for (int z = 0; z <= h; z++)
                    horizontal[x, z] = true;

            var visited = new bool[w, h];
            var stack = new Stack<Vector2Int>();
            var rng = new System.Random(20261025);
            stack.Push(new Vector2Int(0, 5));
            visited[0, 5] = true;
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            while (stack.Count > 0)
            {
                var cell = stack.Peek();
                var options = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    int nx = cell.x + dx[d];
                    int nz = cell.y + dz[d];
                    if (nx >= 0 && nz >= 0 && nx < w && nz < h && !visited[nx, nz])
                        options.Add(d);
                }

                if (options.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                int dir = options[rng.Next(options.Count)];
                int nx2 = cell.x + dx[dir];
                int nz2 = cell.y + dz[dir];
                if (dir == 0)
                    vertical[cell.x + 1, cell.y] = false;
                else if (dir == 1)
                    vertical[cell.x, cell.y] = false;
                else if (dir == 2)
                    horizontal[cell.x, cell.y + 1] = false;
                else
                    horizontal[cell.x, cell.y] = false;
                visited[nx2, nz2] = true;
                stack.Push(new Vector2Int(nx2, nz2));
            }
        }

        static void BuildStage(Transform root, Material wood, Material red, Material stripe, Material darkWood)
        {
            var area = BlockoutFactory.Group(root, "BarkersStage");
            Vector3 c = FrostfellLayout.Stage;
            BlockoutFactory.Cube(area, "StageDeck", c + new Vector3(0f, 0.18f, 0f), new Vector3(10f, 0.36f, 6f), wood);
            BlockoutFactory.Stair(area, "StageSteps", c + new Vector3(0f, 0f, -4.3f), new Vector3(3.4f, 0.36f, 2.4f), 3, wood, 0f);
            BlockoutFactory.Cube(area, "Backdrop", c + new Vector3(0f, 2.2f, 2.7f), new Vector3(10f, 4.2f, 0.35f), red);
            BlockoutFactory.Cube(area, "BackdropStripe", c + new Vector3(0f, 2.6f, 2.9f), new Vector3(10.1f, 0.7f, 0.12f), stripe);
            BlockoutFactory.Cube(area, "Awning", c + new Vector3(0f, 4.15f, 0f), new Vector3(11f, 0.16f, 7f), stripe);
            BlockoutFactory.Cylinder(area, "StagePoleFL", c + new Vector3(-4.4f, 2.1f, -2.5f), 0.12f, 4.2f, darkWood);
            BlockoutFactory.Cylinder(area, "StagePoleFR", c + new Vector3(4.4f, 2.1f, -2.5f), 0.12f, 4.2f, darkWood);
            BlockoutFactory.Cylinder(area, "StagePoleBL", c + new Vector3(-4.4f, 2.1f, 2.4f), 0.12f, 4.2f, darkWood);
            BlockoutFactory.Cylinder(area, "StagePoleBR", c + new Vector3(4.4f, 2.1f, 2.4f), 0.12f, 4.2f, darkWood);
            BlockoutFactory.Sign(area, "BARKER", c + new Vector3(0f, 3.3f, 3.15f), Quaternion.LookRotation(Vector3.back), 4.5f, new Color(0.15f, 0.08f, 0.08f), Color.white);
            BlockoutFactory.Lamp(area, "StageLamp", c + new Vector3(0f, 3.6f, -1f), new Color(1f, 0.78f, 0.5f), 2800f, 14f, true);
        }

        static void BuildRink(Transform root, Material ice, Material wood)
        {
            var area = BlockoutFactory.Group(root, "SkateChannels");
            Vector3 c = FrostfellLayout.Rink;
            var pad = BlockoutFactory.Cylinder(area, "RinkIce", c + Vector3.up * 0.06f, 11f, 0.12f, ice);
            MarkIce(pad);

            // Shallow channels: concentric rings plus a spiral curb, all low enough to step.
            for (int ring = 1; ring <= 3; ring++)
            {
                float radius = 3.2f * ring;
                var channel = BlockoutFactory.Pipe(area, "Channel" + ring, c + Vector3.up * 0.1f, radius, 0.16f, 0.45f, ice, true);
                MarkIce(channel);
            }

            float spiral = 1.2f;
            Vector3 previous = c + new Vector3(spiral, 0.16f, 0f);
            for (int i = 1; i <= 42; i++)
            {
                float t = i / 42f;
                float a = t * Mathf.PI * 4.2f;
                float r = Mathf.Lerp(1.4f, 10.2f, t);
                Vector3 pos = c + new Vector3(Mathf.Cos(a) * r, 0.16f, Mathf.Sin(a) * r);
                var curb = BlockoutFactory.Beam(area, "SpiralCurb", previous, pos, 0.18f, ice);
                if (curb != null)
                    MarkIce(curb);
                previous = pos;
            }

            // Outer wall with a west gap so the walk can reach the entrance.
            int segments = 18;
            for (int i = 0; i < segments; i++)
            {
                float deg = i * 360f / segments;
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 180f)) < 28f)
                    continue;
                float a = deg * Mathf.Deg2Rad;
                Vector3 pos = c + new Vector3(Mathf.Cos(a), 0.7f, Mathf.Sin(a)) * FrostfellLayout.RinkRadius;
                BlockoutFactory.Cube(area, "RinkWall", pos, Quaternion.Euler(0f, -deg + 90f, 0f), new Vector3(2.3f, 1.4f, 0.35f), wood);
            }

            BlockoutFactory.Sign(area, "SKATE", c + new Vector3(-16f, 3.4f, 6f), Quaternion.LookRotation(Vector3.left), 3.2f, new Color(0.1f, 0.16f, 0.26f), Color.white);
        }

        static void BuildGathering(Transform root, Material wood, Material red, Material blue, Material gold, Material stripe, Material warm, Material pine)
        {
            var area = BlockoutFactory.Group(root, "CritterGathering");
            Vector3 fire = FrostfellLayout.Fire;
            Vector3 toRink = FrostfellLayout.Rink - fire;
            toRink.y = 0f;
            toRink.Normalize();
            float yaw = Mathf.Atan2(toRink.x, toRink.z) * Mathf.Rad2Deg;

            var pit = BlockoutFactory.Cylinder(area, "FirePit", fire + Vector3.up * 0.12f, 1.35f, 0.24f, BlockoutFactory.Mat("PitStone", new Color(0.4f, 0.38f, 0.36f), 0.12f));
            pit.name = "FirePit";
            BlockoutFactory.Sphere(area, "Fire", fire + Vector3.up * 0.7f, 0.45f, warm, false);
            BlockoutFactory.Lamp(area, "FireLight", fire + Vector3.up * 1.4f, new Color(1f, 0.55f, 0.25f), 4000f, 18f, true);

            float[] logOffsets = { 55f, 125f, 195f, 265f };
            foreach (float offset in logOffsets)
            {
                float a = (yaw + offset) * Mathf.Deg2Rad;
                Vector3 pos = fire + new Vector3(Mathf.Sin(a), 0.28f, Mathf.Cos(a)) * 1.85f;
                BlockoutFactory.Cube(area, "FireLog", pos, Quaternion.Euler(0f, yaw + offset, 8f), new Vector3(1.3f, 0.28f, 0.32f), wood);
            }

            var stalls = new[]
            {
                new StallSpec(70f, red, "POPCORN"),
                new StallSpec(130f, blue, "CANDY"),
                new StallSpec(190f, gold, "CIDER"),
                new StallSpec(250f, red, "TAFFY"),
                new StallSpec(310f, blue, "NUTS")
            };
            foreach (var stall in stalls)
                PlaceStall(area, fire, yaw + stall.Offset, stall.Cloth, stripe, wood, stall.Label);

            PlaceCritter(area, fire, yaw + 45f, 3.6f, "Rabbit", new Color(0.9f, 0.9f, 0.92f));
            PlaceCritter(area, fire, yaw + 110f, 4f, "Bird", new Color(0.85f, 0.85f, 0.9f));
            PlaceCritter(area, fire, yaw + 175f, 3.5f, "Fox", new Color(0.85f, 0.5f, 0.22f));
            PlaceCritter(area, fire, yaw + 230f, 4.2f, "Rabbit", new Color(0.82f, 0.84f, 0.88f));
            PlaceCritter(area, fire, yaw + 300f, 3.8f, "Bird", new Color(0.86f, 0.88f, 0.9f));

            BlockoutFactory.Sign(area, "GATHERING", fire + new Vector3(0f, 3.4f, -8.5f), Quaternion.LookRotation(Vector3.back), 4f, new Color(0.16f, 0.1f, 0.08f), Color.white);
        }

        struct StallSpec
        {
            public float Offset;
            public Material Cloth;
            public string Label;
            public StallSpec(float offset, Material cloth, string label)
            {
                Offset = offset;
                Cloth = cloth;
                Label = label;
            }
        }

        static void PlaceStall(Transform parent, Vector3 fire, float yaw, Material cloth, Material stripe, Material wood, string label)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 pos = fire + rot * new Vector3(0f, 0f, 7.4f);
            BlockoutFactory.Cube(parent, "Counter_" + label, pos + Vector3.up * 0.55f, rot, new Vector3(2.6f, 1.1f, 1.3f), wood);
            BlockoutFactory.Cube(parent, "Canopy_" + label, pos + rot * new Vector3(0f, 2.25f, -0.15f), rot, new Vector3(3f, 0.12f, 1.8f), cloth);
            BlockoutFactory.Cube(parent, "CanopyStripe_" + label, pos + rot * new Vector3(0f, 2.34f, -0.15f), rot, new Vector3(3.05f, 0.08f, 0.35f), stripe);
            BlockoutFactory.Cylinder(parent, "PoleL_" + label, pos + rot * new Vector3(-1.3f, 1.15f, -0.7f), 0.08f, 2.3f, wood);
            BlockoutFactory.Cylinder(parent, "PoleR_" + label, pos + rot * new Vector3(1.3f, 1.15f, -0.7f), 0.08f, 2.3f, wood);
            var goods = BlockoutFactory.Mat("Goods" + label, cloth.color * 1.1f, 0.25f);
            BlockoutFactory.Sphere(parent, "GoodsA_" + label, pos + rot * new Vector3(-0.45f, 1.25f, 0.15f), 0.18f, goods, false);
            BlockoutFactory.Sphere(parent, "GoodsB_" + label, pos + rot * new Vector3(0.35f, 1.22f, 0.1f), 0.16f, goods, false);
            BlockoutFactory.Sign(parent, label, pos + Vector3.up * 2.9f, Quaternion.LookRotation(rot * Vector3.back), 2.2f, new Color(0.12f, 0.08f, 0.06f), Color.white);
        }

        static void PlaceCritter(Transform parent, Vector3 fire, float yaw, float radius, string kind, Color color)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 pos = fire + rot * new Vector3(0f, 0f, radius);
            var mat = BlockoutFactory.Mat("Critter" + kind + Mathf.RoundToInt(yaw), color, 0.15f);
            if (kind == "Bird")
            {
                BlockoutFactory.Sphere(parent, "Bird", pos + Vector3.up * 0.35f, 0.28f, mat);
                return;
            }

            BlockoutFactory.Sphere(parent, kind + "Body", pos + Vector3.up * 0.4f, 0.38f, mat);
            BlockoutFactory.Sphere(parent, kind + "Head", pos + new Vector3(0f, 0.75f, 0.15f), 0.22f, mat);
        }

        static void BuildTrees(Transform root, Material pine, Material snow)
        {
            var area = BlockoutFactory.Group(root, "RimPines");
            var trunk = BlockoutFactory.Mat("Trunk", new Color(0.38f, 0.26f, 0.16f), 0.12f);
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2f / 28f + 0.2f;
                float radius = 108f + (i % 3) * 4f;
                float x = Mathf.Cos(a) * radius;
                float z = Mathf.Sin(a) * radius;
                float y = BowlHeight(x, z);
                float treeHeight = 7f + (i % 4) * 1.6f;
                BlockoutFactory.Cylinder(area, "Trunk", new Vector3(x, y + 1.1f, z), 0.35f, 2.2f, trunk);
                BlockoutFactory.Cone(area, "Pine", new Vector3(x, y + 2f, z), 2.4f, treeHeight, pine, 8, true);
                BlockoutFactory.Cone(area, "SnowCap", new Vector3(x, y + 2f + treeHeight * 0.72f, z), 1.3f, 1.6f, snow, 8, false);
            }
        }

        static void BuildLighting(Transform root)
        {
            var sun = BlockoutFactory.Sun(root, "Sun", new Color(0.75f, 0.86f, 1f), 52000f, Quaternion.Euler(38f, 25f, 0f));
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.55f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.42f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.26f, 0.32f);
            RenderSettings.fog = false;
            RenderSettings.skybox = BlockoutFactory.SaveSky("FrostfellSky", new Color(0.42f, 0.58f, 0.82f), 1.15f, 1.12f);
        }

        static void BuildRoutes(Transform root)
        {
            var routes = BlockoutFactory.Group(root, "Routes");
            BlockoutFactory.Spawn(routes, "default", FrostfellLayout.Spawn, Quaternion.identity);

            Vector3 fire = FrostfellLayout.Fire;
            Vector3 toRink = FrostfellLayout.Rink - fire;
            toRink.y = 0f;
            toRink.Normalize();
            Vector3 approach = fire + toRink * 12f;
            approach.y = 0.4f;

            float mazeZ = FrostfellLayout.MazeOrigin.z + (5.5f) * FrostfellLayout.MazeCell;
            BlockoutFactory.Path(routes, "frostfell", 0, "gate", new Vector3(0f, 0.4f, -74f));
            BlockoutFactory.Path(routes, "frostfell", 1, "carousel", new Vector3(0f, 0.4f, -36f));
            BlockoutFactory.Path(routes, "frostfell", 2, "ferris", new Vector3(-25f, 0.4f, -8f));
            BlockoutFactory.Path(routes, "frostfell", 3, "maze-entrance", new Vector3(FrostfellLayout.MazeOrigin.x - 2.5f, 0.4f, mazeZ));
            BlockoutFactory.Path(routes, "frostfell", 4, "south-lane", new Vector3(36f, 0.4f, -66f));
            BlockoutFactory.Path(routes, "frostfell", 5, "stage", new Vector3(FrostfellLayout.Stage.x, 0.4f, -66f));
            BlockoutFactory.Path(routes, "frostfell", 6, "rink", new Vector3(34f, 0.4f, FrostfellLayout.Rink.z));
            BlockoutFactory.Path(routes, "frostfell", 7, "fire-approach", approach);
            BlockoutFactory.Path(routes, "frostfell", 8, "fire-pit", new Vector3(fire.x, 0.4f, fire.z));

            BlockoutFactory.Shot(routes, "frostfell-spawn", new Vector3(7f, 5.5f, -96f), new Vector3(0f, 3f, -55f), 55f);
            BlockoutFactory.Shot(routes, "frostfell-overview", new Vector3(16f, 68f, -112f), new Vector3(-4f, 2f, -6f), 58f);
        }

        static void MarkIce(GameObject go)
        {
            if (go != null && go.GetComponent<IceSurface>() == null)
                go.AddComponent<IceSurface>();
        }
    }
}
