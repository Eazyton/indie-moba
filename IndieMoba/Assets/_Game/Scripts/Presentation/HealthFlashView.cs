using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    public sealed class HealthFlashView : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private Color flashColor = new Color(1f, 0.55f, 0.55f, 1f);
        [SerializeField] private float flashDuration = 0.09f;
        [SerializeField] private float deadAlpha = 0.45f;

        private Color[] originalColors;
        private float flashRemaining;
        private bool dead;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<SpriteRenderer>(true);
            }
            if (renderers != null)
            {
                originalColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    originalColors[i] = renderers[i].color;
                }
            }
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
            if (health != null)
            {
                health.DamageTaken += HandleDamageTaken;
                health.Died += HandleDied;
                health.Revived += HandleRevived;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.DamageTaken -= HandleDamageTaken;
                health.Died -= HandleDied;
                health.Revived -= HandleRevived;
            }
        }

        private void HandleDamageTaken(DamageResult result)
        {
            if (!result.Applied)
            {
                return;
            }
            flashRemaining = flashDuration;
        }

        private void HandleDied(Health _)
        {
            dead = true;
        }

        private void HandleRevived(Health _)
        {
            dead = false;
        }

        private void Update()
        {
            if (renderers == null || renderers.Length == 0 || originalColors == null)
            {
                return;
            }
            if (health != null)
            {
                dead = health.IsDead;
            }
            if (flashRemaining > 0f)
            {
                flashRemaining -= Time.deltaTime;
                if (flashRemaining <= 0f)
                {
                    flashRemaining = 0f;
                }
            }
            bool flashing = flashRemaining > 0f;
            float alphaScale = dead ? deadAlpha : 1f;
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = flashing ? flashColor : originalColors[i];
                color.a = (flashing ? flashColor.a : originalColors[i].a) * alphaScale;
                renderers[i].color = color;
            }
        }
    }
}
