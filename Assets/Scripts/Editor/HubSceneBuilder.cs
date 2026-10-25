using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmShift.EditorTools
{
    public static class HubSceneBuilder
    {
        struct DoorSpec
        {
            public string Name;
            public bool East;
            public int Index;
            public bool Open;
            public Color Color;
            public DoorSpec(string name, bool east, int index, bool open, Color color)
            {
                Name = name;
                East = east;
                Index = index;
                Open = open;
                Color = color;
            }
        }

        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = BlockoutFactory.Group(null, "UnstitchedPalace");
            var level = root.gameObject.AddComponent<LevelSettings>();
            level.killHeight = -15f;

            var stone = BlockoutFactory.Mat("PalaceStone", new Color(0.62f, 0.52f, 0.40f), 0.12f);
            var cobble = BlockoutFactory.Mat("Cobble", new Color(0.48f, 0.44f, 0.38f), 0.08f);
            var wood = BlockoutFactory.Mat("InteriorWood", new Color(0.50f, 0.34f, 0.20f), 0.22f);
            var plaster = BlockoutFactory.Mat("Plaster", new Color(0.55f, 0.42f, 0.46f), 0.1f);
            var water = BlockoutFactory.Mat("MoatWater", new Color(0.22f, 0.42f, 0.58f), 0.72f, 0.05f);
            var closed = BlockoutFactory.Mat("ClosedDoor", new Color(0.32f, 0.32f, 0.34f), 0.15f);
            var ice = BlockoutFactory.Mat("IceTower", new Color(0.55f, 0.84f, 0.95f), 0.7f, 0.05f);
            var brass = BlockoutFactory.Mat("Brass", new Color(0.72f, 0.52f, 0.18f), 0.45f, 0.65f);
            var leaf = BlockoutFactory.Mat("Foliage", new Color(0.24f, 0.52f, 0.26f), 0.12f);
            var carpet = BlockoutFactory.Mat("Carpet", new Color(0.55f, 0.16f, 0.22f), 0.08f);

            BuildSite(root, cobble, stone);
            BuildCourtyard(root, cobble, water, wood, stone, ice, brass, leaf);
            BuildShell(root, stone, plaster, wood, carpet);
            BuildDoors(root, stone, closed);
            BuildRooms(root, plaster, wood, carpet, stone, ice, brass);
            BuildGauntlet(root, ice, brass, stone);
            BuildLighting(root);
            BuildRoutes(root);

            BlockoutFactory.ValidatePath("hub");
            int layer = RealmShiftBuild.PlayerLayer;
            var player = BlockoutFactory.CreatePlayer(HubLayout.Spawn, Quaternion.identity, layer);
            player.transform.SetParent(root, true);
            BlockoutFactory.CreateCamera(player.transform, layer);

            BlockoutFactory.EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, RealmShiftScenes.HubPath);
            Debug.Log("RealmShift: built " + RealmShiftScenes.HubPath);
        }

        static void BuildSite(Transform root, Material cobble, Material stone)
        {
            float sx = HubLayout.SiteMaxX - HubLayout.SiteMinX;
            float sz = HubLayout.SiteMaxZ - HubLayout.SiteMinZ;
            float cx = (HubLayout.SiteMinX + HubLayout.SiteMaxX) * 0.5f;
            float cz = (HubLayout.SiteMinZ + HubLayout.SiteMaxZ) * 0.5f;
            BlockoutFactory.Cube(root, "SiteSlab", new Vector3(cx, -0.5f, cz), new Vector3(sx, 1f, sz), cobble);

            float h = 3.2f;
            float t = 1.1f;
            float y = h * 0.5f;
            BlockoutFactory.Cube(root, "PerimeterN", new Vector3(cx, y, HubLayout.SiteMaxZ), new Vector3(sx, h, t), stone);
            BlockoutFactory.Cube(root, "PerimeterS", new Vector3(cx, y, HubLayout.SiteMinZ), new Vector3(sx, h, t), stone);
            BlockoutFactory.Cube(root, "PerimeterE", new Vector3(HubLayout.SiteMaxX, y, cz), new Vector3(t, h, sz), stone);
            BlockoutFactory.Cube(root, "PerimeterW", new Vector3(HubLayout.SiteMinX, y, cz), new Vector3(t, h, sz), stone);
        }

        static void BuildCourtyard(Transform root, Material cobble, Material water, Material wood, Material stone, Material ice, Material brass, Material leaf)
        {
            var yard = BlockoutFactory.Group(root, "Courtyard");
            BlockoutFactory.Cube(yard, "CourtyardRug", new Vector3(0f, 0.025f, -22f), new Vector3(70f, 0.05f, 48f), cobble);

            // Moat in front of the palace with a bridge on the center line.
            BlockoutFactory.Cube(yard, "MoatWest", new Vector3(-24f, 0.06f, 7.5f), new Vector3(32f, 0.12f, 7f), water);
            BlockoutFactory.Cube(yard, "MoatEast", new Vector3(24f, 0.06f, 7.5f), new Vector3(32f, 0.12f, 7f), water);
            BlockoutFactory.Cube(yard, "Bridge", new Vector3(0f, 0.05f, 7.5f), new Vector3(12f, 0.1f, 8f), wood);
            BlockoutFactory.Sign(yard, "PALACE", new Vector3(0f, 4.2f, 12.2f), Quaternion.LookRotation(Vector3.back), 6f, new Color(0.16f, 0.1f, 0.08f), Color.white);

            // Pond west of the approach.
            BlockoutFactory.Cylinder(yard, "Pond", HubLayout.Pond + Vector3.up * 0.07f, HubLayout.PondRadius, 0.12f, water);
            int stones = 14;
            for (int i = 0; i < stones; i++)
            {
                float a = i * Mathf.PI * 2f / stones;
                var pos = HubLayout.Pond + new Vector3(Mathf.Cos(a), 0.2f, Mathf.Sin(a)) * (HubLayout.PondRadius + 0.8f);
                BlockoutFactory.Cube(yard, "PondStone", pos, new Vector3(1.3f, 0.4f, 0.7f), stone);
            }

            BuildTower(yard, "IceTower", HubLayout.IceTower, HubLayout.IceTowerHeight, ice, true, false);
            BuildTower(yard, "GearTower", HubLayout.GearTower, HubLayout.GearTowerHeight, brass, false, true);
            BuildTower(yard, "GreenTower", HubLayout.GreenTower, HubLayout.GreenTowerHeight, leaf, false, false);

            BlockoutFactory.Cylinder(yard, "LampWest", new Vector3(-14f, 2.2f, -24f), 0.18f, 4.4f, wood);
            BlockoutFactory.Cylinder(yard, "LampEast", new Vector3(14f, 2.2f, -24f), 0.18f, 4.4f, wood);
            var glow = BlockoutFactory.Mat("Lantern", new Color(1f, 0.78f, 0.4f), 0.4f, 0f, new Color(1.2f, 0.6f, 0.2f));
            BlockoutFactory.Sphere(yard, "LampGlowW", new Vector3(-14f, 4.5f, -24f), 0.28f, glow, false);
            BlockoutFactory.Sphere(yard, "LampGlowE", new Vector3(14f, 4.5f, -24f), 0.28f, glow, false);

            // Facade just south of the hall, with a gate on the walk.
            float facadeZ = HubLayout.HallMinZ - 0.8f;
            BlockoutFactory.Cube(yard, "FacadeWest", new Vector3(-22f, 4f, facadeZ), new Vector3(36f, 8f, 1.2f), stone);
            BlockoutFactory.Cube(yard, "FacadeEast", new Vector3(22f, 4f, facadeZ), new Vector3(36f, 8f, 1.2f), stone);
            BlockoutFactory.Cube(yard, "FacadeLintel", new Vector3(0f, 6.6f, facadeZ), new Vector3(10f, 2.4f, 1.2f), stone);
            BlockoutFactory.Arch(yard, "PalaceArch", new Vector3(0f, 0f, facadeZ), 5.2f, 0.7f, 1.4f, stone, 0f, false);
        }

        static void BuildTower(Transform parent, string name, Vector3 basePos, float height, Material material, bool icy, bool gears)
        {
            var tower = BlockoutFactory.Group(parent, name);
            float radius = icy ? 3.2f : 2.8f;
            BlockoutFactory.Cylinder(tower, name + "Shaft", basePos + Vector3.up * (height * 0.42f), radius, height * 0.84f, material);
            BlockoutFactory.Cone(tower, name + "Roof", basePos + Vector3.up * (height * 0.84f), radius * 1.35f, height * 0.28f, material, 10, true);
            if (icy)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI * 2f / 6f;
                    var pos = basePos + new Vector3(Mathf.Cos(a) * (radius + 0.4f), 2.2f + i * 0.15f, Mathf.Sin(a) * (radius + 0.4f));
                    BlockoutFactory.Cube(tower, "Icicle", pos, Quaternion.Euler(0f, a * Mathf.Rad2Deg, 18f), new Vector3(0.35f, 1.4f, 0.35f), material);
                }
            }
            if (gears)
            {
                for (int i = 0; i < 3; i++)
                {
                    float y = 8f + i * 7f;
                    BlockoutFactory.Torus(tower, "Gear", basePos + Vector3.up * y, Quaternion.Euler(90f, i * 25f, 0f), 4.2f, 0.45f, material, 16, true);
                }
            }
            if (!icy && !gears)
            {
                BlockoutFactory.Cube(tower, "Treehouse", basePos + new Vector3(2.2f, height * 0.55f, 0f), new Vector3(3.5f, 2.2f, 3.2f), material);
                BlockoutFactory.Cone(tower, "Canopy", basePos + Vector3.up * (height * 0.7f), 5f, 6f, material, 8, true);
            }
        }

        static void BuildShell(Transform root, Material stone, Material plaster, Material wood, Material carpet)
        {
            var hall = BlockoutFactory.Group(root, "FlickeringHall");
            float x0 = -HubLayout.HallHalfWidth;
            float x1 = HubLayout.HallHalfWidth;
            float z0 = HubLayout.HallMinZ;
            float z1 = HubLayout.HallMaxZ;
            float length = z1 - z0;
            float midZ = (z0 + z1) * 0.5f;
            BlockoutFactory.Cube(hall, "HallFloor", new Vector3(0f, 0.03f, midZ), new Vector3(x1 - x0, 0.06f, length), wood);
            BlockoutFactory.Cube(hall, "HallCarpet", new Vector3(0f, 0.07f, midZ), new Vector3(3.2f, 0.04f, length - 2f), carpet);

            // Side walls up to the door head, with gaps cut for the eight doors and the remix passage.
            var westGaps = new List<Vector2>
            {
                new Vector2(25f, 4.2f)
            };
            var eastGaps = new List<Vector2>();
            for (int i = 0; i < HubLayout.DoorZ.Length; i++)
            {
                westGaps.Add(new Vector2(HubLayout.DoorZ[i], HubLayout.DoorWidth));
                eastGaps.Add(new Vector2(HubLayout.DoorZ[i], HubLayout.DoorWidth));
            }
            WallAlongZ(hall, x0, z0, z1, HubLayout.DoorHeight, 0.7f, plaster, westGaps, "HallWallW");
            WallAlongZ(hall, x1, z0, z1, HubLayout.DoorHeight, 0.7f, plaster, eastGaps, "HallWallE");

            float band = HubLayout.CeilingY - HubLayout.DoorHeight;
            BlockoutFactory.Cube(hall, "HallBandW", new Vector3(x0, HubLayout.DoorHeight + band * 0.5f, midZ), new Vector3(0.7f, band, length), plaster);
            BlockoutFactory.Cube(hall, "HallBandE", new Vector3(x1, HubLayout.DoorHeight + band * 0.5f, midZ), new Vector3(0.7f, band, length), plaster);
            BlockoutFactory.Cube(hall, "HallCeiling", new Vector3(0f, HubLayout.CeilingY, midZ), new Vector3(x1 - x0 + 0.7f, 0.25f, length), stone);

            // North wall of the hall, with a passage into the junction.
            BlockoutFactory.Cube(hall, "HallNorthWest", new Vector3(-10.5f, 2.6f, z1), new Vector3(5f, 5.2f, 0.7f), plaster);
            BlockoutFactory.Cube(hall, "HallNorthEast", new Vector3(10.5f, 2.6f, z1), new Vector3(5f, 5.2f, 0.7f), plaster);
            BlockoutFactory.Cube(hall, "HallNorthLintel", new Vector3(0f, 4.6f, z1), new Vector3(8f, 2.2f, 0.7f), plaster);
        }

        static void WallAlongZ(Transform parent, float x, float z0, float z1, float height, float thickness, Material material, List<Vector2> gaps, string name)
        {
            gaps.Sort((a, b) => a.x.CompareTo(b.x));
            var cuts = new List<float> { z0 };
            foreach (var gap in gaps)
            {
                cuts.Add(gap.x - gap.y * 0.5f);
                cuts.Add(gap.x + gap.y * 0.5f);
            }
            cuts.Add(z1);
            int piece = 0;
            for (int i = 0; i + 1 < cuts.Count; i += 2)
            {
                float a = cuts[i];
                float b = cuts[i + 1];
                if (b - a < 0.25f)
                    continue;
                BlockoutFactory.Cube(parent, name + piece, new Vector3(x, height * 0.5f, (a + b) * 0.5f), new Vector3(thickness, height, b - a), material);
                piece++;
            }
        }

        static void BuildDoors(Transform root, Material stone, Material closed)
        {
            var doors = new[]
            {
                new DoorSpec("Frostfell Fair", true, 0, true, new Color(0.45f, 0.82f, 0.95f)),
                new DoorSpec("Sunken Nave", true, 1, false, new Color(0.25f, 0.48f, 0.78f)),
                new DoorSpec("Cinder Flats", true, 2, false, new Color(0.86f, 0.40f, 0.16f)),
                new DoorSpec("Rustworks", true, 3, false, new Color(0.62f, 0.32f, 0.2f)),
                new DoorSpec("Canopy Tangle", false, 0, false, new Color(0.28f, 0.62f, 0.28f)),
                new DoorSpec("Gale Spires", false, 1, false, new Color(0.72f, 0.82f, 0.78f)),
                new DoorSpec("Burrowmere", false, 2, false, new Color(0.55f, 0.38f, 0.22f)),
                new DoorSpec("Spare", false, 3, false, new Color(0.52f, 0.52f, 0.56f))
            };

            var parent = BlockoutFactory.Group(root, "WorldDoors");
            foreach (var door in doors)
            {
                float z = HubLayout.DoorZ[door.Index];
                float x = door.East ? HubLayout.HallHalfWidth : -HubLayout.HallHalfWidth;
                float inward = door.East ? -1f : 1f;
                var theme = BlockoutFactory.Mat("Door_" + BlockoutFactory.Sanitize(door.Name), door.Color, door.Open ? 0.35f : 0.12f);
                var frame = BlockoutFactory.Group(parent, "Door_" + BlockoutFactory.Sanitize(door.Name));
                var marker = frame.gameObject.AddComponent<WorldDoor>();
                marker.worldName = door.Name;
                marker.open = door.Open;

                float half = HubLayout.DoorWidth * 0.5f;
                BlockoutFactory.Cube(frame, "JambA", new Vector3(x, HubLayout.DoorHeight * 0.5f, z - half), new Vector3(0.45f, HubLayout.DoorHeight, 0.35f), theme);
                BlockoutFactory.Cube(frame, "JambB", new Vector3(x, HubLayout.DoorHeight * 0.5f, z + half), new Vector3(0.45f, HubLayout.DoorHeight, 0.35f), theme);
                BlockoutFactory.Cube(frame, "HeaderFlag", new Vector3(x + inward * 0.2f, HubLayout.DoorHeight + 0.45f, z), new Vector3(0.8f, 0.7f, 0.8f), theme);

                string label = door.Open ? door.Name : door.Name + "\n(closed)";
                Vector3 signPos = new Vector3(x + inward * 0.85f, HubLayout.DoorHeight + 1.35f, z);
                Quaternion faceIn = Quaternion.LookRotation(new Vector3(inward, 0f, 0f));
                BlockoutFactory.Sign(frame, label, signPos, faceIn, 4.4f, new Color(0.1f, 0.08f, 0.07f), Color.white);

                if (door.Open)
                {
                    BlockoutFactory.Portal(frame, "Portal", new Vector3(x - inward * 2.4f, 1.6f, z), new Vector3(2.4f, 3.2f, HubLayout.DoorWidth * 0.85f), RealmShiftScenes.Frostfell, "default");
                }
                else
                {
                    BlockoutFactory.Cube(frame, "Closed", new Vector3(x, HubLayout.DoorHeight * 0.5f, z), new Vector3(0.35f, HubLayout.DoorHeight - 0.05f, HubLayout.DoorWidth - 0.5f), closed);
                }
            }
        }

        static void BuildRooms(Transform root, Material plaster, Material wood, Material carpet, Material stone, Material ice, Material brass)
        {
            var rooms = BlockoutFactory.Group(root, "Rooms");
            // Junction north of the hall.
            BlockoutFactory.Cube(rooms, "JunctionFloor", new Vector3(0f, 0.03f, 62f), new Vector3(28f, 0.06f, 12f), wood);

            BuildBoxRoom(rooms, "CollectionRoom", 16f, 40f, 60f, 78f, plaster, wood, "west", 69f, 4.5f);
            FillCollection(rooms, wood, ice);

            BuildBoxRoom(rooms, "ThroneRoom", -42f, -16f, 60f, 80f, plaster, wood, "east", 70f, 4.5f);
            BuildThrone(rooms, carpet, wood);

            BuildBoxRoom(rooms, "RemixGallery", -42f, -16f, 24f, 46f, plaster, wood, "east", 25f, 4.2f);
            BuildStatues(rooms);

            BuildTrueBodies(rooms, plaster, carpet, stone, wood);
        }

        static void BuildBoxRoom(Transform parent, string name, float x0, float x1, float z0, float z1, Material wall, Material floor, string doorSide, float doorCenter, float doorWidth)
        {
            var room = BlockoutFactory.Group(parent, name);
            float cx = (x0 + x1) * 0.5f;
            float cz = (z0 + z1) * 0.5f;
            BlockoutFactory.Cube(room, name + "Floor", new Vector3(cx, 0.03f, cz), new Vector3(x1 - x0, 0.06f, z1 - z0), floor);
            float h = 5.2f;
            float t = 0.6f;
            // South / north full or gapped.
            WallAlongX(room, z0, x0, x1, h, t, wall, doorSide == "south" ? doorCenter : (float?)null, doorWidth, name + "S");
            WallAlongX(room, z1, x0, x1, h, t, wall, doorSide == "north" ? doorCenter : (float?)null, doorWidth, name + "N");
            WallAlongZ(room, x0, z0, z1, h, t, wall, doorSide == "west" ? new List<Vector2> { new Vector2(doorCenter, doorWidth) } : new List<Vector2>(), name + "W");
            WallAlongZ(room, x1, z0, z1, h, t, wall, doorSide == "east" ? new List<Vector2> { new Vector2(doorCenter, doorWidth) } : new List<Vector2>(), name + "E");
            BlockoutFactory.Cube(room, name + "Ceiling", new Vector3(cx, h, cz), new Vector3(x1 - x0, 0.2f, z1 - z0), wall);
            BlockoutFactory.Lamp(room, name + "Light", new Vector3(cx, 4.2f, cz), new Color(1f, 0.78f, 0.55f), 3200f, 16f, false);
        }

        static void WallAlongX(Transform parent, float z, float x0, float x1, float height, float thickness, Material material, float? gapCenter, float gapWidth, string name)
        {
            if (!gapCenter.HasValue)
            {
                BlockoutFactory.Cube(parent, name, new Vector3((x0 + x1) * 0.5f, height * 0.5f, z), new Vector3(x1 - x0, height, thickness), material);
                return;
            }
            float a = gapCenter.Value - gapWidth * 0.5f;
            float b = gapCenter.Value + gapWidth * 0.5f;
            if (a - x0 > 0.3f)
                BlockoutFactory.Cube(parent, name + "A", new Vector3((x0 + a) * 0.5f, height * 0.5f, z), new Vector3(a - x0, height, thickness), material);
            if (x1 - b > 0.3f)
                BlockoutFactory.Cube(parent, name + "B", new Vector3((b + x1) * 0.5f, height * 0.5f, z), new Vector3(x1 - b, height, thickness), material);
            BlockoutFactory.Cube(parent, name + "Lintel", new Vector3(gapCenter.Value, height - 0.7f, z), new Vector3(gapWidth, 1.4f, thickness), material);
        }

        static void FillCollection(Transform parent, Material wood, Material glass)
        {
            var room = BlockoutFactory.Group(parent, "CollectionProps");
            for (int i = 0; i < 6; i++)
            {
                float x = 22f + i * 2.6f;
                Case(room, new Vector3(x, 0.55f, 63f), wood, glass, i);
                Case(room, new Vector3(x, 0.55f, 75f), wood, glass, i + 6);
            }
            BlockoutFactory.Cube(room, "Ledger", new Vector3(28f, 0.85f, 69f), new Vector3(2.2f, 0.8f, 1.1f), wood);
        }

        static void Case(Transform parent, Vector3 pos, Material wood, Material glass, int index)
        {
            BlockoutFactory.Cube(parent, "Case" + index, pos, new Vector3(1.5f, 1.1f, 0.7f), wood);
            BlockoutFactory.Sphere(parent, "Specimen" + index, pos + Vector3.up * 0.85f, 0.28f, glass, true);
        }

        static void BuildThrone(Transform parent, Material carpet, Material wood)
        {
            var room = BlockoutFactory.Group(parent, "ThroneProps");
            BlockoutFactory.Cube(room, "ThroneCarpet", new Vector3(-29f, 0.08f, 70f), new Vector3(3.2f, 0.04f, 14f), carpet);
            Vector3 origin = new Vector3(-29f, 0f, 74f);
            var colors = new[]
            {
                new Color(0.7f, 0.15f, 0.2f),
                new Color(0.15f, 0.35f, 0.7f),
                new Color(0.85f, 0.7f, 0.2f),
                new Color(0.2f, 0.55f, 0.35f),
                new Color(0.55f, 0.25f, 0.6f),
                new Color(0.85f, 0.45f, 0.2f)
            };
            BlockoutFactory.Cube(room, "ThroneDais", origin + new Vector3(0f, 0.2f, 0f), new Vector3(3.4f, 0.4f, 2.6f), carpet);
            BlockoutFactory.Cube(room, "ThroneSeat", origin + new Vector3(0f, 0.85f, 0.1f), new Vector3(1.7f, 0.45f, 1.3f), wood);
            BlockoutFactory.Cube(room, "ThroneBack", origin + new Vector3(0f, 1.9f, 0.7f), new Vector3(1.8f, 2.1f, 0.35f), wood);
            BlockoutFactory.Cube(room, "ThroneArmL", origin + new Vector3(-0.85f, 1.05f, 0f), new Vector3(0.25f, 0.7f, 1.1f), wood);
            BlockoutFactory.Cube(room, "ThroneArmR", origin + new Vector3(0.85f, 1.05f, 0f), new Vector3(0.25f, 0.7f, 1.1f), wood);
            for (int i = 0; i < colors.Length; i++)
            {
                var patch = BlockoutFactory.Mat("Patch" + i, colors[i], 0.2f);
                float y = 1.3f + (i % 3) * 0.55f;
                float x = -0.45f + (i / 3) * 0.7f;
                BlockoutFactory.Cube(room, "Patch" + i, origin + new Vector3(x, y, 0.92f), new Vector3(0.55f, 0.45f, 0.12f), patch);
            }
            // About 3 m from the dais floor to the top of the back.
            BlockoutFactory.Stair(room, "ThroneSteps", origin + new Vector3(0f, 0f, -1.8f), new Vector3(2.2f, 0.4f, 1.6f), 3, wood, 0f);
        }

        static void BuildStatues(Transform parent)
        {
            var room = BlockoutFactory.Group(parent, "RemixStatues");
            var plinthMat = BlockoutFactory.Mat("Plinth", new Color(0.45f, 0.36f, 0.28f), 0.15f);
            var mole = BlockoutFactory.Mat("Mole", new Color(0.42f, 0.3f, 0.2f), 0.15f);
            var bird = BlockoutFactory.Mat("Bird", new Color(0.2f, 0.55f, 0.62f), 0.25f);
            var mouse = BlockoutFactory.Mat("Mouse", new Color(0.62f, 0.62f, 0.64f), 0.2f);
            var pink = BlockoutFactory.Mat("Ear", new Color(0.85f, 0.55f, 0.58f), 0.2f);
            StatueMole(room, new Vector3(-34f, 0f, 36f), plinthMat, mole);
            StatueBird(room, new Vector3(-29f, 0f, 36f), plinthMat, bird);
            StatueMouse(room, new Vector3(-24f, 0f, 36f), plinthMat, mouse, pink);
            BlockoutFactory.Sign(room, "REMIX GALLERY", new Vector3(-29f, 3.6f, 44.5f), Quaternion.LookRotation(Vector3.back), 6f, new Color(0.12f, 0.08f, 0.08f), Color.white);
        }

        static void StatueMole(Transform parent, Vector3 pos, Material plinth, Material body)
        {
            BlockoutFactory.Cube(parent, "MolePlinth", pos + Vector3.up * 0.55f, new Vector3(1.4f, 1.1f, 1.4f), plinth);
            BlockoutFactory.Sphere(parent, "MoleBody", pos + new Vector3(0f, 1.55f, 0f), 0.55f, body);
            BlockoutFactory.Sphere(parent, "MoleHead", pos + new Vector3(0f, 2.15f, 0.15f), 0.32f, body);
            BlockoutFactory.Cylinder(parent, "Goggle", pos + new Vector3(0f, 2.2f, 0.4f), 0.16f, 0.08f, BlockoutFactory.Mat("Goggle", new Color(0.15f, 0.15f, 0.18f), 0.6f, 0.4f));
        }

        static void StatueBird(Transform parent, Vector3 pos, Material plinth, Material body)
        {
            BlockoutFactory.Cube(parent, "BirdPlinth", pos + Vector3.up * 0.55f, new Vector3(1.4f, 1.1f, 1.4f), plinth);
            BlockoutFactory.Sphere(parent, "BirdBody", pos + new Vector3(0f, 1.6f, 0f), 0.48f, body);
            BlockoutFactory.Cone(parent, "Beak", pos + new Vector3(0f, 1.7f, 0.45f), 0.12f, 0.45f, BlockoutFactory.Mat("Beak", new Color(0.9f, 0.6f, 0.15f)), 8, true);
            BlockoutFactory.Cube(parent, "Wing", pos + new Vector3(0.45f, 1.65f, 0f), Quaternion.Euler(0f, 0f, 20f), new Vector3(0.7f, 0.15f, 0.45f), body);
        }

        static void StatueMouse(Transform parent, Vector3 pos, Material plinth, Material body, Material ear)
        {
            BlockoutFactory.Cube(parent, "MousePlinth", pos + Vector3.up * 0.55f, new Vector3(1.4f, 1.1f, 1.4f), plinth);
            BlockoutFactory.Sphere(parent, "MouseBody", pos + new Vector3(0f, 1.45f, 0f), 0.42f, body);
            BlockoutFactory.Sphere(parent, "MouseHead", pos + new Vector3(0f, 2.0f, 0.15f), 0.28f, body);
            BlockoutFactory.Sphere(parent, "EarL", pos + new Vector3(-0.18f, 2.28f, 0.05f), 0.12f, ear, false);
            BlockoutFactory.Sphere(parent, "EarR", pos + new Vector3(0.18f, 2.28f, 0.05f), 0.12f, ear, false);
        }

        static void BuildTrueBodies(Transform parent, Material plaster, Material carpet, Material stone, Material wood)
        {
            var room = BlockoutFactory.Group(parent, "TrueBodies");
            Vector3 c = HubLayout.TrueBodies;
            float r = HubLayout.TrueBodiesRadius;
            BlockoutFactory.Cylinder(room, "RoundFloor", c + Vector3.up * 0.04f, r, 0.08f, wood);
            int segments = 12;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                // Leave the south gap (toward -Z, angle around pi*1.5 or -pi/2 depending on atan2).
                // South is angle = -90 deg / 270 deg. Skip the segment nearest south.
                float deg = a * Mathf.Rad2Deg;
                float southDelta = Mathf.DeltaAngle(deg, 180f); // sin(a) negative is -Z when a = pi? 
                // a=0 -> +X, a=90 -> +Z, a=180 -> -X, a=270 -> -Z.
                if (Mathf.Abs(Mathf.DeltaAngle(deg, 270f)) < 22f)
                    continue;
                Vector3 pos = c + new Vector3(Mathf.Cos(a), 2.6f, Mathf.Sin(a)) * (r - 0.4f);
                var wall = BlockoutFactory.Cube(room, "RoundWall" + i, pos, Quaternion.Euler(0f, -deg, 0f), new Vector3(2.4f, 5.2f, 0.6f), plaster);
                var tapestry = BlockoutFactory.Mat("Tapestry" + (i % 6), Color.HSVToRGB((i % 6) / 6f, 0.45f, 0.75f), 0.15f);
                BlockoutFactory.Cube(room, "Tapestry" + i, pos + Quaternion.Euler(0f, -deg, 0f) * new Vector3(0f, 0.2f, -0.4f), Quaternion.Euler(0f, -deg, 0f), new Vector3(1.4f, 2.2f, 0.08f), tapestry);
                wall.name = "RoundWall" + i;
            }
            BlockoutFactory.Cylinder(room, "Dais", c + Vector3.up * 0.22f, HubLayout.DaisRadius, 0.44f, carpet);
            BlockoutFactory.Stair(room, "DaisSteps", c + new Vector3(0f, 0f, -HubLayout.DaisRadius - 0.4f), new Vector3(3f, 0.44f, 2.2f), 3, wood, 0f);
            var cloth = BlockoutFactory.Mat("Mannequin", new Color(0.75f, 0.7f, 0.6f), 0.1f);
            float[] angles = { 20f, 80f, 150f, 210f, 330f };
            foreach (float deg in angles)
            {
                float a = deg * Mathf.Deg2Rad;
                Vector3 pos = c + new Vector3(Mathf.Cos(a), 1.1f, Mathf.Sin(a)) * 7.2f;
                BlockoutFactory.Cylinder(room, "Figure", pos, 0.35f, 2.2f, cloth);
            }
            BlockoutFactory.Sign(room, "TRUE BODIES", c + new Vector3(0f, 4.4f, r - 1.2f), Quaternion.LookRotation(Vector3.back), 5f, new Color(0.12f, 0.08f, 0.08f), Color.white);
            BlockoutFactory.Lamp(room, "DaisLight", c + Vector3.up * 4.4f, new Color(1f, 0.8f, 0.55f), 4000f, 18f, false);
        }

        static void BuildGauntlet(Transform root, Material ice, Material brass, Material stone)
        {
            var hall = BlockoutFactory.Group(root, "Gauntlet");
            // Short iced passage off the east courtyard. Flat, walkable, not on the test path.
            BlockoutFactory.Cube(hall, "GauntletFloor", new Vector3(46f, 0.04f, -32f), new Vector3(48f, 0.08f, 6f), ice);
            BlockoutFactory.Cube(hall, "GauntletWallN", new Vector3(46f, 2f, -29.2f), new Vector3(48f, 4f, 0.5f), stone);
            BlockoutFactory.Cube(hall, "GauntletWallS", new Vector3(46f, 2f, -34.8f), new Vector3(48f, 4f, 0.5f), stone);
            BlockoutFactory.Cube(hall, "GauntletEnd", new Vector3(70f, 2f, -32f), new Vector3(0.6f, 4f, 6f), stone);
            for (int i = 0; i < 4; i++)
            {
                float x = 28f + i * 10f;
                BlockoutFactory.Torus(hall, "GauntletGear" + i, new Vector3(x, 2.2f, -29.6f), Quaternion.Euler(0f, 0f, 90f), 1.3f, 0.22f, brass, 12, false);
            }
            BlockoutFactory.Sign(hall, "GAUNTLET", new Vector3(24f, 3.2f, -32f), Quaternion.LookRotation(Vector3.right), 3.5f, new Color(0.1f, 0.1f, 0.12f), new Color(0.8f, 0.92f, 1f));
        }

        static void BuildLighting(Transform root)
        {
            var sun = BlockoutFactory.Sun(root, "Sun", new Color(1f, 0.9f, 0.75f), 48000f, Quaternion.Euler(48f, -32f, 0f));
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.5f, 0.4f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.38f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.18f, 0.14f);
            RenderSettings.fog = false;
            RenderSettings.skybox = BlockoutFactory.SaveSky("HubSky", new Color(0.55f, 0.42f, 0.48f), 1.05f, 1.15f);

            float[] zs = HubLayout.DoorZ;
            for (int i = 0; i < zs.Length; i++)
            {
                BlockoutFactory.Lamp(root, "HallLamp" + i, new Vector3(0f, 4.4f, zs[i]), new Color(1f, 0.72f, 0.45f), 2200f, 12f, true);
            }
        }

        static void BuildRoutes(Transform root)
        {
            var routes = BlockoutFactory.Group(root, "Routes");
            BlockoutFactory.Spawn(routes, "default", HubLayout.Spawn, Quaternion.identity);
            BlockoutFactory.Spawn(routes, "from-frostfell", HubLayout.ReturnSpawn, Quaternion.Euler(0f, HubLayout.ReturnYaw, 0f));

            BlockoutFactory.Path(routes, "hub", 0, "courtyard", new Vector3(0f, 0.4f, -28f));
            BlockoutFactory.Path(routes, "hub", 1, "bridge", new Vector3(0f, 0.4f, 8f));
            BlockoutFactory.Path(routes, "hub", 2, "hall-entrance", new Vector3(0f, 0.4f, 18f));
            BlockoutFactory.Path(routes, "hub", 3, "hall-center", new Vector3(0f, 0.4f, 38f));
            BlockoutFactory.Path(routes, "hub", 4, "frostfell-door", new Vector3(4.2f, 0.4f, HubLayout.DoorZ[0]));

            BlockoutFactory.Shot(routes, "hub-courtyard", new Vector3(34f, 16f, -60f), new Vector3(4f, 4f, -12f), 55f);
            BlockoutFactory.Shot(routes, "hub-flickering-hall", new Vector3(0f, 2.7f, 15.4f), new Vector3(0f, 2.1f, 50f), 74f);
        }
    }
}
