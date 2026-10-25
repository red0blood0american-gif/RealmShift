using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealmShift
{
    /// <summary>
    /// Batch play-mode walk used by -executeMethod VerifyBatch.Run -realmshiftVerify.
    /// Loads each scene, checks the spawn is grounded, walks the main path, and saves screenshots.
    /// </summary>
    public class VerificationDriver : MonoBehaviour
    {
        public const string DoneFile = "/tmp/realmshift-verify-done.txt";
        public const string ReportFile = "/opt/cursor/artifacts/verify-report.txt";

        static bool started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (started)
                return;
            if (!HasArg("-realmshiftVerify"))
                return;
            started = true;
            var go = new GameObject("VerificationDriver");
            DontDestroyOnLoad(go);
            go.AddComponent<VerificationDriver>();
        }

        static bool HasArg(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == arg)
                    return true;
            }
            return false;
        }

        void Start()
        {
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var log = new StringBuilder();
            int code = 0;
            log.AppendLine("RealmShift verification");
            log.AppendLine("unity " + Application.unityVersion);
            IEnumerator routine = RunAll(log);
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                        break;
                    current = routine.Current;
                }
                catch (Exception e)
                {
                    code = 1;
                    log.AppendLine("FAIL " + e.Message);
                    UnityEngine.Debug.LogException(e);
                    break;
                }
                yield return current;
            }

            log.AppendLine(code == 0 ? "RESULT PASS" : "RESULT FAIL");
            try
            {
                var dir = Path.GetDirectoryName(ReportFile);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(ReportFile, log.ToString());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Could not write " + ReportFile + ": " + e.Message);
            }
            File.WriteAllText(DoneFile, code + "\n" + log);
            UnityEngine.Debug.Log(log.ToString());
        }

        IEnumerator RunAll(StringBuilder log)
        {
            yield return PlayScene(log, RealmShiftScenes.Hub, "hub", -0.75f, new[]
            {
                "hub-courtyard",
                "hub-flickering-hall"
            }, shootBeforeWalk: true);

            yield return null;
            UnityEngine.SceneManagement.SceneManager.LoadScene(RealmShiftScenes.Frostfell);
            yield return null;
            yield return null;

            yield return PlayScene(log, RealmShiftScenes.Frostfell, "frostfell", -0.75f, new[]
            {
                "frostfell-spawn",
                "frostfell-overview"
            }, shootBeforeWalk: true, recordWalk: true);
        }

        IEnumerator PlayScene(StringBuilder log, string sceneName, string pathId, float minY, string[] shots, bool shootBeforeWalk, bool recordWalk = false)
        {
            float wait = 0f;
            while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName && wait < 5f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != sceneName)
                throw new InvalidOperationException("Active scene is " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " expected " + sceneName);

            yield return null;
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
                throw new InvalidOperationException("No player in " + sceneName);
            yield return BlockoutWalker.WaitGrounded(player, 3f);
            log.AppendLine($"PASS {sceneName} grounded y={player.FeetY:0.00} pos={player.transform.position}");

            if (shootBeforeWalk)
            {
                for (int i = 0; i < shots.Length; i++)
                    yield return Shoot(log, shots[i]);
            }

            if (recordWalk && pathId == "frostfell")
                StartCoroutine(RecordWalkClip());

            yield return BlockoutWalker.FollowPath(player, pathId, minY);
            log.AppendLine($"PASS {sceneName} path '{pathId}' lowestY={player.LowestFeetY:0.00} end={player.transform.position}");

            if (!shootBeforeWalk)
            {
                for (int i = 0; i < shots.Length; i++)
                    yield return Shoot(log, shots[i]);
            }
        }

        IEnumerator Shoot(StringBuilder log, string shotId)
        {
            ShotMarker shot = null;
            var shots = FindObjectsByType<ShotMarker>(FindObjectsSortMode.None);
            foreach (var candidate in shots)
            {
                if (candidate.shotId == shotId)
                    shot = candidate;
            }
            if (shot == null)
                throw new InvalidOperationException("Missing shot marker " + shotId);

            var cam = Camera.main;
            if (cam == null)
                throw new InvalidOperationException("No main camera for " + shotId);
            var orbit = cam.GetComponent<OrbitCamera>();
            string path = Path.Combine(ShotDirectory(), shotId + ".png");
            bool blank = false;
            yield return Capture(cam, orbit, shot.transform.position, shot.lookAt, shot.fieldOfView, path, resultBlank => blank = resultBlank);
            log.AppendLine((blank ? "SHOT-BLANK " : "SHOT ") + path);
        }

        static string ShotDirectory()
        {
            const string preferred = "/opt/cursor/artifacts/screenshots";
            try
            {
                Directory.CreateDirectory(preferred);
                return preferred;
            }
            catch (Exception)
            {
                const string fallback = "/tmp/realmshift-artifacts/screenshots";
                Directory.CreateDirectory(fallback);
                return fallback;
            }
        }

        static IEnumerator Capture(Camera cam, OrbitCamera orbit, Vector3 position, Vector3 lookAt, float fov, string path, Action<bool> done)
        {
            var previousPos = cam.transform.position;
            var previousRot = cam.transform.rotation;
            float previousFov = cam.fieldOfView;
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var previousTarget = cam.targetTexture;
            if (orbit != null)
                orbit.suspend = true;
            cam.fieldOfView = fov;
            cam.targetTexture = rt;
            cam.transform.position = position;
            cam.transform.LookAt(lookAt);
            yield return null;
            yield return null;
            cam.transform.position = position;
            cam.transform.LookAt(lookAt);
            Submit(cam, rt);

            bool blank = true;
            try
            {
                var previousActive = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previousActive;
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                blank = IsBlank(tex);
                UnityEngine.Object.Destroy(tex);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Capture failed for " + path + ": " + e.Message);
            }

            cam.targetTexture = previousTarget;
            cam.fieldOfView = previousFov;
            cam.transform.SetPositionAndRotation(previousPos, previousRot);
            if (orbit != null)
                orbit.suspend = false;
            UnityEngine.Object.Destroy(rt);
            done?.Invoke(blank);
            yield return null;
        }

        static void Submit(Camera cam, RenderTexture rt)
        {
            var request = new RenderPipeline.StandardRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, request))
                RenderPipeline.SubmitRenderRequest(cam, request);
        }

        static bool IsBlank(Texture2D tex)
        {
            var pixels = tex.GetPixels32();
            float min = 1f;
            float max = 0f;
            for (int i = 0; i < pixels.Length; i += 80)
            {
                float l = pixels[i].r + pixels[i].g + pixels[i].b;
                l /= 3f * 255f;
                if (l < min) min = l;
                if (l > max) max = l;
            }
            return (max - min) < 0.02f;
        }

        IEnumerator RecordWalkClip()
        {
            var cam = Camera.main;
            if (cam == null)
                yield break;
            string dir = Path.Combine(ShotDirectory(), "walk-frames");
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (Exception)
            {
                yield break;
            }

            int frames = 36;
            for (int i = 0; i < frames; i++)
            {
                yield return new WaitForSeconds(0.12f);
                var rt = new RenderTexture(640, 360, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                yield return null;
                try
                {
                    Submit(cam, rt);
                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    tex.Apply();
                    File.WriteAllBytes(Path.Combine(dir, $"frame_{i:00}.png"), tex.EncodeToPNG());
                    Destroy(tex);
                }
                catch (Exception)
                {
                    // A missing frame should not fail the walk.
                }
                RenderTexture.active = null;
                cam.targetTexture = prev;
                Destroy(rt);
            }

            try
            {
                string mp4 = Path.Combine(ShotDirectory(), "frostfell-walk.mp4");
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-y -framerate 8 -i frame_%02d.png -c:v libx264 -pix_fmt yuv420p \"" + mp4 + "\"",
                    WorkingDirectory = dir,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var process = Process.Start(psi);
                process?.WaitForExit(30000);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Walk clip encode skipped: " + e.Message);
            }
        }
    }
}
