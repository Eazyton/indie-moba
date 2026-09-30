using UnityEngine;
using IndieMoba.Combat;
using IndieMoba.Match;
using IndieMoba.Minions;
using IndieMoba.Structures;

namespace IndieMoba.Presentation
{
    public sealed class LaneDebugOverlay : MonoBehaviour
    {
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private MinionRegistry registry;
        [SerializeField] private Structure[] structures;
        [SerializeField] private MatchController match;
        [SerializeField] private HeroReviveTool reviveTool;
        [SerializeField] private bool visible = true;
        [SerializeField] private float referenceHeight = 720f;
        [SerializeField] private float width = 300f;

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }
            float scale = Mathf.Max(1f, Screen.height / referenceHeight);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float screenWidth = Screen.width / scale;
            GUILayout.BeginArea(new Rect(screenWidth - width - 8f, 8f, width, 500f));
            GUILayout.BeginVertical("box");
            if (match != null)
            {
                GUILayout.Label($"Match: {match.State}");
            }
            if (waveSpawner != null)
            {
                string state = waveSpawner.SpawningEnabled ? $"next in {waveSpawner.SecondsUntilNextWave:0.0}s" : "spawning off";
                GUILayout.Label($"Wave {waveSpawner.WaveNumber} ({state})");
            }
            if (registry != null)
            {
                GUILayout.Label($"Minions Blue {registry.CountAlive(Team.Blue)} | Red {registry.CountAlive(Team.Red)}");
            }
            if (structures != null)
            {
                for (int i = 0; i < structures.Length; i++)
                {
                    Structure structure = structures[i];
                    if (structure == null || structure.Health == null)
                    {
                        continue;
                    }
                    string flags = structure.IsDestroyed ? " DESTROYED" : structure.IsInvulnerable ? " INVULNERABLE" : structure.IsProtectionActive ? " PROTECTED" : "";
                    GUILayout.Label($"{structure.gameObject.name} {structure.Health.Current:0}/{structure.Health.MaxHealth:0}{flags}");
                }
            }
            if (reviveTool != null)
            {
                GUI.enabled = reviveTool.CanRevive;
                if (GUILayout.Button("Revive Hero (F2)"))
                {
                    reviveTool.RequestRevive();
                }
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
