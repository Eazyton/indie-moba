using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Jungle
{
    // Runs every jungle camp on the shared simulation clock (SimulationOrder.Neutral), routes combat
    // events to camps (hero aggro, kills) and grants carrier buffs. MonsterKilled / CampCleared are the
    // reward hooks for future economy (gold/XP are out of scope in Phase 5B).
    // Kill attribution uses DamageInfo.Source (GameObject); multiplayer will need stable entity ids.
    [DisallowMultipleComponent]
    public sealed class JungleDirector : MonoBehaviour, ISimulationSystem
    {
        [SerializeField] private SimulationTickRunner runner;
        [SerializeField] private CombatWorld world;
        [SerializeField] private JungleCamp[] camps = Array.Empty<JungleCamp>();

        private readonly List<JungleCamp> runtimeCamps = new List<JungleCamp>();
        private readonly Dictionary<ICombatTarget, JungleMonster> monsterByTarget = new Dictionary<ICombatTarget, JungleMonster>();
        private readonly Dictionary<GameObject, CombatTarget> sourceCache = new Dictionary<GameObject, CombatTarget>();
        private float elapsed;
        private bool initialized;
        private bool subscribed;

        public event Action<JungleCamp, JungleCampState> CampStateChanged;
        public event Action<JungleMonster, GameObject> MonsterKilled;
        public event Action<JungleCamp, GameObject> CampCleared;
        public event Action<JungleCamp, StatusEffectController, StatusEffectDefinition> BuffGranted;

        public int SimulationOrder => Core.SimulationOrder.Neutral;
        public IReadOnlyList<JungleCamp> Camps => runtimeCamps;
        public float ElapsedTime => elapsed;
        public CombatWorld World => world;

        public void SimulateTick(float deltaTime, int tick)
        {
            if (!initialized)
            {
                Initialize();
            }
            elapsed += deltaTime;
            for (int i = 0; i < runtimeCamps.Count; i++)
            {
                try
                {
                    runtimeCamps[i].Tick(deltaTime, tick);
                }
                catch (Exception exception)
                {
                    // One faulty camp must not stop the others.
                    Debug.LogException(exception, runtimeCamps[i]);
                }
            }
        }

        // Debug: skip every pending spawn/respawn timer.
        public void ForceSpawnAll()
        {
            for (int i = 0; i < runtimeCamps.Count; i++)
            {
                runtimeCamps[i].ForceSpawnNow();
            }
        }

        private void Initialize()
        {
            initialized = true;
            runtimeCamps.Clear();
            monsterByTarget.Clear();
            JungleCamp[] source = camps != null && camps.Length > 0 ? camps : GetComponentsInChildren<JungleCamp>(true);
            for (int i = 0; i < source.Length; i++)
            {
                JungleCamp camp = source[i];
                if (camp == null || runtimeCamps.Contains(camp) || !camp.Initialize(runner, world))
                {
                    continue;
                }
                runtimeCamps.Add(camp);
                camp.StateChanged += HandleCampStateChanged;
                for (int m = 0; m < camp.Members.Count; m++)
                {
                    JungleMonster monster = camp.Members[m];
                    if (monster.CombatTarget != null)
                    {
                        monsterByTarget[monster.CombatTarget] = monster;
                    }
                }
            }
        }

        private void HandleDamageApplied(ICombatTarget target, DamageResult result)
        {
            if (target == null || !monsterByTarget.TryGetValue(target, out JungleMonster monster))
            {
                return;
            }
            JungleCamp camp = monster.Camp;
            if (camp == null)
            {
                return;
            }
            GameObject sourceObject = result.Info.Source;
            CombatTarget attacker = ResolveSource(sourceObject);
            if (attacker != null && attacker.Kind == CombatTargetKind.Hero && !result.Blocked)
            {
                camp.NotifyHeroDamage(attacker);
            }
            if (!result.Killed)
            {
                return;
            }
            MonsterKilled?.Invoke(monster, sourceObject);
            if (monster == camp.BuffCarrier)
            {
                GrantCarrierBuff(camp, attacker);
            }
            if (camp.NotifyMonsterKilled(monster))
            {
                CampCleared?.Invoke(camp, sourceObject);
            }
        }

        // The living hero who lands the killing blow on the carrier receives the camp buff.
        private void GrantCarrierBuff(JungleCamp camp, CombatTarget killer)
        {
            JungleRewardProfile reward = camp.Config != null ? camp.Config.Reward : null;
            if (reward == null || reward.CarrierBuff == null || killer == null || killer.Kind != CombatTargetKind.Hero || !killer.IsAlive)
            {
                return;
            }
            StatusEffectController controller = killer.GetComponent<StatusEffectController>();
            if (controller == null)
            {
                Debug.LogWarning($"{killer.name} killed a buff carrier but has no StatusEffectController; buff not granted.", killer);
                return;
            }
            if (controller.Apply(reward.CarrierBuff, camp.gameObject, Team.Neutral))
            {
                BuffGranted?.Invoke(camp, controller, reward.CarrierBuff);
            }
        }

        private CombatTarget ResolveSource(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return null;
            }
            if (!sourceCache.TryGetValue(sourceObject, out CombatTarget cached) || cached == null)
            {
                cached = sourceObject.GetComponent<CombatTarget>();
                sourceCache[sourceObject] = cached;
            }
            return cached;
        }

        private void HandleCampStateChanged(JungleCamp camp, JungleCampState state)
        {
            CampStateChanged?.Invoke(camp, state);
        }

        private void OnEnable()
        {
            if (runner == null) runner = FindAnyObjectByType<SimulationTickRunner>();
            if (world == null) world = FindAnyObjectByType<CombatWorld>();
            if (runner == null || world == null)
            {
                Debug.LogError($"{name}: JungleDirector requires a SimulationTickRunner and a CombatWorld.", this);
                return;
            }
            runner.Register(this);
            world.DamageApplied += HandleDamageApplied;
            subscribed = true;
        }

        private void Start()
        {
            if (!initialized && runner != null && world != null)
            {
                Initialize();
            }
        }

        private void OnDisable()
        {
            if (runner != null) runner.Unregister(this);
            if (subscribed && world != null) world.DamageApplied -= HandleDamageApplied;
            subscribed = false;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeCamps.Count; i++)
            {
                if (runtimeCamps[i] != null)
                {
                    runtimeCamps[i].StateChanged -= HandleCampStateChanged;
                }
            }
        }
    }
}
