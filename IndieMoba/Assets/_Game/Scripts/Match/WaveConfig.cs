using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Minions;

namespace IndieMoba.Match
{
    [CreateAssetMenu(menuName = "IndieMoba/Match/Wave Config", fileName = "WaveConfig")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float firstWaveDelay = 4f;
        [Min(0f)] [SerializeField] private float waveInterval = 30f;
        [Min(0f)] [SerializeField] private float spawnGap = 0.7f;
        [SerializeField] private MinionAttackType[] composition = { MinionAttackType.Melee, MinionAttackType.Ranged, MinionAttackType.Melee, MinionAttackType.Ranged, MinionAttackType.Melee };
        [SerializeField] private float[] lateralOffsets = { -0.75f, 0f, 0.75f };
        [Min(0)] [SerializeField] private int maxWaves;

        public float FirstWaveDelay => firstWaveDelay;
        public float WaveInterval => waveInterval;
        public float SpawnGap => spawnGap;
        public IReadOnlyList<MinionAttackType> Composition => composition;
        public IReadOnlyList<float> LateralOffsets => lateralOffsets;
        public int MaxWaves => maxWaves;
    }
}
