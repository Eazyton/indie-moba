using System.Collections.Generic;
using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Presentation
{
    public sealed class CombatFeedbackSpawner : MonoBehaviour
    {
        [SerializeField] private CombatWorld world;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Font font;
        [SerializeField] private float heightOffset = 1.1f;
        [SerializeField] private float riseDistance = 1.0625f;
        [SerializeField] private float holdTime = 0.25f;
        [SerializeField] private float fadeTime = 0.75f;
        [SerializeField] private int fontSize = 40;
        [SerializeField] private float characterSize = 0.1f;
        [SerializeField] private string sortingLayer = "Overhead";
        [SerializeField] private int sortingOrder = 100;
        [SerializeField] private Sprite sparkSprite;
        [SerializeField] private int sparkCount = 6;
        [SerializeField] private float sparkSpeed = 3f;
        [SerializeField] private float sparkLifetime = 0.3f;
        [SerializeField] private Color sparkColor = new Color(1f, 0.85f, 0.45f, 1f);

        private readonly List<FloatingText> activeTexts = new List<FloatingText>();
        private readonly Stack<FloatingText> textPool = new Stack<FloatingText>();
        private readonly List<Spark> activeSparks = new List<Spark>();
        private readonly Stack<Spark> sparkPool = new Stack<Spark>();

        private void Awake()
        {
            if (world == null)
            {
                world = FindAnyObjectByType<CombatWorld>();
            }
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                world.DamageApplied += HandleDamageApplied;
            }
        }

        private void OnDisable()
        {
            if (world != null)
            {
                world.DamageApplied -= HandleDamageApplied;
            }
        }

        private void HandleDamageApplied(ICombatTarget target, DamageResult result)
        {
            if (!result.Applied || target == null)
            {
                return;
            }
            try
            {
                ShowFeedback(target, result);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void ShowFeedback(ICombatTarget target, DamageResult result)
        {
            Vector3 position = new Vector3(target.Position.x, target.Position.y + heightOffset, 0f);
            string text = result.HealthDamage <= 0f && result.Absorbed > 0f
                ? "(absorbed)"
                : Mathf.RoundToInt(result.HealthDamage).ToString();
            SpawnText(position, text, ResolveColor(target, result));
            SpawnSparks(position);
        }

        private Color ResolveColor(ICombatTarget target, DamageResult result)
        {
            if (playerTarget != null && target.Transform == playerTarget)
            {
                return new Color(1f, 0.353f, 0.29f, 1f);
            }
            if (playerTarget != null && result.Info.Source != null && result.Info.Source.transform == playerTarget)
            {
                return new Color(1f, 0.824f, 0.478f, 1f);
            }
            return new Color(1f, 0.702f, 0.278f, 1f);
        }

        private void SpawnText(Vector3 position, string text, Color color)
        {
            FloatingText instance = AcquireText();
            instance.GameObject.transform.position = position;
            instance.Text.text = text;
            instance.Text.color = color;
            instance.Color = color;
            instance.Age = 0f;
            instance.StartPosition = position;
            activeTexts.Add(instance);
        }

        private FloatingText AcquireText()
        {
            FloatingText instance = null;
            while (textPool.Count > 0 && (instance == null || instance.GameObject == null))
            {
                instance = textPool.Pop();
            }
            if (instance == null || instance.GameObject == null)
            {
                instance = CreateText();
            }
            instance.GameObject.SetActive(true);
            return instance;
        }

        private FloatingText CreateText()
        {
            GameObject go = new GameObject("DamageText");
            go.transform.SetParent(transform, false);
            TextMesh textMesh = go.AddComponent<TextMesh>();
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            if (font != null)
            {
                textMesh.font = font;
                meshRenderer.sharedMaterial = font.material;
            }
            textMesh.fontSize = fontSize;
            textMesh.characterSize = characterSize;
            meshRenderer.sortingLayerName = sortingLayer;
            meshRenderer.sortingOrder = sortingOrder;
            return new FloatingText { GameObject = go, Text = textMesh, Renderer = meshRenderer };
        }

        private void SpawnSparks(Vector3 position)
        {
            if (sparkSprite == null)
            {
                return;
            }
            for (int i = 0; i < sparkCount; i++)
            {
                Spark spark = AcquireSpark();
                spark.GameObject.transform.position = position;
                Vector2 direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Vector2.up;
                }
                spark.Direction = direction.normalized;
                spark.Age = 0f;
                activeSparks.Add(spark);
            }
        }

        private Spark AcquireSpark()
        {
            Spark spark = null;
            while (sparkPool.Count > 0 && (spark == null || spark.GameObject == null))
            {
                spark = sparkPool.Pop();
            }
            if (spark == null || spark.GameObject == null)
            {
                spark = CreateSpark();
            }
            spark.GameObject.SetActive(true);
            return spark;
        }

        private Spark CreateSpark()
        {
            GameObject go = new GameObject("Spark");
            go.transform.SetParent(transform, false);
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sparkSprite;
            spriteRenderer.color = sparkColor;
            spriteRenderer.sortingLayerName = sortingLayer;
            spriteRenderer.sortingOrder = sortingOrder;
            return new Spark { GameObject = go, Renderer = spriteRenderer };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float total = holdTime + fadeTime;
            for (int i = activeTexts.Count - 1; i >= 0; i--)
            {
                FloatingText instance = activeTexts[i];
                if (instance.GameObject == null)
                {
                    activeTexts.RemoveAt(i);
                    continue;
                }
                instance.Age += dt;
                float t = Mathf.Clamp01(instance.Age / total);
                instance.GameObject.transform.position = instance.StartPosition + Vector3.up * (riseDistance * t);
                Color color = instance.Color;
                if (instance.Age > holdTime)
                {
                    color.a = 1f - Mathf.Clamp01((instance.Age - holdTime) / fadeTime);
                }
                instance.Text.color = color;
                if (instance.Age >= total)
                {
                    activeTexts.RemoveAt(i);
                    instance.GameObject.SetActive(false);
                    textPool.Push(instance);
                }
            }
            for (int i = activeSparks.Count - 1; i >= 0; i--)
            {
                Spark spark = activeSparks[i];
                if (spark.GameObject == null)
                {
                    activeSparks.RemoveAt(i);
                    continue;
                }
                spark.Age += dt;
                spark.GameObject.transform.position += (Vector3)(spark.Direction * sparkSpeed) * dt;
                Color color = sparkColor;
                color.a = 1f - Mathf.Clamp01(spark.Age / sparkLifetime);
                spark.Renderer.color = color;
                if (spark.Age >= sparkLifetime)
                {
                    activeSparks.RemoveAt(i);
                    spark.GameObject.SetActive(false);
                    sparkPool.Push(spark);
                }
            }
        }

        private sealed class FloatingText
        {
            public GameObject GameObject;
            public TextMesh Text;
            public MeshRenderer Renderer;
            public Vector3 StartPosition;
            public Color Color;
            public float Age;
        }

        private sealed class Spark
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public Vector2 Direction;
            public float Age;
        }
    }
}
