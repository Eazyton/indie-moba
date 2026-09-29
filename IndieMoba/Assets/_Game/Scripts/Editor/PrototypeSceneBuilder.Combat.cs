using System.IO;
using System.Linq;
using IndieMoba.Combat;
using IndieMoba.Presentation;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;

namespace IndieMoba.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private const string VexSheetPath = "Assets/_Game/Art/Characters/_Placeholder/Vex/Vex_Sheet.png";
        private const string ProjectileQSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Projectile_Q.png";
        private const string ShieldRingSpritePath = "Assets/_Game/Art/VFX/_Placeholder/ShieldRing.png";
        private const string SparkSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Spark.png";
        private const string EmberSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Ember.png";
        private const string CircleSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Circle.png";
        private const string RingSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Ring.png";
        private const string AbilityConfigFolder = "Assets/_Game/Data/Abilities/";
        private const string BasicAttackConfigPath = AbilityConfigFolder + "Hero_BasicAttack.asset";
        private const string QConfigPath = AbilityConfigFolder + "Hero_Q_Projectile.asset";
        private const string WConfigPath = AbilityConfigFolder + "Hero_W_Shield.asset";
        private const string EConfigPath = AbilityConfigFolder + "Hero_E_Dash.asset";
        private const string RConfigPath = AbilityConfigFolder + "Hero_R_Area.asset";
        private const string CombatDummyPrefabPath = "Assets/_Game/Prefabs/Characters/CombatDummy.prefab";
        private const string BasicAttackProjectilePrefabPath = "Assets/_Game/Prefabs/Gameplay/Projectiles/BasicAttackProjectile.prefab";
        private const string AbilityProjectileQPrefabPath = "Assets/_Game/Prefabs/Gameplay/Projectiles/AbilityProjectile_Q.prefab";
        private const string NiloAttackAnimPath = "Assets/_Game/Art/Characters/_Placeholder/Nilo/Animations/Nilo_Attack.anim";

        private static void ConfigureCombatTextureImports()
        {
            string[] sprites = { ProjectileQSpritePath, ShieldRingSpritePath, SparkSpritePath, EmberSpritePath };
            foreach (string path in sprites)
            {
                AssetDatabase.ImportAsset(path);
            }
            GeneratePlaceholderTexture(CircleSpritePath, false);
            GeneratePlaceholderTexture(RingSpritePath, true);
            foreach (string path in sprites)
            {
                ConfigureSingleSprite(path, SpriteAlignment.Center, Vector2.zero);
            }
            ConfigureSingleSprite(CircleSpritePath, SpriteAlignment.Center, Vector2.zero);
            ConfigureSingleSprite(RingSpritePath, SpriteAlignment.Center, Vector2.zero);
            AssetDatabase.ImportAsset(VexSheetPath);
            ConfigureCharacterSheet(VexSheetPath, "Vex");
        }

        private static void GeneratePlaceholderTexture(string path, bool ring)
        {
            if (!File.Exists(path))
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float center = (size - 1) / 2f;
                float radius = size / 2f;
                float inner = radius - 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        bool inside = ring ? dist >= inner && dist <= radius : dist <= radius;
                        texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                    }
                }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                Log("Generated " + path);
            }
            AssetDatabase.ImportAsset(path);
        }

        private static void ConfigureCharacterSheet(string path, string namePrefix)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                LogWarning("Missing texture: " + path);
                return;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            ApplyCommonSettings(importer, settings);
            settings.spriteMode = (int)SpriteImportMode.Multiple;
            importer.SetTextureSettings(settings);

            var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .ToDictionary(s => s.name, s => s.GetSpriteID());

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
            dp.InitSpriteEditorDataProvider();

            const int count = 19;
            var rects = new SpriteRect[count];
            var pairs = new SpriteNameFileIdPair[count];
            for (int i = 0; i < count; i++)
            {
                string name = namePrefix + "_" + i;
                GUID id = existing.TryGetValue(name, out GUID g) ? g : GUID.Generate();
                rects[i] = new SpriteRect
                {
                    name = name,
                    spriteID = id,
                    rect = new Rect(i * 56f, 0f, 56f, 56f),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 3f / 56f)
                };
                pairs[i] = new SpriteNameFileIdPair(name, id);
            }
            dp.SetSpriteRects(rects);
            dp.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            dp.Apply();
            importer.SaveAndReimport();
        }

        private static void BuildProjectilePrefabs()
        {
            EnsureFolder("Assets/_Game/Prefabs/Gameplay");
            EnsureFolder("Assets/_Game/Prefabs/Gameplay/Projectiles");
            BuildProjectilePrefab(BasicAttackProjectilePrefabPath, "BasicAttackProjectile", EmberSpritePath,
                new Vector3(2f, 2f, 1f), new Color(1f, 210f / 255f, 122f / 255f, 1f), false);
            BuildProjectilePrefab(AbilityProjectileQPrefabPath, "AbilityProjectile_Q", ProjectileQSpritePath,
                Vector3.one, Color.white, true);
        }

        private static void BuildProjectilePrefab(string path, string name, string spritePath, Vector3 scale, Color color, bool rotateToDirection)
        {
            var sprite = LoadSprite(spritePath);
            var root = new GameObject(name);
            var view = root.AddComponent<ProjectileView>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "VFX";
            sr.sortingOrder = 0;
            sr.color = color;
            visual.transform.localScale = scale;
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("visualRoot").objectReferenceValue = visual.transform;
            viewSo.FindProperty("rotateToDirection").boolValue = rotateToDirection;
            viewSo.FindProperty("heightOffset").floatValue = 0.5f;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void BuildAbilityConfigs()
        {
            EnsureFolder(AbilityConfigFolder);
            var basicAttackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasicAttackProjectilePrefabPath);
            var qPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AbilityProjectileQPrefabPath);
            BuildAbilityConfig(BasicAttackConfigPath, so =>
            {
                so.FindProperty("displayName").stringValue = "Basic Attack";
                so.FindProperty("kind").enumValueIndex = (int)AbilityKind.BasicAttack;
                so.FindProperty("cooldown").floatValue = 0.8f;
                so.FindProperty("damage").floatValue = 58f;
                so.FindProperty("range").floatValue = 4.6875f;
                so.FindProperty("speed").floatValue = 18f;
                so.FindProperty("radius").floatValue = 0.125f;
                so.FindProperty("halfArcDegrees").floatValue = 43f;
                so.FindProperty("closeRange").floatValue = 0.875f;
                so.FindProperty("projectileViewPrefab").objectReferenceValue = basicAttackPrefab;
            });
            BuildAbilityConfig(QConfigPath, so =>
            {
                so.FindProperty("displayName").stringValue = "Bark Shot";
                so.FindProperty("kind").enumValueIndex = (int)AbilityKind.LinearProjectile;
                so.FindProperty("cooldown").floatValue = 8f;
                so.FindProperty("damage").floatValue = 80f;
                so.FindProperty("range").floatValue = 12.5f;
                so.FindProperty("speed").floatValue = 20f;
                so.FindProperty("radius").floatValue = 0.34375f;
                so.FindProperty("projectileViewPrefab").objectReferenceValue = qPrefab;
            });
            BuildAbilityConfig(WConfigPath, so =>
            {
                so.FindProperty("displayName").stringValue = "Bark Shield";
                so.FindProperty("kind").enumValueIndex = (int)AbilityKind.Shield;
                so.FindProperty("cooldown").floatValue = 15f;
                so.FindProperty("amount").floatValue = 200f;
                so.FindProperty("duration").floatValue = 4f;
            });
            BuildAbilityConfig(EConfigPath, so =>
            {
                so.FindProperty("displayName").stringValue = "Dash";
                so.FindProperty("kind").enumValueIndex = (int)AbilityKind.Dash;
                so.FindProperty("cooldown").floatValue = 10f;
                so.FindProperty("range").floatValue = 6.25f;
                so.FindProperty("duration").floatValue = 0.17f;
                so.FindProperty("minAimDistance").floatValue = 0.125f;
            });
            BuildAbilityConfig(RConfigPath, so =>
            {
                so.FindProperty("displayName").stringValue = "Rootquake";
                so.FindProperty("kind").enumValueIndex = (int)AbilityKind.DelayedArea;
                so.FindProperty("cooldown").floatValue = 60f;
                so.FindProperty("damage").floatValue = 150f;
                so.FindProperty("radius").floatValue = 6.25f;
                so.FindProperty("castRange").floatValue = 20.3125f;
                so.FindProperty("delay").floatValue = 1f;
            });
        }

        private static void BuildAbilityConfig(string path, System.Action<SerializedObject> configure)
        {
            var config = AssetDatabase.LoadAssetAtPath<AbilityConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AbilityConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            var so = new SerializedObject(config);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static AbilityConfig LoadAbilityConfig(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<AbilityConfig>(AbilityConfigFolder + assetName + ".asset");
        }

        private static void BuildNiloAttackAnimation()
        {
            var sprites = LoadNiloSprites();
            var layout = LoadLayout();
            var attack = layout.anims.First(a => a.key == "nilo-attack");
            BuildAnimationClip(NiloAttackAnimPath, sprites, attack, false);
        }

        private static void BuildCombatDummyPrefab()
        {
            var vexSprites = LoadVexSprites();
            var shadowSprite = LoadSprite(ShadowSpritePath);

            var root = new GameObject("CombatDummy");
            var health = root.AddComponent<Health>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("maxHealth").floatValue = 600f;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            var target = root.AddComponent<CombatTarget>();
            var targetSo = new SerializedObject(target);
            targetSo.FindProperty("team").enumValueIndex = 2;
            targetSo.FindProperty("radius").floatValue = 0.375f;
            targetSo.FindProperty("health").objectReferenceValue = health;
            targetSo.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<CombatDummy>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sortingGroup = visual.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "Actors";
            sortingGroup.sortingOrder = 0;

            var shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(visual.transform, false);
            shadowGO.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var shadowSr = shadowGO.AddComponent<SpriteRenderer>();
            shadowSr.sprite = shadowSprite;
            shadowSr.sortingLayerName = "Actors";
            shadowSr.sortingOrder = -1;
            shadowSr.color = new Color(1f, 1f, 1f, 1f);

            var spriteGO = new GameObject("Sprite");
            spriteGO.transform.SetParent(visual.transform, false);
            var sr = spriteGO.AddComponent<SpriteRenderer>();
            sr.sprite = vexSprites[0];
            sr.flipX = true;
            sr.sortingLayerName = "Actors";
            sr.sortingOrder = 0;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;

            var flash = visual.AddComponent<HealthFlashView>();
            var flashSo = new SerializedObject(flash);
            flashSo.FindProperty("renderers").arraySize = 1;
            flashSo.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = sr;
            flashSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CombatDummyPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static Sprite[] LoadVexSprites()
        {
            return AssetDatabase.LoadAllAssetsAtPath(VexSheetPath).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                .ToArray();
        }
    }
}
