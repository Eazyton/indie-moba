using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    public sealed class AreaTelegraphSpawner : MonoBehaviour
    {
        [SerializeField] private CombatWorld world;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite ringSprite;
        [SerializeField] private Color color = new Color(1f, 0.29f, 0.165f, 1f);
        [SerializeField] private float minFillAlpha = 0.1f;
        [SerializeField] private float maxFillAlpha = 0.32f;
        [SerializeField] private float flashAlpha = 0.6f;
        [SerializeField] private float flashTime = 0.25f;
        [SerializeField] private string sortingLayer = "GroundDecor";
        [SerializeField] private int sortingOrder = 50;

        private readonly Dictionary<int, AreaView> views = new Dictionary<int, AreaView>();
        private readonly List<AreaView> flashing = new List<AreaView>();
        private readonly Stack<AreaView> pool = new Stack<AreaView>();

        private void Awake()
        {
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
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
                world.AreaStarted += HandleAreaStarted;
                world.AreaResolved += HandleAreaResolved;
            }
        }

        private void OnDisable()
        {
            if (world != null)
            {
                world.AreaStarted -= HandleAreaStarted;
                world.AreaResolved -= HandleAreaResolved;
            }
            foreach (KeyValuePair<int, AreaView> pair in views)
            {
                pair.Value.GameObject.SetActive(false);
                pool.Push(pair.Value);
            }
            views.Clear();
            for (int i = 0; i < flashing.Count; i++)
            {
                flashing[i].GameObject.SetActive(false);
                pool.Push(flashing[i]);
            }
            flashing.Clear();
        }

        private void HandleAreaStarted(DelayedAreaState area)
        {
            if (area == null || circleSprite == null || ringSprite == null)
            {
                return;
            }
            AreaView view = Acquire();
            float scale = (2f * area.Radius) / circleSprite.bounds.size.x;
            view.GameObject.transform.position = area.Center;
            view.GameObject.transform.localScale = new Vector3(scale, scale, 1f);
            view.Fill.sprite = circleSprite;
            view.OuterRing.sprite = ringSprite;
            view.InnerRing.sprite = ringSprite;
            view.InnerRing.transform.localScale = Vector3.zero;
            view.State = area;
            view.FlashRemaining = 0f;
            views[area.Id] = view;
        }

        private void HandleAreaResolved(DelayedAreaState area)
        {
            if (area == null || !views.TryGetValue(area.Id, out AreaView view))
            {
                return;
            }
            views.Remove(area.Id);
            view.State = null;
            view.FlashRemaining = flashTime;
            flashing.Add(view);
        }

        private AreaView Acquire()
        {
            AreaView view = pool.Count > 0 ? pool.Pop() : Create();
            view.GameObject.SetActive(true);
            return view;
        }

        private AreaView Create()
        {
            GameObject go = new GameObject("AreaTelegraph");
            go.transform.SetParent(transform, false);
            SpriteRenderer fill = go.AddComponent<SpriteRenderer>();
            fill.sprite = circleSprite;
            fill.color = color;
            fill.sortingLayerName = sortingLayer;
            fill.sortingOrder = sortingOrder;
            GameObject outer = new GameObject("OuterRing");
            outer.transform.SetParent(go.transform, false);
            SpriteRenderer outerRenderer = outer.AddComponent<SpriteRenderer>();
            outerRenderer.sprite = ringSprite;
            outerRenderer.color = color;
            outerRenderer.sortingLayerName = sortingLayer;
            outerRenderer.sortingOrder = sortingOrder + 1;
            GameObject inner = new GameObject("InnerRing");
            inner.transform.SetParent(go.transform, false);
            SpriteRenderer innerRenderer = inner.AddComponent<SpriteRenderer>();
            innerRenderer.sprite = ringSprite;
            innerRenderer.color = color;
            innerRenderer.sortingLayerName = sortingLayer;
            innerRenderer.sortingOrder = sortingOrder + 2;
            return new AreaView { GameObject = go, Fill = fill, OuterRing = outerRenderer, InnerRing = innerRenderer };
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            foreach (KeyValuePair<int, AreaView> pair in views)
            {
                AreaView view = pair.Value;
                if (view.State == null)
                {
                    continue;
                }
                float progress = view.State.NormalizedProgress;
                Color fillColor = color;
                fillColor.a = Mathf.Lerp(minFillAlpha, maxFillAlpha, progress);
                view.Fill.color = fillColor;
                view.OuterRing.color = fillColor;
                view.InnerRing.color = fillColor;
                view.InnerRing.transform.localScale = new Vector3(progress, progress, 1f);
            }
            for (int i = flashing.Count - 1; i >= 0; i--)
            {
                AreaView view = flashing[i];
                view.FlashRemaining -= dt;
                float t = Mathf.Clamp01(view.FlashRemaining / flashTime);
                Color flashColor = color;
                flashColor.a = flashAlpha * t;
                view.Fill.color = flashColor;
                view.OuterRing.color = flashColor;
                view.InnerRing.color = flashColor;
                if (view.FlashRemaining <= 0f)
                {
                    flashing.RemoveAt(i);
                    view.GameObject.SetActive(false);
                    pool.Push(view);
                }
            }
        }

        private sealed class AreaView
        {
            public GameObject GameObject;
            public SpriteRenderer Fill;
            public SpriteRenderer OuterRing;
            public SpriteRenderer InnerRing;
            public DelayedAreaState State;
            public float FlashRemaining;
        }
    }
}
