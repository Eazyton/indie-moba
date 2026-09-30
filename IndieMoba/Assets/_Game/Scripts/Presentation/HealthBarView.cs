using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Sprite barSprite;
        [SerializeField] private Vector2 size = new Vector2(0.9f, 0.1f);
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
        [SerializeField] private Color fillColor = new Color(0.3f, 0.9f, 0.4f, 1f);
        [SerializeField] private bool hideWhenFull;
        [SerializeField] private bool hideWhenDead = true;
        [SerializeField] private string sortingLayer = "Overhead";
        [SerializeField] private int sortingOrder = 50;

        private SpriteRenderer background;
        private SpriteRenderer fill;
        private Transform fillTransform;
        private float lastNormalized = -1f;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }
            if (barSprite == null)
            {
                return;
            }
            background = CreatePart("Background", backgroundColor, sortingOrder);
            fill = CreatePart("Fill", fillColor, sortingOrder + 1);
            fillTransform = fill.transform;
            background.transform.localScale = ScaleFor(size.x);
        }

        private SpriteRenderer CreatePart(string partName, Color color, int order)
        {
            GameObject go = new GameObject(partName);
            go.transform.SetParent(transform, false);
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = barSprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingLayerName = sortingLayer;
            spriteRenderer.sortingOrder = order;
            return spriteRenderer;
        }

        private Vector3 ScaleFor(float width)
        {
            Vector2 spriteSize = barSprite.bounds.size;
            return new Vector3(width / Mathf.Max(0.0001f, spriteSize.x), size.y / Mathf.Max(0.0001f, spriteSize.y), 1f);
        }

        private void LateUpdate()
        {
            if (health == null || fill == null)
            {
                return;
            }
            float normalized = Mathf.Clamp01(health.Normalized);
            bool visible = !(hideWhenDead && health.IsDead) && !(hideWhenFull && normalized >= 0.999f);
            background.enabled = visible;
            fill.enabled = visible && normalized > 0f;
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            float sign = parentScale.x < 0f ? -1f : 1f;
            Vector3 localScale = transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * sign;
            transform.localScale = localScale;
            if (Mathf.Approximately(normalized, lastNormalized))
            {
                return;
            }
            lastNormalized = normalized;
            fillTransform.localScale = ScaleFor(size.x * normalized);
            fillTransform.localPosition = new Vector3(-size.x * (1f - normalized) * 0.5f, 0f, 0f);
        }
    }
}
