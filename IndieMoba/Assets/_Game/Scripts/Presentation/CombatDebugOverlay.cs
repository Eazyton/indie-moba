using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Core;

namespace IndieMoba.Presentation
{
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField] private HeroCombat hero;
        [SerializeField] private Health[] trackedHealth;
        [SerializeField] private bool visible = true;
        [SerializeField] private float referenceHeight = 720f;

        private string deniedText;
        private float deniedTimer;

        private void OnEnable()
        {
            if (hero != null)
            {
                hero.ActivationDenied += HandleActivationDenied;
            }
        }

        private void OnDisable()
        {
            if (hero != null)
            {
                hero.ActivationDenied -= HandleActivationDenied;
            }
        }

        private void HandleActivationDenied(AbilitySlot slot, AbilityFailReason reason)
        {
            if (slot == AbilitySlot.BasicAttack && (reason == AbilityFailReason.OnCooldown || reason == AbilityFailReason.NoTarget))
            {
                return;
            }
            deniedText = $"{SlotKey(slot)}: {reason}";
            deniedTimer = 1.5f;
        }

        private void Update()
        {
            if (deniedTimer > 0f)
            {
                deniedTimer -= Time.deltaTime;
                if (deniedTimer <= 0f)
                {
                    deniedTimer = 0f;
                    deniedText = null;
                }
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }
            float scale = Mathf.Max(1f, Screen.height / referenceHeight);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(8f, 8f, 320f, 500f));
            GUILayout.BeginVertical("box");
            if (hero != null && hero.Health != null)
            {
                string dead = hero.Health.IsDead ? " DEAD" : "";
                GUILayout.Label($"Hero HP {hero.Health.Current:0}/{hero.Health.MaxHealth:0}  Shield {hero.Health.Shield:0} ({hero.Health.ShieldRemaining:0.0}s){dead}");
            }
            if (hero != null)
            {
                for (int i = 0; i < hero.Slots.Count; i++)
                {
                    AbilitySlotRuntime slot = hero.Slots[i];
                    string key = SlotKey(slot.Slot);
                    if (!slot.IsConfigured)
                    {
                        GUILayout.Label($"{key} empty");
                        continue;
                    }
                    string state = slot.Cooldown.IsReady ? "Ready" : $"{slot.Cooldown.Remaining:0.0}s";
                    string active = slot.IsActive ? " ACTIVE" : "";
                    GUILayout.Label($"{key} {slot.Config.DisplayName} {state}{active}");
                }
            }
            if (deniedTimer > 0f && deniedText != null)
            {
                GUILayout.Label(deniedText);
            }
            if (trackedHealth != null)
            {
                for (int i = 0; i < trackedHealth.Length; i++)
                {
                    Health health = trackedHealth[i];
                    if (health == null)
                    {
                        continue;
                    }
                    string dead = health.IsDead ? " DEAD" : "";
                    GUILayout.Label($"{health.gameObject.name} HP {health.Current:0}/{health.MaxHealth:0}{dead}");
                }
            }
            GUILayout.Label("RMB move | Arrows move | LMB hold attack | Q W E R");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private static string SlotKey(AbilitySlot slot)
        {
            switch (slot)
            {
                case AbilitySlot.BasicAttack: return "LMB";
                case AbilitySlot.Ability1: return "Q";
                case AbilitySlot.Ability2: return "W";
                case AbilitySlot.Ability3: return "E";
                case AbilitySlot.Ultimate: return "R";
                default: return "?";
            }
        }
    }
}
