using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    // Development only. Spawns a temporary enemy "hero" (CombatDummy prefab reclassified as Hero, Red,
    // with a StatusEffectController) next to the player to test hero-vs-hero status effects such as
    // Ember Blight. Created at runtime, never saved in the scene; despawned on toggle or scene unload.
    // Note: as a Hero it is a valid target for blue towers/minions too.
    public sealed class JungleTestHeroTool : MonoBehaviour
    {
        [SerializeField] private GameObject dummyPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private Vector2 offset = new Vector2(3f, 0f);
        [SerializeField] private Team team = Team.Red;

        private GameObject instance;

        public bool CanSpawn => dummyPrefab != null;
        public bool IsSpawned => instance != null;
        public StatusEffectController Controller { get; private set; }
        public Health Health { get; private set; }

        public void Toggle()
        {
            if (instance != null)
            {
                Despawn();
            }
            else
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            if (dummyPrefab == null)
            {
                return;
            }
            Vector2 origin = player != null ? (Vector2)player.position : Vector2.zero;
            Vector2 position = origin + offset;
            instance = Instantiate(dummyPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
            instance.name = "TestHero_" + team + " (runtime)";
            CombatTarget target = instance.GetComponent<CombatTarget>();
            if (target != null)
            {
                target.SetTeam(team);
                target.SetKind(CombatTargetKind.Hero);
            }
            Health = instance.GetComponent<Health>();
            Controller = instance.GetComponent<StatusEffectController>();
            if (Controller == null)
            {
                Controller = instance.AddComponent<StatusEffectController>();
            }
        }

        private void Despawn()
        {
            if (instance != null)
            {
                Destroy(instance);
            }
            instance = null;
            Controller = null;
            Health = null;
        }

        private void OnDisable()
        {
            Despawn();
        }
    }
}
