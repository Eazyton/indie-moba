using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    // Placeholder status visuals: a pulsing ground ring under every unit with active status effects,
    // tinted with the effect's display color (buffs take priority over debuffs). Reads state only.
    public sealed class StatusEffectAuraSpawner : MonoBehaviour
    {
        [SerializeField] private Sprite ringSprite;
        [SerializeField] private float ringScale = 1.1f;
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private string sortingLayer = "GroundDecor";
        [SerializeField] private int sortingOrder = 20;

        private readonly Dictionary<StatusEffectController, SpriteRenderer> rings = new Dictionary<StatusEffectController, SpriteRenderer>();
        private readonly List<StatusEffectController> stale = new List<StatusEffectController>();

        private void LateUpdate()
        {
            if (ringSprite == null)
            {
                return;
            }
            IReadOnlyList<StatusEffectController> controllers = StatusEffectController.Active;
            for (int i = 0; i < controllers.Count; i++)
            {
                StatusEffectController controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }
                bool show = controller.Effects.Count > 0 && (controller.Health == null || controller.Health.IsAlive);
                if (!rings.TryGetValue(controller, out SpriteRenderer ring))
                {
                    if (!show)
                    {
                        continue;
                    }
                    ring = CreateRing(controller.name);
                    rings.Add(controller, ring);
                }
                ring.enabled = show;
                if (!show)
                {
                    continue;
                }
                StatusEffectDefinition primary = PickPrimary(controller);
                Color color = primary != null ? primary.DisplayColor : Color.white;
                color.a *= 0.55f + 0.35f * Mathf.Sin(Time.time * pulseSpeed);
                ring.color = color;
                Transform t = ring.transform;
                Vector3 position = controller.transform.position;
                t.position = new Vector3(position.x, position.y, position.z);
                float radius = controller.Target != null ? controller.Target.Radius : 0.4f;
                float size = radius * 2f * ringScale / Mathf.Max(0.0001f, ringSprite.bounds.size.x);
                t.localScale = new Vector3(size, size, 1f);
            }
            stale.Clear();
            foreach (KeyValuePair<StatusEffectController, SpriteRenderer> pair in rings)
            {
                if (pair.Key == null || !pair.Key.isActiveAndEnabled)
                {
                    stale.Add(pair.Key);
                }
            }
            for (int i = 0; i < stale.Count; i++)
            {
                SpriteRenderer ring = rings[stale[i]];
                if (ring != null)
                {
                    Destroy(ring.gameObject);
                }
                rings.Remove(stale[i]);
            }
        }

        private static StatusEffectDefinition PickPrimary(StatusEffectController controller)
        {
            StatusEffectDefinition fallback = null;
            IReadOnlyList<StatusEffectInstance> effects = controller.Effects;
            for (int i = 0; i < effects.Count; i++)
            {
                StatusEffectDefinition definition = effects[i].Definition;
                if (definition == null)
                {
                    continue;
                }
                if (definition.IsBuff)
                {
                    return definition;
                }
                if (fallback == null)
                {
                    fallback = definition;
                }
            }
            return fallback;
        }

        private SpriteRenderer CreateRing(string ownerName)
        {
            GameObject go = new GameObject("StatusAura_" + ownerName);
            go.transform.SetParent(transform, false);
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ringSprite;
            spriteRenderer.sortingLayerName = sortingLayer;
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }
    }
}
