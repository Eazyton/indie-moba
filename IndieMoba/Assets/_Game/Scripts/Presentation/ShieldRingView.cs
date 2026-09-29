using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    public sealed class ShieldRingView : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer ring;
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private float minAlpha = 0.55f;
        [SerializeField] private float maxAlpha = 0.9f;
        [SerializeField] private float spinSpeed = 30f;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
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
                health.ShieldChanged += HandleShieldChanged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.ShieldChanged -= HandleShieldChanged;
            }
        }

        private void HandleShieldChanged(float shield)
        {
            if (ring != null)
            {
                ring.enabled = shield > 0f;
            }
        }

        private void LateUpdate()
        {
            if (ring == null || health == null)
            {
                return;
            }
            bool hasShield = health.Shield > 0f;
            if (ring.enabled != hasShield)
            {
                ring.enabled = hasShield;
            }
            if (!hasShield)
            {
                return;
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            Color color = ring.color;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, pulse);
            ring.color = color;
            ring.transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }
    }
}
