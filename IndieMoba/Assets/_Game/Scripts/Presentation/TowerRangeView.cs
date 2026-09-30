using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Structures;

namespace IndieMoba.Presentation
{
    public sealed class TowerRangeView : MonoBehaviour
    {
        [SerializeField] private TowerTargeting tower;
        [SerializeField] private CombatTarget player;
        [SerializeField] private SpriteRenderer ringRenderer;
        [SerializeField] private Color neutralColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color nonPlayerTargetColor = new Color(0.3f, 0.75f, 1f, 0.45f);
        [SerializeField] private Color playerTargetColor = new Color(1f, 0.25f, 0.2f, 0.65f);
        [SerializeField] private bool showWarning = true;

        private void Awake()
        {
            if (tower == null)
            {
                tower = GetComponentInParent<TowerTargeting>();
            }
            if (ringRenderer == null)
            {
                ringRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (ringRenderer == null)
            {
                return;
            }
            if (!showWarning || tower == null || tower.Config == null || tower.Structure == null || tower.Structure.IsDestroyed)
            {
                ringRenderer.enabled = false;
                return;
            }
            float range = tower.Config.Range;
            ICombatTarget target = tower.CurrentTarget;
            bool hasTarget = target != null && target.IsAlive;
            bool playerNearby = false;
            if (player != null && player.IsAlive && player.Team != tower.Structure.Team)
            {
                float distance = Vector2.Distance(player.Position, tower.Structure.Position) - player.Radius;
                playerNearby = distance <= tower.Config.WarningRange;
            }
            if (!hasTarget && !playerNearby)
            {
                ringRenderer.enabled = false;
                return;
            }
            ringRenderer.enabled = true;
            Vector2 spriteSize = ringRenderer.sprite != null ? (Vector2)ringRenderer.sprite.bounds.size : Vector2.one;
            float diameter = range * 2f;
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            float sx = Mathf.Abs(parentScale.x) > 0.0001f ? Mathf.Abs(parentScale.x) : 1f;
            float sy = Mathf.Abs(parentScale.y) > 0.0001f ? Mathf.Abs(parentScale.y) : 1f;
            transform.localScale = new Vector3(diameter / Mathf.Max(0.0001f, spriteSize.x) / sx, diameter / Mathf.Max(0.0001f, spriteSize.y) / sy, 1f);
            if (!hasTarget)
            {
                ringRenderer.color = neutralColor;
            }
            else if (player != null && target.Transform == player.Transform)
            {
                ringRenderer.color = playerTargetColor;
            }
            else
            {
                ringRenderer.color = nonPlayerTargetColor;
            }
        }
    }
}
