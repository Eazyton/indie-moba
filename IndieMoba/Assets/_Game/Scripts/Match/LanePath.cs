using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Match
{
    public enum LaneId
    {
        Mid = 0,
        Top = 1,
        Bottom = 2
    }

    [DisallowMultipleComponent]
    public sealed class LanePath : MonoBehaviour
    {
        [SerializeField] private LaneId laneId;
        [SerializeField] private Transform[] waypoints;

        public LaneId LaneId => laneId;
        public Transform[] Waypoints => waypoints;

        public void GetWaypoints(Team team, List<Vector2> results)
        {
            results.Clear();
            if (waypoints == null)
            {
                return;
            }
            if (team == Team.Red)
            {
                for (int i = waypoints.Length - 1; i >= 0; i--)
                {
                    if (waypoints[i] != null)
                    {
                        results.Add(waypoints[i].position);
                    }
                }
            }
            else
            {
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null)
                    {
                        results.Add(waypoints[i].position);
                    }
                }
            }
        }

        public Vector2 GetLaneNormal(Team team)
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                return Vector2.up;
            }
            int firstIndex = team == Team.Red ? waypoints.Length - 1 : 0;
            int secondIndex = team == Team.Red ? waypoints.Length - 2 : 1;
            Transform firstTransform = waypoints[firstIndex];
            Transform secondTransform = waypoints[secondIndex];
            if (firstTransform == null || secondTransform == null)
            {
                return Vector2.up;
            }
            Vector2 direction = secondTransform.position - firstTransform.position;
            if (direction.sqrMagnitude < 1e-8f)
            {
                return Vector2.up;
            }
            return new Vector2(-direction.y, direction.x).normalized;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null)
            {
                return;
            }
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                {
                    continue;
                }
                Gizmos.DrawWireSphere(waypoints[i].position, 0.25f);
                if (i > 0 && waypoints[i - 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
                }
            }
        }
    }
}
