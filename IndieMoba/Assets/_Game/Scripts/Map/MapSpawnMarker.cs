using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Match;

namespace IndieMoba.Map
{
    public sealed class MapSpawnMarker : MonoBehaviour
    {
        [SerializeField] private MapSpawnKind kind;
        [SerializeField] private Team team;
        [SerializeField] private LaneId lane;

        public MapSpawnKind Kind => kind;
        public Team Team => team;
        public LaneId Lane => lane;
        public Vector2 Position => transform.position;

        public void Configure(MapSpawnKind spawnKind, Team spawnTeam, LaneId spawnLane)
        {
            kind = spawnKind;
            team = spawnTeam;
            lane = spawnLane;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = team == Team.Blue ? new Color(0.3f, 0.6f, 1f, 1f) : new Color(1f, 0.35f, 0.3f, 1f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * (kind == MapSpawnKind.Fountain ? 2f : 1f));
        }
    }
}
