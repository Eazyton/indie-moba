using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using IndieMoba.Combat;
using IndieMoba.Jungle;

namespace IndieMoba.Presentation
{
    // F3 jungle debug overlay: camp states, timers, HP, targets, and the player's status effects.
    // Development only; reads state and offers debug actions (force spawn, test hero).
    public sealed class JungleDebugOverlay : MonoBehaviour
    {
        [SerializeField] private JungleDirector director;
        [SerializeField] private StatusEffectController playerEffects;
        [SerializeField] private HeroCombat playerCombat;
        [SerializeField] private JungleTestHeroTool testHeroTool;
        [SerializeField] private bool visible;
        [SerializeField] private float referenceHeight = 720f;
        [SerializeField] private float left = 336f;
        [SerializeField] private float width = 380f;

        private Vector2 scroll;
        private string lastEvent = "";
        private JungleDirector subscribed;

        private void OnEnable()
        {
            if (director != null)
            {
                subscribed = director;
                subscribed.BuffGranted += HandleBuffGranted;
                subscribed.CampCleared += HandleCampCleared;
            }
        }

        private void OnDisable()
        {
            if (subscribed != null)
            {
                subscribed.BuffGranted -= HandleBuffGranted;
                subscribed.CampCleared -= HandleCampCleared;
                subscribed = null;
            }
        }

        private void HandleBuffGranted(JungleCamp camp, StatusEffectController hero, StatusEffectDefinition buff)
        {
            lastEvent = $"{director.ElapsedTime:0.0}s {hero.name} got {buff.DisplayName} from {camp.CampId}";
        }

        private void HandleCampCleared(JungleCamp camp, GameObject killer)
        {
            lastEvent = $"{director.ElapsedTime:0.0}s {camp.CampId} cleared by {(killer != null ? killer.name : "?")}";
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
            {
                visible = !visible;
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
            float screenHeight = Screen.height / scale;
            GUILayout.BeginArea(new Rect(left, 8f, width, Mathf.Max(300f, screenHeight - 16f)));
            GUILayout.BeginVertical("box");
            GUILayout.Label("JUNGLE DEBUG (F3)");
            if (director == null)
            {
                GUILayout.Label("No JungleDirector assigned.");
                GUILayout.EndVertical();
                GUILayout.EndArea();
                return;
            }
            GUILayout.Label($"Jungle time {director.ElapsedTime:0.0}s | camps {director.Camps.Count}");
            DrawPlayer();
            if (!string.IsNullOrEmpty(lastEvent))
            {
                GUILayout.Label("Last: " + lastEvent);
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force spawn all"))
            {
                director.ForceSpawnAll();
            }
            if (testHeroTool != null)
            {
                GUI.enabled = testHeroTool.CanSpawn;
                if (GUILayout.Button(testHeroTool.IsSpawned ? "Remove test hero" : "Spawn test hero (Red)"))
                {
                    testHeroTool.Toggle();
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            if (testHeroTool != null && testHeroTool.IsSpawned)
            {
                string hp = testHeroTool.Health != null ? $"{testHeroTool.Health.Current:0}/{testHeroTool.Health.MaxHealth:0}" : "-";
                GUILayout.Label($"Test hero HP {hp} {Describe(testHeroTool.Controller)}");
            }
            scroll = GUILayout.BeginScrollView(scroll);
            IReadOnlyList<JungleCamp> camps = director.Camps;
            for (int i = 0; i < camps.Count; i++)
            {
                DrawCamp(camps[i]);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawPlayer()
        {
            if (playerEffects == null)
            {
                GUILayout.Label("Player: no StatusEffectController");
                return;
            }
            string recovery = playerCombat != null ? $" cdRate x{playerCombat.CooldownRecoveryRate:0.00}" : "";
            GUILayout.Label($"Player{recovery} speed x{playerEffects.MoveSpeedMultiplier:0.00}");
            GUILayout.Label("  " + Describe(playerEffects));
        }

        private static string Describe(StatusEffectController controller)
        {
            if (controller == null || controller.Effects.Count == 0)
            {
                return "(no effects)";
            }
            string text = "";
            for (int i = 0; i < controller.Effects.Count; i++)
            {
                StatusEffectInstance effect = controller.Effects[i];
                string label = effect.Definition != null ? effect.Definition.DisplayName : "?";
                text += (i > 0 ? ", " : "") + $"{label} {effect.Remaining:0.0}s";
            }
            return text;
        }

        private static void DrawCamp(JungleCamp camp)
        {
            if (camp == null)
            {
                return;
            }
            float hp = 0f;
            float maxHp = 0f;
            for (int m = 0; m < camp.Members.Count; m++)
            {
                JungleMonster monster = camp.Members[m];
                if (monster.Health == null)
                {
                    continue;
                }
                maxHp += monster.Health.MaxHealth;
                if (monster.IsAlive)
                {
                    hp += monster.Health.Current;
                }
            }
            string detail;
            switch (camp.State)
            {
                case JungleCampState.WaitingFirstSpawn:
                    detail = $"spawn in {camp.SpawnTimeRemaining:0.0}s";
                    break;
                case JungleCampState.Cleared:
                    detail = $"respawn in {camp.SpawnTimeRemaining:0.0}s";
                    break;
                case JungleCampState.Engaged:
                    string targetName = camp.CurrentTarget != null && camp.CurrentTarget.Transform != null ? camp.CurrentTarget.Transform.name : "-";
                    float resetIn = camp.Config != null ? Mathf.Max(0f, camp.Config.ResetAfterNoDamage - camp.TimeSinceHeroDamage) : 0f;
                    detail = $"-> {targetName}, reset in {resetIn:0.0}s";
                    break;
                default:
                    detail = "";
                    break;
            }
            string carrier = camp.BuffCarrier != null && camp.Config != null && camp.Config.Reward != null && camp.Config.Reward.CarrierBuff != null
                ? $" [{camp.Config.Reward.CarrierBuff.DisplayName}]" : "";
            GUILayout.Label($"{camp.CampId}{carrier}: {camp.State} {camp.AliveCount}/{camp.Members.Count} HP {hp:0}/{maxHp:0} {detail}");
        }
    }
}
