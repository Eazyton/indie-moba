using System;
using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Characters;
using IndieMoba.Core;

namespace IndieMoba.Combat
{
    public sealed class AbilitySlotRuntime
    {
        public readonly AbilitySlot Slot;
        public readonly AbilityConfig Config;
        public readonly IAbilityBehaviour Behaviour;
        public Cooldown Cooldown;
        public bool WasActive;

        public AbilitySlotRuntime(AbilitySlot slot, AbilityConfig config)
        {
            Slot = slot;
            Config = config;
            Behaviour = config != null ? AbilityBehaviourFactory.Create(config.Kind) : null;
        }

        public bool IsConfigured => Config != null && Behaviour != null;
        public bool IsActive => Behaviour != null && Behaviour.IsActive;
    }

    [DisallowMultipleComponent]
    public sealed class HeroCombat : MonoBehaviour, ISimulationSystem
    {
        public const int SlotCount = 5;

        [SerializeField] private SimulationTickRunner runner;
        [SerializeField] private CombatWorld world;
        [SerializeField] private HeroActor actor;
        [SerializeField] private Health health;
        [SerializeField] private CombatTarget self;
        [SerializeField] private MonoBehaviour abilityInputBehaviour;
        [SerializeField] private Vector2 aimOriginOffset = new Vector2(0f, 0.5f);
        [SerializeField] private AbilityConfig basicAttack;
        [SerializeField] private AbilityConfig ability1;
        [SerializeField] private AbilityConfig ability2;
        [SerializeField] private AbilityConfig ability3;
        [SerializeField] private AbilityConfig ultimate;

        private readonly AbilitySlotRuntime[] slots = new AbilitySlotRuntime[SlotCount];
        private readonly List<AbilityCommand> commandBuffer = new List<AbilityCommand>();
        private readonly List<AbilityCommand> pendingCommands = new List<AbilityCommand>();
        private IAbilityInputSource inputSource;

        public event Action<AbilitySlot, AbilityConfig> AbilityStarted;
        public event Action<AbilitySlot, AbilityConfig> AbilityCompleted;
        public event Action<AbilitySlot, float> CooldownStarted;
        public event Action<AbilitySlot, AbilityFailReason> ActivationDenied;

        public int SimulationOrder => Core.SimulationOrder.Abilities;
        public IReadOnlyList<AbilitySlotRuntime> Slots => slots;
        public Health Health => health;
        public HeroActor Actor => actor;
        public Team Team => self != null ? self.Team : Team.Neutral;
        public CombatWorld World => world;

        public AbilitySlotRuntime GetSlot(AbilitySlot slot) => slots[(int)slot];

        public void SetInputSource(IAbilityInputSource source)
        {
            inputSource = source;
        }

        public void RequestAbility(in AbilityCommand command)
        {
            pendingCommands.Add(command);
        }

        public void SimulateTick(float deltaTime, int tick)
        {
            bool alive = health == null || health.IsAlive;
            for (int i = 0; i < slots.Length; i++)
            {
                AbilitySlotRuntime runtime = slots[i];
                runtime.Cooldown.Tick(deltaTime);
                if (!runtime.IsConfigured)
                {
                    continue;
                }
                if (runtime.WasActive)
                {
                    runtime.Behaviour.Tick(BuildContext(runtime.Slot, default), runtime.Config, deltaTime);
                    if (!runtime.IsActive)
                    {
                        runtime.WasActive = false;
                        AbilityCompleted?.Invoke(runtime.Slot, runtime.Config);
                    }
                }
            }

            commandBuffer.Clear();
            if (inputSource != null)
            {
                inputSource.DrainCommands(commandBuffer);
            }
            commandBuffer.AddRange(pendingCommands);
            pendingCommands.Clear();
            if (!alive)
            {
                return;
            }

            uint handled = 0;
            for (int i = commandBuffer.Count - 1; i >= 0; i--)
            {
                AbilityCommand command = commandBuffer[i];
                uint bit = 1u << (int)command.Slot;
                if ((handled & bit) != 0)
                {
                    continue;
                }
                handled |= bit;
                TryActivate(command);
            }
        }

        private void TryActivate(in AbilityCommand command)
        {
            AbilitySlotRuntime runtime = slots[(int)command.Slot];
            AbilityFailReason reason;
            if (!runtime.IsConfigured)
            {
                reason = AbilityFailReason.NotConfigured;
            }
            else if (!runtime.Cooldown.IsReady)
            {
                reason = AbilityFailReason.OnCooldown;
            }
            else if (IsBusy())
            {
                reason = AbilityFailReason.Busy;
            }
            else if (runtime.Behaviour.TryExecute(BuildContext(command.Slot, command), runtime.Config, out reason))
            {
                runtime.Cooldown.Start(runtime.Config.Cooldown);
                AbilityStarted?.Invoke(runtime.Slot, runtime.Config);
                CooldownStarted?.Invoke(runtime.Slot, runtime.Config.Cooldown);
                if (runtime.IsActive)
                {
                    runtime.WasActive = true;
                }
                else
                {
                    AbilityCompleted?.Invoke(runtime.Slot, runtime.Config);
                }
                return;
            }
            ActivationDenied?.Invoke(command.Slot, reason);
        }

        private bool IsBusy()
        {
            return actor != null && actor.IsForcedMoving;
        }

        private AbilityContext BuildContext(AbilitySlot slot, in AbilityCommand command)
        {
            Vector2 position = actor != null ? actor.State.Position : (Vector2)transform.position;
            Vector2 facing = actor != null ? actor.State.Facing : Vector2.right;
            return new AbilityContext(slot, gameObject, position, position + aimOriginOffset, command.AimPoint, command.HasAim,
                facing, Team, world, health, actor, self, command.Targeting, command.ExplicitTarget as ICombatTarget);
        }

        private void HandleDied(Health _)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Behaviour != null)
                {
                    slots[i].Behaviour.Cancel();
                }
            }
            if (actor != null)
            {
                actor.MovementEnabled = false;
            }
        }

        private void HandleRevived(Health _)
        {
            if (actor != null)
            {
                actor.MovementEnabled = true;
            }
        }

        private void Awake()
        {
            if (actor == null) actor = GetComponent<HeroActor>();
            if (health == null) health = GetComponent<Health>();
            if (self == null) self = GetComponent<CombatTarget>();
            inputSource = abilityInputBehaviour as IAbilityInputSource;
            slots[(int)AbilitySlot.BasicAttack] = new AbilitySlotRuntime(AbilitySlot.BasicAttack, basicAttack);
            slots[(int)AbilitySlot.Ability1] = new AbilitySlotRuntime(AbilitySlot.Ability1, ability1);
            slots[(int)AbilitySlot.Ability2] = new AbilitySlotRuntime(AbilitySlot.Ability2, ability2);
            slots[(int)AbilitySlot.Ability3] = new AbilitySlotRuntime(AbilitySlot.Ability3, ability3);
            slots[(int)AbilitySlot.Ultimate] = new AbilitySlotRuntime(AbilitySlot.Ultimate, ultimate);
        }

        private void OnEnable()
        {
            if (runner == null) runner = FindAnyObjectByType<SimulationTickRunner>();
            if (world == null) world = FindAnyObjectByType<CombatWorld>();
            if (runner != null) runner.Register(this);
            if (health != null)
            {
                health.Died += HandleDied;
                health.Revived += HandleRevived;
            }
        }

        private void OnDisable()
        {
            if (runner != null) runner.Unregister(this);
            if (health != null)
            {
                health.Died -= HandleDied;
                health.Revived -= HandleRevived;
            }
        }

        private void OnValidate()
        {
            if (abilityInputBehaviour != null && !(abilityInputBehaviour is IAbilityInputSource))
            {
                Debug.LogWarning($"{name}: abilityInputBehaviour does not implement IAbilityInputSource.", this);
                abilityInputBehaviour = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (basicAttack != null)
            {
                Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.6f);
                Gizmos.DrawWireSphere(transform.position, basicAttack.Range);
            }
            if (ultimate != null && ultimate.CastRange > 0f)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.4f);
                Gizmos.DrawWireSphere(transform.position, ultimate.CastRange);
            }
        }
    }
}
