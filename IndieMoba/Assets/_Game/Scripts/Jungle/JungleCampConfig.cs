using System;
using UnityEngine;

namespace IndieMoba.Jungle
{
    public enum JungleCampType
    {
        Small = 0,
        Buff = 1
    }

    // Authoring data used by the editor wiring to populate a camp. Runtime uses the scene members.
    [Serializable]
    public sealed class JungleCampMember
    {
        public GameObject monsterPrefab;
        public Vector2 localOffset;
        public bool buffCarrier;
    }

    // Camp rules (prototype playtest values from the Phase 5B design, not values from another game).
    [CreateAssetMenu(menuName = "IndieMoba/Jungle/Camp Config", fileName = "JungleCampConfig")]
    public sealed class JungleCampConfig : ScriptableObject
    {
        [SerializeField] private string displayName = "Camp";
        [SerializeField] private JungleCampType campType = JungleCampType.Small;
        [Min(0f)] [SerializeField] private float firstSpawnTime = 20f;
        [Min(0f)] [SerializeField] private float respawnTime = 55f;
        [Tooltip("Monsters never leave this radius around the camp center; targets outside it are dropped.")]
        [Min(0.5f)] [SerializeField] private float leashRadius = 6.5f;
        [Tooltip("When the current target becomes invalid, heroes within this radius of the camp center can be picked.")]
        [Min(0f)] [SerializeField] private float acquireRadius = 5f;
        [Tooltip("Seconds without hero damage before the camp resets.")]
        [Min(0.1f)] [SerializeField] private float resetAfterNoDamage = 6f;
        [Min(0.1f)] [SerializeField] private float returnSpeed = 7f;
        [Tooltip("Seconds to regenerate to full while returning; HP snaps to full on arrival.")]
        [Min(0.01f)] [SerializeField] private float resetHealDuration = 1.5f;
        [Tooltip("If members are not home after this many seconds they are snapped home.")]
        [Min(0.1f)] [SerializeField] private float returnTimeout = 4f;
        [Min(1)] [SerializeField] private int retargetIntervalTicks = 15;
        [SerializeField] private JungleCampMember[] members = Array.Empty<JungleCampMember>();
        [SerializeField] private JungleRewardProfile reward;

        public string DisplayName => displayName;
        public JungleCampType CampType => campType;
        public float FirstSpawnTime => firstSpawnTime;
        public float RespawnTime => respawnTime;
        public float LeashRadius => leashRadius;
        public float AcquireRadius => acquireRadius;
        public float ResetAfterNoDamage => resetAfterNoDamage;
        public float ReturnSpeed => returnSpeed;
        public float ResetHealDuration => resetHealDuration;
        public float ReturnTimeout => returnTimeout;
        public int RetargetIntervalTicks => retargetIntervalTicks;
        public JungleCampMember[] Members => members;
        public JungleRewardProfile Reward => reward;
    }
}
