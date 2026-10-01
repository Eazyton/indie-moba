using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Match;

namespace IndieMoba.Map
{
    public sealed class StructureSlotMarker : MonoBehaviour
    {
        [SerializeField] private string slotId;
        [SerializeField] private Team team;
        [SerializeField] private LaneId lane;
        [SerializeField] private StructureTier tier;
        [Min(0f)] [SerializeField] private float previewRange = 7.8125f;

        public string SlotId => slotId;
        public Team Team => team;
        public LaneId Lane => lane;
        public StructureTier Tier => tier;
        public float PreviewRange => previewRange;
        public Vector2 Position => transform.position;

        public void Configure(string id, Team slotTeam, LaneId slotLane, StructureTier slotTier)
        {
            slotId = id;
            team = slotTeam;
            lane = slotLane;
            tier = slotTier;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = team == Team.Blue ? new Color(0.3f, 0.6f, 1f, 0.8f) : new Color(1f, 0.35f, 0.3f, 0.8f);
            MapGizmoUtility.DrawCircle(transform.position, previewRange, 32);
        }
    }
}
