using System.IO;
using System.Linq;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Match;
using IndieMoba.Minions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace IndieMoba.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private const string MinionArtFolder = "Assets/_Game/Art/Characters/_Placeholder/Minions/";
        private const string MinionAnimFolder = MinionArtFolder + "Animations/";
        private const string StructureArtFolder = "Assets/_Game/Art/Structures/_Placeholder/";
        private const string ArrowSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Projectile_Arrow.png";
        private const string TowerProjectileSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Projectile_Tower.png";
        private const string BarSpritePath = "Assets/_Game/Art/VFX/_Placeholder/Bar.png";
        private const string MinionDataFolder = "Assets/_Game/Data/Minions/";
        private const string StructureDataFolder = "Assets/_Game/Data/Structures/";
        private const string MatchDataFolder = "Assets/_Game/Data/Match/";
        private const string MinionMovementConfigPath = MinionDataFolder + "MinionMovementConfig.asset";
        private const string MinionMeleeConfigPath = MinionDataFolder + "Minion_Melee.asset";
        private const string MinionRangedConfigPath = MinionDataFolder + "Minion_Ranged.asset";
        private const string TowerConfigPath = StructureDataFolder + "Tower.asset";
        private const string NexusConfigPath = StructureDataFolder + "Nexus.asset";
        private const string WaveConfigPath = MatchDataFolder + "Wave_Default.asset";
        private const string MinionPrefabFolder = "Assets/_Game/Prefabs/Minions/";
        private const string StructurePrefabFolder = "Assets/_Game/Prefabs/Structures/";
        private const string MinionArrowPrefabPath = "Assets/_Game/Prefabs/Gameplay/Projectiles/MinionArrowProjectile.prefab";
        private const string TowerProjectilePrefabPath = "Assets/_Game/Prefabs/Gameplay/Projectiles/TowerProjectile.prefab";
        private const string NexusProjectilePrefabPath = "Assets/_Game/Prefabs/Gameplay/Projectiles/NexusProjectile.prefab";
        private const float LaneY = 11.25f;
        private const float TowerRadius = 0.8125f;
        private const float NexusRadius = 1.3125f;

        private static readonly string[] MinionTypes = { "Melee", "Ranged" };
        private static readonly string[] TeamNames = { "Blue", "Red" };
        private static readonly Color BlueColor = new Color(0f, 200f / 255f, 180f / 255f, 1f);
        private static readonly Color RedColor = new Color(1f, 107f / 255f, 107f / 255f, 1f);

        private static string MinionSheetPath(string type, string team) => MinionArtFolder + "Minion_" + type + "_" + team + ".png";
        private static string MinionPrefabPath(string type, string team) => MinionPrefabFolder + "Minion_" + type + "_" + team + ".prefab";
        private static string MinionControllerPath(string type, string team) => MinionAnimFolder + "Minion_" + type + "_" + team + ".controller";
        private static string StructurePrefabPath(string kind, string team) => StructurePrefabFolder + kind + "_" + team + ".prefab";

        private static void ConfigureLaneTextureImports()
        {
            foreach (string type in MinionTypes)
            {
                foreach (string team in TeamNames)
                {
                    string path = MinionSheetPath(type, team);
                    AssetDatabase.ImportAsset(path);
                    ConfigureGridSheet(path, "Minion_" + type + "_" + team, 10, 30, 30, new Vector2(0.5f, 3f / 30f));
                }
            }
            foreach (string team in TeamNames)
            {
                ConfigureSingleSprite(StructureArtFolder + "Tower_" + team + ".png", SpriteAlignment.Custom, new Vector2(0.5f, 6f / 86f));
                ConfigureSingleSprite(StructureArtFolder + "Nexus_" + team + ".png", SpriteAlignment.Custom, new Vector2(0.5f, 4f / 118f));
                ConfigureSingleSprite(StructureArtFolder + "TowerCrystal_" + team + ".png", SpriteAlignment.Center, Vector2.zero);
                ConfigureSingleSprite(StructureArtFolder + "NexusCrystal_" + team + ".png", SpriteAlignment.Center, Vector2.zero);
            }
            ConfigureSingleSprite(StructureArtFolder + "Rubble.png", SpriteAlignment.Custom, new Vector2(0.5f, 6f / 40f));
            ConfigureSingleSprite(ArrowSpritePath, SpriteAlignment.Center, Vector2.zero);
            ConfigureSingleSprite(TowerProjectileSpritePath, SpriteAlignment.Center, Vector2.zero);
            if (!File.Exists(BarSpritePath))
            {
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = Enumerable.Repeat(Color.white, 16).ToArray();
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(BarSpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(BarSpritePath);
            ConfigureSingleSprite(BarSpritePath, SpriteAlignment.Center, Vector2.zero);
        }

        private static void ConfigureGridSheet(string path, string namePrefix, int count, int frameWidth, int frameHeight, Vector2 pivot)
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
                    rect = new Rect(i * frameWidth, 0f, frameWidth, frameHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot
                };
                pairs[i] = new SpriteNameFileIdPair(name, id);
            }
            dp.SetSpriteRects(rects);
            dp.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            dp.Apply();
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadSheetSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                .ToArray();
        }

        private static void BuildLaneAssets()
        {
            EnsureFolder(MinionAnimFolder.TrimEnd('/'));
            EnsureFolder(MinionDataFolder.TrimEnd('/'));
            EnsureFolder(StructureDataFolder.TrimEnd('/'));
            EnsureFolder(MatchDataFolder.TrimEnd('/'));
            var layout = LoadLayout();
            for (int t = 0; t < TeamNames.Length; t++)
            {
                foreach (string type in MinionTypes)
                {
                    BuildMinionAnimations(layout, type, t);
                }
            }
            var movement = AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>(MinionMovementConfigPath);
            if (movement == null)
            {
                movement = ScriptableObject.CreateInstance<CharacterMovementConfig>();
                AssetDatabase.CreateAsset(movement, MinionMovementConfigPath);
            }
            var so = new SerializedObject(movement);
            so.FindProperty("maxSpeed").floatValue = 2.5625f;
            so.FindProperty("acceleration").floatValue = 60f;
            so.FindProperty("deceleration").floatValue = 80f;
            so.FindProperty("stopDistance").floatValue = 0.05f;
            so.FindProperty("collisionRadius").floatValue = 0.28f;
            so.FindProperty("skinWidth").floatValue = 0.02f;
            so.FindProperty("maxSlideIterations").intValue = 3;
            so.FindProperty("obstacleMask").intValue = 1 << 6;
            so.FindProperty("facingThreshold").floatValue = 0.05f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(movement);

            BuildConfigAsset<WaveConfig>(WaveConfigPath, w =>
            {
                w.FindProperty("firstWaveDelay").floatValue = 4f;
                w.FindProperty("waveInterval").floatValue = 30f;
                w.FindProperty("spawnGap").floatValue = 0.7f;
                var composition = w.FindProperty("composition");
                MinionAttackType[] order = { MinionAttackType.Melee, MinionAttackType.Ranged, MinionAttackType.Melee, MinionAttackType.Ranged, MinionAttackType.Melee };
                composition.arraySize = order.Length;
                for (int i = 0; i < order.Length; i++)
                {
                    composition.GetArrayElementAtIndex(i).intValue = (int)order[i];
                }
                var offsets = w.FindProperty("lateralOffsets");
                float[] values = { -0.75f, 0f, 0.75f };
                offsets.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    offsets.GetArrayElementAtIndex(i).floatValue = values[i];
                }
                w.FindProperty("maxWaves").intValue = 0;
            });
        }

        private static void BuildMinionAnimations(LayoutData layout, string type, int teamIndex)
        {
            string team = TeamNames[teamIndex];
            string keyPrefix = "minion-" + type.ToLowerInvariant() + "-" + teamIndex + "-";
            var sprites = LoadSheetSprites(MinionSheetPath(type, team));
            if (sprites.Length < 10)
            {
                LogWarning("Minion sheet not sliced: " + type + " " + team);
                return;
            }
            string clipPrefix = MinionAnimFolder + "Minion_" + type + "_" + team + "_";
            var idleAnim = new LayoutAnim { key = keyPrefix + "idle", frames = new[] { 0 }, frameRate = 1, repeat = -1 };
            BuildAnimationClip(clipPrefix + "Idle.anim", sprites, idleAnim, true);
            BuildAnimationClip(clipPrefix + "Walk.anim", sprites, layout.anims.First(a => a.key == keyPrefix + "walk"), true);
            BuildAnimationClip(clipPrefix + "Attack.anim", sprites, layout.anims.First(a => a.key == keyPrefix + "attack"), false);
            BuildAnimationClip(clipPrefix + "Death.anim", sprites, layout.anims.First(a => a.key == keyPrefix + "death"), false);

            string controllerPath = MinionControllerPath(type, team);
            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPrefix + "Idle.anim");
            var walk = sm.AddState("Walk");
            walk.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPrefix + "Walk.anim");
            var attack = sm.AddState("Attack");
            attack.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPrefix + "Attack.anim");
            var death = sm.AddState("Death");
            death.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPrefix + "Death.anim");
            sm.defaultState = idle;
            var toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
            var toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
            var anyToAttack = sm.AddAnyStateTransition(attack);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0f;
            anyToAttack.canTransitionToSelf = true;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            anyToAttack.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            var attackToIdle = attack.AddTransition(idle);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0f;
            var anyToDeath = sm.AddAnyStateTransition(death);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0f;
            anyToDeath.canTransitionToSelf = false;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            EditorUtility.SetDirty(controller);
        }

        private static T BuildConfigAsset<T>(string path, System.Action<SerializedObject> configure) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            var so = new SerializedObject(asset);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
