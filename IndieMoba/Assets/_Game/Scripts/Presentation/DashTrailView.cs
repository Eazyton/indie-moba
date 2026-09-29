using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Characters;

namespace IndieMoba.Presentation
{
    public sealed class DashTrailView : MonoBehaviour
    {
        [SerializeField] private HeroActor actor;
        [SerializeField] private SpriteRenderer source;
        [SerializeField] private float interval = 0.03f;
        [SerializeField] private float ghostLifetime = 0.2f;
        [SerializeField] private Color ghostColor = new Color(0.6f, 1f, 0.6f, 0.6f);

        private readonly List<Ghost> activeGhosts = new List<Ghost>();
        private readonly Stack<Ghost> pool = new Stack<Ghost>();
        private float timer;

        private void Awake()
        {
            if (actor == null)
            {
                actor = GetComponentInParent<HeroActor>();
            }
        }

        private void Update()
        {
            if (actor == null || source == null)
            {
                return;
            }
            if (actor.IsForcedMoving)
            {
                timer += Time.deltaTime;
                while (timer >= interval)
                {
                    timer -= interval;
                    SpawnGhost();
                }
            }
            else
            {
                timer = 0f;
            }
            float dt = Time.deltaTime;
            for (int i = activeGhosts.Count - 1; i >= 0; i--)
            {
                Ghost ghost = activeGhosts[i];
                ghost.Age += dt;
                Color color = ghostColor;
                color.a = ghostColor.a * (1f - Mathf.Clamp01(ghost.Age / ghostLifetime));
                ghost.Renderer.color = color;
                if (ghost.Age >= ghostLifetime)
                {
                    activeGhosts.RemoveAt(i);
                    ghost.GameObject.SetActive(false);
                    pool.Push(ghost);
                }
            }
        }

        private void SpawnGhost()
        {
            Ghost ghost = Acquire();
            ghost.GameObject.transform.position = source.transform.position;
            ghost.GameObject.transform.localScale = source.transform.lossyScale;
            ghost.Renderer.sprite = source.sprite;
            ghost.Renderer.flipX = source.flipX;
            ghost.Renderer.sortingLayerID = source.sortingLayerID;
            ghost.Renderer.sortingOrder = source.sortingOrder - 1;
            ghost.Renderer.color = ghostColor;
            ghost.Age = 0f;
            activeGhosts.Add(ghost);
        }

        private Ghost Acquire()
        {
            Ghost ghost = pool.Count > 0 ? pool.Pop() : CreateGhost();
            ghost.GameObject.SetActive(true);
            return ghost;
        }

        private Ghost CreateGhost()
        {
            GameObject go = new GameObject("DashGhost");
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            return new Ghost { GameObject = go, Renderer = spriteRenderer };
        }

        private sealed class Ghost
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public float Age;
        }
    }
}
