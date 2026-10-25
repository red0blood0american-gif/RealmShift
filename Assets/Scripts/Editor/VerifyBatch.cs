using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RealmShift.EditorTools
{
    /// <summary>
    /// Batchmode entry that enters Play mode so VerificationDriver can walk the blockout.
    /// Invoke without -quit: -executeMethod RealmShift.EditorTools.VerifyBatch.Run -realmshiftVerify
    /// </summary>
    [InitializeOnLoad]
    public static class VerifyBatch
    {
        const string StartedFile = "/tmp/realmshift-verify-started.txt";
        const double TimeoutMinutes = 25;

        static VerifyBatch()
        {
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            try
            {
                if (File.Exists(VerificationDriver.DoneFile))
                    File.Delete(VerificationDriver.DoneFile);
                File.WriteAllText(StartedFile, DateTime.UtcNow.Ticks.ToString());

                if (!File.Exists(RealmShiftScenes.HubPath) || !File.Exists(RealmShiftScenes.FrostfellPath))
                    throw new InvalidOperationException("Scenes are missing. Run RealmShift > Build All first.");

                RealmShiftBuild.RefreshBuildSettings();
                EditorSceneManager.OpenScene(RealmShiftScenes.HubPath);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e)
            {
                Debug.LogError("RealmShift verify failed to start: " + e);
                File.WriteAllText(VerificationDriver.DoneFile, "1\n" + e.Message);
                EditorApplication.Exit(1);
            }
        }

        static void Tick()
        {
            if (!HasArg("-realmshiftVerify"))
                return;

            if (File.Exists(VerificationDriver.DoneFile))
            {
                string text = File.ReadAllText(VerificationDriver.DoneFile);
                int code = text.StartsWith("0") ? 0 : 1;
                EditorApplication.Exit(code);
                return;
            }

            if (!File.Exists(StartedFile))
                return;

            if (!long.TryParse(File.ReadAllText(StartedFile).Trim(), out long ticks))
                return;
            var started = new DateTime(ticks, DateTimeKind.Utc);
            if ((DateTime.UtcNow - started).TotalMinutes > TimeoutMinutes)
            {
                File.WriteAllText(VerificationDriver.DoneFile, "1\nTimed out after " + TimeoutMinutes + " minutes.");
                EditorApplication.Exit(1);
            }
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
    }
}
