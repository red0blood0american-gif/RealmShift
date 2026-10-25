using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealmShift
{
    /// <summary>Drives the player along PathMarkers and fails if they leave the floor.</summary>
    public static class BlockoutWalker
    {
        public static IEnumerator WaitGrounded(PlayerController player, float timeoutSeconds)
        {
            if (player == null)
                throw new InvalidOperationException("No player in the scene.");
            float t = 0f;
            while (t < timeoutSeconds)
            {
                if (player.IsGrounded && player.FeetY > -0.35f && player.FeetY < 1.4f)
                {
                    player.ResetFloorTrail();
                    yield break;
                }
                t += Mathf.Min(Time.deltaTime, 0.05f);
                yield return null;
            }
            throw new InvalidOperationException(
                $"Player was not grounded after {timeoutSeconds:0.0}s at {player.transform.position} grounded={player.IsGrounded}.");
        }

        public static IEnumerator FollowPath(PlayerController player, string pathId, float minFeetY)
        {
            if (player == null)
                throw new InvalidOperationException("No player.");
            var markers = Collect(pathId);
            if (markers.Count == 0)
                throw new InvalidOperationException($"Path '{pathId}' has no markers.");

            player.ResetFloorTrail();
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];
                Vector3 target = marker.transform.position;
                float distance = Horizontal(player.transform.position, target);
                float budget = Mathf.Max(8f, distance / 3.5f + 5f);
                float spent = 0f;
                float best = distance;
                float sinceProgress = 0f;
                player.DriveTo(target, sprint: true);
                while (Horizontal(player.transform.position, target) > 1.7f)
                {
                    if (player.LowestFeetY < minFeetY)
                    {
                        player.StopDriving();
                        throw new InvalidOperationException(
                            $"Fell to y={player.LowestFeetY:0.00} on '{pathId}' heading to {marker.label} ({marker.order}) at {player.transform.position}.");
                    }
                    float now = Horizontal(player.transform.position, target);
                    float step = Mathf.Min(Time.deltaTime, 0.05f);
                    spent += step;
                    if (now < best - 0.25f)
                    {
                        best = now;
                        sinceProgress = 0f;
                    }
                    else
                    {
                        sinceProgress += step;
                    }
                    if (sinceProgress > 4f || spent > budget)
                    {
                        string blocker = DescribeBlocker(player.transform.position, target);
                        player.StopDriving();
                        throw new InvalidOperationException(
                            $"Stuck going to {marker.label} ({marker.order}) on '{pathId}'. pos={player.transform.position} dist={now:0.0} blocker={blocker}");
                    }
                    player.DriveTo(target, sprint: true);
                    yield return null;
                }
            }
            player.StopDriving();
            if (player.LowestFeetY < minFeetY)
                throw new InvalidOperationException($"Path '{pathId}' dipped to y={player.LowestFeetY:0.00}.");
        }

        public static List<PathMarker> Collect(string pathId)
        {
            var all = UnityEngine.Object.FindObjectsByType<PathMarker>(FindObjectsSortMode.None);
            var list = new List<PathMarker>();
            foreach (var marker in all)
            {
                if (marker.pathId == pathId)
                    list.Add(marker);
            }
            list.Sort((a, b) => a.order.CompareTo(b.order));
            return list;
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        static string DescribeBlocker(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return "none";
            dir.Normalize();
            Vector3 origin = from + Vector3.up * 0.9f;
            if (Physics.Raycast(origin, dir, out var hit, 2.2f, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.name;
            return "none";
        }
    }
}
