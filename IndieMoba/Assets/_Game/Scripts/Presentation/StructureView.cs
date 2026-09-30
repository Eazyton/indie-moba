using UnityEngine;
using IndieMoba.Structures;

namespace IndieMoba.Presentation
{
    public sealed class StructureView : MonoBehaviour
    {
        [SerializeField] private Structure structure;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer crystalRenderer;
        [SerializeField] private Sprite rubbleSprite;
        [SerializeField] private GameObject invulnerableIndicator;
        [SerializeField] private bool showProtectionTint = true;
        [SerializeField] private Color protectedTint = new Color(0.75f, 0.85f, 1f, 1f);
        [SerializeField] private Color normalTint = Color.white;
        [SerializeField] private float crystalBobAmplitude = 0.0625f;
        [SerializeField] private float crystalBobSpeed = 2f;

        private Sprite bodySprite;
        private Vector3 crystalBasePosition;

        private void Awake()
        {
            if (structure == null)
            {
                structure = GetComponentInParent<Structure>();
            }
            if (bodyRenderer != null)
            {
                bodySprite = bodyRenderer.sprite;
            }
            if (crystalRenderer != null)
            {
                crystalBasePosition = crystalRenderer.transform.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (structure == null)
            {
                return;
            }
            bool destroyed = structure.IsDestroyed;
            if (bodyRenderer != null)
            {
                Sprite wanted = destroyed && rubbleSprite != null ? rubbleSprite : bodySprite;
                if (bodyRenderer.sprite != wanted)
                {
                    bodyRenderer.sprite = wanted;
                }
                bodyRenderer.color = !destroyed && showProtectionTint && structure.IsProtectionActive ? protectedTint : normalTint;
            }
            if (crystalRenderer != null)
            {
                crystalRenderer.enabled = !destroyed;
                if (!destroyed)
                {
                    float offset = Mathf.Sin(Time.time * crystalBobSpeed) * crystalBobAmplitude;
                    crystalRenderer.transform.localPosition = crystalBasePosition + new Vector3(0f, offset, 0f);
                }
            }
            if (invulnerableIndicator != null)
            {
                bool show = !destroyed && structure.IsInvulnerable;
                if (invulnerableIndicator.activeSelf != show)
                {
                    invulnerableIndicator.SetActive(show);
                }
            }
        }
    }
}
