using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    public sealed class ProjectileViewSpawner : MonoBehaviour
    {
        [SerializeField] private CombatWorld world;
        [SerializeField] private Transform container;

        private readonly Dictionary<int, ProjectileView> views = new Dictionary<int, ProjectileView>();
        private IProjectileViewFactory factory = new InstantiateProjectileViewFactory();

        public void SetFactory(IProjectileViewFactory value)
        {
            factory = value != null ? value : new InstantiateProjectileViewFactory();
        }

        private void Awake()
        {
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
            if (container == null)
            {
                container = transform;
            }
        }

        private void OnEnable()
        {
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
            if (world != null)
            {
                world.ProjectileSpawned += HandleProjectileSpawned;
                world.ProjectileDestroyed += HandleProjectileDestroyed;
            }
        }

        private void OnDisable()
        {
            if (world != null)
            {
                world.ProjectileSpawned -= HandleProjectileSpawned;
                world.ProjectileDestroyed -= HandleProjectileDestroyed;
            }
            ReleaseAll();
        }

        private void HandleProjectileSpawned(ProjectileState state)
        {
            if (state == null || state.ViewPrefab == null)
            {
                return;
            }
            GameObject instance = factory.Acquire(state.ViewPrefab, state.Position, container);
            ProjectileView view = instance.GetComponent<ProjectileView>();
            if (view == null)
            {
                view = instance.AddComponent<ProjectileView>();
            }
            view.Bind(state, world != null ? world.Runner : null);
            views[state.Id] = view;
        }

        private void HandleProjectileDestroyed(ProjectileState state, bool hit)
        {
            if (state == null || !views.TryGetValue(state.Id, out ProjectileView view))
            {
                return;
            }
            views.Remove(state.Id);
            view.Unbind();
            factory.Release(view.gameObject);
        }

        private void ReleaseAll()
        {
            foreach (KeyValuePair<int, ProjectileView> pair in views)
            {
                pair.Value.Unbind();
                factory.Release(pair.Value.gameObject);
            }
            views.Clear();
        }
    }
}
