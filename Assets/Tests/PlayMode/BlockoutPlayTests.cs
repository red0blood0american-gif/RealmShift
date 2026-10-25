using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RealmShift.Tests
{
    /// <summary>
    /// Loads the generated scenes and walks the same routes the batch verifier uses.
    /// Scenes must already be built and listed in EditorBuildSettings.
    /// </summary>
    public class BlockoutPlayTests
    {
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Hub_HasEightLabeledDoorsAndOnlyFrostfellIsOpen()
        {
            yield return Load(RealmShiftScenes.Hub);
            var doors = Object.FindObjectsByType<WorldDoor>(FindObjectsSortMode.None);
            Assert.AreEqual(8, doors.Length, "The flickering hall should have one doorway per world.");

            string[] names =
            {
                "Frostfell Fair",
                "Canopy Tangle",
                "Sunken Nave",
                "Gale Spires",
                "Cinder Flats",
                "Burrowmere",
                "Rustworks",
                "Spare"
            };
            foreach (string name in names)
            {
                WorldDoor match = null;
                foreach (var door in doors)
                {
                    if (door.worldName == name)
                        match = door;
                }
                Assert.IsNotNull(match, "Missing door " + name);
                if (name == "Frostfell Fair")
                    Assert.IsTrue(match.open, name);
                else
                    Assert.IsFalse(match.open, name);
            }

            int frostfellPortals = 0;
            var portals = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None);
            foreach (var portal in portals)
            {
                if (portal.sceneName == RealmShiftScenes.Frostfell)
                    frostfellPortals++;
            }
            Assert.AreEqual(1, frostfellPortals, "Only the Frostfell doorway should load a scene.");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Hub_PlayerGroundedAndReachesFrostfellDoor()
        {
            yield return Load(RealmShiftScenes.Hub);
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player);
            yield return BlockoutWalker.WaitGrounded(player, 3f);
            Assert.IsTrue(player.IsGrounded);
            Assert.Greater(player.FeetY, -0.35f);
            yield return BlockoutWalker.FollowPath(player, "hub", -0.75f);
            Assert.Greater(player.LowestFeetY, -0.75f);
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Frostfell_PlayerGroundedAndWalksTheFair()
        {
            yield return Load(RealmShiftScenes.Frostfell);
            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.IsNotNull(player);
            yield return BlockoutWalker.WaitGrounded(player, 3f);
            Assert.IsTrue(player.IsGrounded);
            Assert.Greater(player.FeetY, -0.35f);
            yield return BlockoutWalker.FollowPath(player, "frostfell", -0.75f);
            Assert.Greater(player.LowestFeetY, -0.75f);
        }

        static IEnumerator Load(string scene)
        {
            var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            Assert.IsNotNull(op, "Scene '" + scene + "' is not in the build settings. Run RealmShift > Build All.");
            while (!op.isDone)
                yield return null;
            yield return null;
            yield return null;
        }
    }
}
