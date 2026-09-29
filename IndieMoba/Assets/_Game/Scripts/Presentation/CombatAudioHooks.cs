using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Presentation
{
    public sealed class CombatAudioHooks : MonoBehaviour
    {
        [SerializeField] private HeroCombat combat;
        [SerializeField] private CombatWorld world;
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip basicAttackClip;
        [SerializeField] private AudioClip ability1Clip;
        [SerializeField] private AudioClip ability2Clip;
        [SerializeField] private AudioClip ability3Clip;
        [SerializeField] private AudioClip ultimateClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip denyClip;
        [SerializeField] private float volume = 1f;

        private void Awake()
        {
            if (combat == null)
            {
                combat = GetComponentInParent<HeroCombat>();
            }
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (combat == null)
            {
                combat = GetComponentInParent<HeroCombat>();
            }
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
            if (combat != null)
            {
                combat.AbilityStarted += HandleAbilityStarted;
                combat.ActivationDenied += HandleActivationDenied;
            }
            if (world != null)
            {
                world.DamageApplied += HandleDamageApplied;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.AbilityStarted -= HandleAbilityStarted;
                combat.ActivationDenied -= HandleActivationDenied;
            }
            if (world != null)
            {
                world.DamageApplied -= HandleDamageApplied;
            }
        }

        private void HandleAbilityStarted(AbilitySlot slot, AbilityConfig config)
        {
            AudioClip clip = null;
            switch (slot)
            {
                case AbilitySlot.BasicAttack: clip = basicAttackClip; break;
                case AbilitySlot.Ability1: clip = ability1Clip; break;
                case AbilitySlot.Ability2: clip = ability2Clip; break;
                case AbilitySlot.Ability3: clip = ability3Clip; break;
                case AbilitySlot.Ultimate: clip = ultimateClip; break;
            }
            Play(clip);
        }

        private void HandleActivationDenied(AbilitySlot slot, AbilityFailReason reason)
        {
            if (slot == AbilitySlot.BasicAttack)
            {
                return;
            }
            Play(denyClip);
        }

        private void HandleDamageApplied(ICombatTarget target, DamageResult result)
        {
            if (combat != null && result.Info.Source == combat.gameObject)
            {
                Play(hitClip);
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
