using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace RealmShift.EditorTools
{
    /// <summary>Primitives, ProBuilder meshes, signs, and path checks shared by the level builders.</summary>
    public static class BlockoutFactory
    {
        static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

        public static void ResetGeneratedMeshes()
        {
            MeshCache.Clear();
            if (AssetDatabase.IsValidFolder("Assets/Meshes/Generated"))
                AssetDatabase.DeleteAsset("Assets/Meshes/Generated");
            EnsureFolder("Assets/Meshes/Generated");
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static Material Mat(string name, Color color, float smoothness = 0.18f, float metallic = 0f, Color? emission = null)
        {
            if (MaterialCache.TryGetValue(name, out var cached) && cached != null)
            {
                ApplyMaterial(cached, color, smoothness, metallic, emission);
                return cached;
            }

            EnsureFolder("Assets/Materials/Blockout");
            string path = "Assets/Materials/Blockout/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new System.InvalidOperationException("URP Lit shader was not found. Is com.unity.render-pipelines.universal installed?");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            ApplyMaterial(mat, color, smoothness, metallic, emission);
            EditorUtility.SetDirty(mat);
            MaterialCache[name] = mat;
            return mat;
        }

        static void ApplyMaterial(Material mat, Color color, float smoothness, float metallic, Color? emission)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null && mat.shader != shader)
                mat.shader = shader;
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            if (emission.HasValue && emission.Value.maxColorComponent > 0.01f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        public static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider = true)
        {
            return Cube(parent, name, position, Quaternion.identity, size, material, collider);
        }

        public static GameObject Cube(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 size, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!collider)
            {
                var existing = go.GetComponent<Collider>();
                if (existing != null)
                    Object.DestroyImmediate(existing);
            }
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        public static GameObject Cylinder(Transform parent, string name, Vector3 position, float radius, float height, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (!collider)
            {
                var existing = go.GetComponent<Collider>();
                if (existing != null)
                    Object.DestroyImmediate(existing);
            }
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 position, float radius, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            if (!collider)
            {
                var existing = go.GetComponent<Collider>();
                if (existing != null)
                    Object.DestroyImmediate(existing);
            }
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        public static GameObject Beam(Transform parent, string name, Vector3 a, Vector3 b, float thickness, Material material)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.05f)
                return null;
            Vector3 up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(delta / length, Vector3.up)) > 0.96f)
                up = Vector3.forward;
            var rotation = Quaternion.LookRotation(delta / length, up);
            return Cube(parent, name, (a + b) * 0.5f, rotation, new Vector3(thickness, thickness, length), material, true);
        }

        public static GameObject Cone(Transform parent, string name, Vector3 position, float radius, float height, Material material, int sides = 12, bool collider = true)
        {
            string key = string.Format(CultureInfo.InvariantCulture, "cone_r{0:0.###}_h{1:0.###}_s{2}", radius, height, sides);
            var mesh = Bake(key, () => ShapeGenerator.GenerateCone(PivotLocation.Center, radius, height, sides));
            return PlaceMesh(parent, name, mesh, position, Quaternion.identity, Vector3.one, material, collider, snapBottomTo: position.y);
        }

        public static GameObject Stair(Transform parent, string name, Vector3 position, Vector3 size, int steps, Material material, float yawDegrees)
        {
            string key = string.Format(CultureInfo.InvariantCulture, "stair_{0:0.###}_{1:0.###}_{2:0.###}_{3}", size.x, size.y, size.z, steps);
            var mesh = Bake(key, () => ShapeGenerator.GenerateStair(PivotLocation.Center, size, steps, true));
            var go = PlaceMesh(parent, name, mesh, position, Quaternion.Euler(0f, yawDegrees, 0f), Vector3.one, material, true, snapBottomTo: 0f);
            return go;
        }

        public static GameObject Torus(Transform parent, string name, Vector3 position, Quaternion rotation, float outerRadius, float tubeRadius, Material material, int columns = 24, bool collider = false)
        {
            tubeRadius = Mathf.Min(tubeRadius, outerRadius * 0.45f);
            string key = string.Format(CultureInfo.InvariantCulture, "torus_o{0:0.###}_t{1:0.###}_c{2}", outerRadius, tubeRadius, columns);
            var mesh = Bake(key, () => ShapeGenerator.GenerateTorus(PivotLocation.Center, 8, columns, outerRadius, tubeRadius, true, 360f, 360f, false));
            return PlaceMesh(parent, name, mesh, position, rotation, Vector3.one, material, collider, snapBottomTo: null);
        }

        public static GameObject Pipe(Transform parent, string name, Vector3 position, float outerRadius, float height, float thickness, Material material, bool collider = true)
        {
            string key = string.Format(CultureInfo.InvariantCulture, "pipe_r{0:0.###}_h{1:0.###}_t{2:0.###}", outerRadius, height, thickness);
            var mesh = Bake(key, () => ShapeGenerator.GeneratePipe(PivotLocation.Center, outerRadius, height, thickness, 28, 1));
            return PlaceMesh(parent, name, mesh, position, Quaternion.identity, Vector3.one, material, collider, snapBottomTo: position.y - height * 0.5f);
        }

        public static GameObject Arch(Transform parent, string name, Vector3 position, float radius, float width, float depth, Material material, float yawDegrees, bool collider = false)
        {
            string key = string.Format(CultureInfo.InvariantCulture, "arch_r{0:0.###}_w{1:0.###}_d{2:0.###}", radius, width, depth);
            var mesh = Bake(key, () => ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, radius, width, depth, 14, true, true, true, true, true));
            var go = PlaceMesh(parent, name, mesh, position, Quaternion.Euler(0f, yawDegrees, 0f), Vector3.one, material, collider, snapBottomTo: 0f);
            RecenterXZ(go, position.x, position.z);
            SnapBottom(go, 0f);
            return go;
        }

        public static void Sign(Transform parent, string text, Vector3 position, Quaternion rotation, float width, Color boardColor, Color textColor)
        {
            int lines = 1;
            int longest = 1;
            var parts = text.Split('\n');
            lines = parts.Length;
            foreach (var line in parts)
                longest = Mathf.Max(longest, line.Length);
            float character = width / (longest * 0.58f);
            float height = Mathf.Max(width * 0.26f, lines * character * 1.55f);
            var boardMat = Mat("SignBoard", boardColor, 0.12f);
            var board = Cube(parent, "Sign_" + Sanitize(text), position, rotation, new Vector3(width, height, 0.12f), boardMat, true);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(board.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, 0.09f);
            labelGo.transform.localRotation = Quaternion.identity;
            var textMesh = labelGo.AddComponent<TextMesh>();
            textMesh.font = BuiltinFont();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = textColor;
            textMesh.fontSize = 64;
            textMesh.characterSize = character;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.richText = false;
            var label = labelGo.AddComponent<BlockoutLabel>();
            label.color = textColor;
        }

        public static Light Sun(Transform parent, string name, Color color, float lux, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.rotation = rotation;
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.lightUnit = UnityEngine.Rendering.LightUnit.Lux;
            light.intensity = lux;
            light.shadows = LightShadows.Soft;
            return light;
        }

        public static Light Lamp(Transform parent, string name, Vector3 position, Color color, float lumens, float range, bool flicker = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.lightUnit = UnityEngine.Rendering.LightUnit.Lumen;
            light.intensity = lumens;
            light.range = range;
            light.shadows = LightShadows.None;
            if (flicker)
            {
                var wobble = go.AddComponent<FlickerLight>();
                wobble.baseIntensity = lumens;
            }
            return light;
        }

        public static void Spawn(Transform parent, string id, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("Spawn_" + id);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var spawn = go.AddComponent<SpawnPoint>();
            spawn.spawnId = id;
        }

        public static void Path(Transform parent, string pathId, int order, string label, Vector3 position)
        {
            var go = new GameObject($"Path_{pathId}_{order:00}_{Sanitize(label)}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var marker = go.AddComponent<PathMarker>();
            marker.pathId = pathId;
            marker.order = order;
            marker.label = label;
        }

        public static void Shot(Transform parent, string id, Vector3 cameraPosition, Vector3 lookAt, float fov)
        {
            var go = new GameObject("Shot_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = cameraPosition;
            var shot = go.AddComponent<ShotMarker>();
            shot.shotId = id;
            shot.lookAt = lookAt;
            shot.fieldOfView = fov;
        }

        public static GameObject Portal(Transform parent, string name, Vector3 position, Vector3 size, string scene, string spawnId)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var portal = go.AddComponent<ScenePortal>();
            portal.sceneName = scene;
            portal.spawnId = spawnId;
            return go;
        }

        public static GameObject CreatePlayer(Vector3 position, Quaternion rotation, int playerLayer)
        {
            var go = new GameObject("Player");
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.45f;
            controller.slopeLimit = 50f;
            controller.skinWidth = 0.06f;
            controller.minMoveDistance = 0f;
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Body";
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.transform.SetParent(go.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            capsule.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            capsule.GetComponent<Renderer>().sharedMaterial = Mat("Player", new Color(0.92f, 0.48f, 0.14f), 0.25f);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(go.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.15f, 0.46f);
            nose.transform.localScale = new Vector3(0.16f, 0.16f, 0.55f);
            nose.GetComponent<Renderer>().sharedMaterial = Mat("Facing", new Color(0.98f, 0.9f, 0.15f), 0.3f);

            go.AddComponent<PlayerInputReader>();
            go.AddComponent<PlayerController>();
            SetLayer(go, playerLayer);
            go.transform.SetPositionAndRotation(position, rotation);

            EnsureFolder("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Player.prefab");
            Object.DestroyImmediate(go);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public static OrbitCamera CreateCamera(Transform target, int playerLayer)
        {
            var go = new GameObject("OrbitCamera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.Skybox;
            go.AddComponent<AudioListener>();
            var extra = typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData);
            if (go.GetComponent(extra) == null)
                go.AddComponent(extra);
            var orbit = go.AddComponent<OrbitCamera>();
            orbit.SetTarget(target);
            int mask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
            orbit.collisionMask = mask;
            return orbit;
        }

        public static void ValidatePath(string pathId)
        {
            Physics.SyncTransforms();
            var markers = Object.FindObjectsByType<PathMarker>(FindObjectsSortMode.None);
            var list = new List<PathMarker>();
            foreach (var marker in markers)
            {
                if (marker.pathId == pathId)
                    list.Add(marker);
            }
            list.Sort((a, b) => a.order.CompareTo(b.order));
            if (list.Count < 2)
                throw new System.InvalidOperationException($"Path '{pathId}' needs at least 2 markers, found {list.Count}.");

            for (int i = 0; i < list.Count; i++)
            {
                Vector3 point = list[i].transform.position;
                if (!Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var floor, 8f, ~0, QueryTriggerInteraction.Ignore))
                    throw new System.InvalidOperationException($"No floor under {pathId} '{list[i].label}' at {point}.");
                if (floor.point.y < -0.2f || floor.point.y > 0.75f)
                    throw new System.InvalidOperationException($"Floor y={floor.point.y:0.00} under '{list[i].label}' on {floor.collider.name}.");

                if (i == 0)
                    continue;
                Vector3 from = list[i - 1].transform.position;
                Vector3 to = point;
                Vector3 flat = to - from;
                flat.y = 0f;
                float distance = flat.magnitude;
                if (distance < 0.2f)
                    continue;
                Vector3 direction = flat / distance;
                Vector3 low = from + Vector3.up * 0.7f;
                Vector3 high = from + Vector3.up * 1.55f;
                if (Physics.CapsuleCast(low, high, 0.42f, direction, out var block, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (block.distance < distance - 1.25f)
                    {
                        throw new System.InvalidOperationException(
                            $"Path '{pathId}' blocked between '{list[i - 1].label}' and '{list[i].label}' by {block.collider.name} at {block.point} ({block.distance:0.0}m of {distance:0.0}m).");
                    }
                }
            }
            Debug.Log($"RealmShift: path '{pathId}' is clear ({list.Count} points).");
        }

        public static Material SaveSky(string assetName, Color tint, float thickness, float exposure)
        {
            EnsureFolder("Assets/Materials/Blockout");
            string path = "Assets/Materials/Blockout/" + assetName + ".mat";
            var shader = Shader.Find("Skybox/Procedural");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null)
            {
                mat.shader = shader;
            }
            if (mat.HasProperty("_SkyTint"))
                mat.SetColor("_SkyTint", tint);
            if (mat.HasProperty("_GroundColor"))
                mat.SetColor("_GroundColor", tint * 0.45f);
            if (mat.HasProperty("_AtmosphereThickness"))
                mat.SetFloat("_AtmosphereThickness", thickness);
            if (mat.HasProperty("_Exposure"))
                mat.SetFloat("_Exposure", exposure);
            if (mat.HasProperty("_SunSize"))
                mat.SetFloat("_SunSize", 0.04f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Mesh Bake(string key, System.Func<ProBuilderMesh> build)
        {
            key = Sanitize(key);
            if (MeshCache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var pb = build();
            if (pb == null)
                throw new System.InvalidOperationException("ProBuilder returned null for " + key);
            pb.ToMesh();
            pb.Refresh();
            var source = pb.GetComponent<MeshFilter>().sharedMesh;
            var mesh = Object.Instantiate(source);
            mesh.name = key;
            Object.DestroyImmediate(pb.gameObject);
            string path = "Assets/Meshes/Generated/" + key + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            MeshCache[key] = saved;
            return saved;
        }

        static GameObject PlaceMesh(Transform parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool collider, float? snapBottomTo)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (collider)
            {
                var meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = false;
            }
            if (snapBottomTo.HasValue)
                SnapBottom(go, snapBottomTo.Value);
            return go;
        }

        static void SnapBottom(GameObject go, float groundY)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;
            float lift = groundY - renderer.bounds.min.y;
            go.transform.position += Vector3.up * lift;
        }

        static void RecenterXZ(GameObject go, float x, float z)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;
            var bounds = renderer.bounds;
            float cx = (bounds.min.x + bounds.max.x) * 0.5f;
            float cz = (bounds.min.z + bounds.max.z) * 0.5f;
            go.transform.position += new Vector3(x - cx, 0f, z - cz);
        }

        static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayer(child.gameObject, layer);
        }

        static Font BuiltinFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        public static string Sanitize(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    builder.Append(c);
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                    builder.Append('_');
            }
            if (builder.Length == 0)
                return "mesh";
            return builder.ToString();
        }
    }
}
