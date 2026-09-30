using System.Collections.Generic;
using System.Linq;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Presentation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace IndieMoba.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private enum EnvCollider
        {
            None,
            Tree,
            Rock,
            Pillar,
            RuinWall,
            Crystal
        }

        private static void ConfigureTextureImports()
        {
            Log("Step 2/5: Texture import settings");
            string artRoot = "Assets/_Game/Art";
            ConfigureSingleSprite(artRoot + "/Environment/_Placeholder/Terrain/Terrain_PrototypeArea.png", SpriteAlignment.BottomLeft, Vector2.zero);
            for (int i = 0; i < 3; i++)
            {
                ConfigureSingleSprite(artRoot + "/Environment/_Placeholder/Trees/Tree_" + i + ".png", SpriteAlignment.Custom, new Vector2(0.5f, 4f / 76f));
            }
            string[] vegetation = { "Bush_0", "Bush_1", "Mushroom_0", "Mushroom_1", "Flower_0", "Flower_1", "Flower_2", "Flower_3" };
            foreach (string name in vegetation)
            {
                ConfigureSingleSprite(artRoot + "/Environment/_Placeholder/Vegetation/" + name + ".png", SpriteAlignment.Custom, new Vector2(0.5f, 0.06f));
            }
            for (int i = 0; i < 3; i++)
            {
                ConfigureSingleSprite(artRoot + "/Environment/_Placeholder/Rocks/Rock_" + i + ".png", SpriteAlignment.Custom, new Vector2(0.5f, 0.06f));
            }
            string[] ruins = { "Pillar_0", "Pillar_1", "RuinWall", "Crystal" };
            foreach (string name in ruins)
            {
                ConfigureSingleSprite(artRoot + "/Environment/_Placeholder/Ruins/" + name + ".png", SpriteAlignment.Custom, new Vector2(0.5f, 0.06f));
            }
            ConfigureSingleSprite(ShadowSpritePath, SpriteAlignment.Center, Vector2.zero);
            ConfigureSingleSprite(CollisionCellSpritePath, SpriteAlignment.Center, Vector2.zero);
            ConfigureNiloSheet(NiloSheetPath);
            ConfigureCombatTextureImports();
            ConfigureLaneTextureImports();
        }

        private static void ConfigureSingleSprite(string path, SpriteAlignment alignment, Vector2 pivot)
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
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)alignment;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void ConfigureNiloSheet(string path)
        {
            ConfigureCharacterSheet(path, "Nilo_Sheet");
        }

        private static void ApplyCommonSettings(TextureImporter importer, TextureImporterSettings settings)
        {
            settings.textureType = TextureImporterType.Sprite;
            settings.spritePixelsPerUnit = Ppu;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.npotScale = TextureImporterNPOTScale.None;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }

        private static void BuildAssets()
        {
            Log("Step 3/5: Assets");
            EnsureFolder("Assets/_Game/Data");
            EnsureFolder("Assets/_Game/Data/Characters");
            EnsureFolder("Assets/_Game/Art/Characters/_Placeholder/Nilo/Animations");
            BuildCollisionTile();
            BuildMovementConfig();
            BuildAnimations();
            BuildNiloAttackAnimation();
            BuildAnimatorController();
            BuildLaneAssets();
            AssetDatabase.SaveAssets();
        }

        private static void BuildCollisionTile()
        {
            var sprite = LoadSprite(CollisionCellSpritePath);
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, CollisionTilePath);
            }
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
        }

        private static void BuildMovementConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>(MovementConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterMovementConfig>();
                AssetDatabase.CreateAsset(config, MovementConfigPath);
            }
            var so = new SerializedObject(config);
            so.FindProperty("obstacleMask").intValue = 1 << 6;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildAnimations()
        {
            var sprites = LoadNiloSprites();
            var layout = LoadLayout();
            var idle = layout.anims.First(a => a.key == "nilo-idle");
            var walk = layout.anims.First(a => a.key == "nilo-walk");
            BuildAnimationClip(IdleAnimPath, sprites, idle);
            BuildAnimationClip(WalkAnimPath, sprites, walk);
        }

        private static void BuildAnimationClip(string path, Sprite[] sprites, LayoutAnim anim, bool loop = true)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew)
            {
                clip = new AnimationClip();
            }
            clip.frameRate = anim.frameRate;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[anim.frames.Length + 1];
            for (int i = 0; i < anim.frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / (float)anim.frameRate,
                    value = sprites[anim.frames[i]]
                };
            }
            keys[anim.frames.Length] = new ObjectReferenceKeyframe
            {
                time = anim.frames.Length / (float)anim.frameRate,
                value = sprites[anim.frames[anim.frames.Length - 1]]
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNew)
            {
                AssetDatabase.CreateAsset(clip, path);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }
        }

        private static void BuildAnimatorController()
        {
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleAnimPath);
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkAnimPath);
            var attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(NiloAttackAnimPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            }
            else
            {
                for (int i = controller.parameters.Length - 1; i >= 0; i--)
                {
                    controller.RemoveParameter(i);
                }
                for (int i = controller.layers.Length - 1; i > 0; i--)
                {
                    controller.RemoveLayer(i);
                }
                var sm = controller.layers[0].stateMachine;
                foreach (var st in sm.states)
                {
                    sm.RemoveState(st.state);
                }
                foreach (var t in sm.anyStateTransitions)
                {
                    sm.RemoveAnyStateTransition(t);
                }
            }
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            var stateMachine = controller.layers[0].stateMachine;
            var idleState = stateMachine.AddState("Idle");
            idleState.motion = idle;
            var walkState = stateMachine.AddState("Walk");
            walkState.motion = walk;
            var attackState = stateMachine.AddState("Attack");
            attackState.motion = attack;
            stateMachine.defaultState = idleState;
            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            var anyToAttack = stateMachine.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0f;
            anyToAttack.canTransitionToSelf = false;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0f;
            EditorUtility.SetDirty(controller);
        }

        private static void BuildPrefabs()
        {
            Log("Step 4/5: Prefabs");
            EnsureFolder("Assets/_Game/Prefabs");
            EnsureFolder("Assets/_Game/Prefabs/Characters");
            EnsureFolder("Assets/_Game/Prefabs/Characters/Visuals");
            EnsureFolder("Assets/_Game/Prefabs/Environment");
            EnsureFolder("Assets/_Game/Prefabs/Environment/_Placeholder");
            BuildProjectilePrefabs();
            BuildAbilityConfigs();
            BuildNiloVisualPrefab();
            BuildHeroPrefab();
            BuildCombatDummyPrefab();
            BuildEnvironmentPrefabs();
            BuildLanePrefabs();
        }

        private static void BuildNiloVisualPrefab()
        {
            var sprites = LoadNiloSprites();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            var shadowSprite = LoadSprite(ShadowSpritePath);

            var root = new GameObject("Nilo_Visual_Placeholder");

            var shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(root.transform, false);
            shadowGO.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var shadowSr = shadowGO.AddComponent<SpriteRenderer>();
            shadowSr.sprite = shadowSprite;
            shadowSr.sortingLayerName = "Actors";
            shadowSr.sortingOrder = -1;
            shadowSr.color = new Color(1f, 1f, 1f, 1f);

            var spriteGO = new GameObject("Sprite");
            spriteGO.transform.SetParent(root.transform, false);
            var sr = spriteGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.sortingLayerName = "Actors";
            sr.sortingOrder = 0;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            var animator = spriteGO.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(root, VisualPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildHeroPrefab()
        {
            var movementConfig = AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>(MovementConfigPath);
            var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);

            var root = new GameObject("Hero_Placeholder");
            root.layer = LayerMask.NameToLayer("Hero");
            var actor = root.AddComponent<HeroActor>();
            var actorSo = new SerializedObject(actor);
            actorSo.FindProperty("movementConfig").objectReferenceValue = movementConfig;
            actorSo.FindProperty("tickRate").floatValue = 60f;
            actorSo.ApplyModifiedPropertiesWithoutUndo();

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.layer = LayerMask.NameToLayer("Hero");
            var sortingGroup = visualRoot.AddComponent<SortingGroup>();
            sortingGroup.sortingLayerName = "Actors";
            sortingGroup.sortingOrder = 0;
            var presenter = visualRoot.AddComponent<CharacterPresenter>();
            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("actor").objectReferenceValue = actor;
            presenterSo.FindProperty("flipRoot").objectReferenceValue = null;
            presenterSo.FindProperty("interpolate").boolValue = true;
            presenterSo.FindProperty("artFacesRight").boolValue = true;
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);
            visualInstance.transform.SetParent(visualRoot.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            var spriteAnimator = visualInstance.transform.Find("Sprite").GetComponent<Animator>();

            var bridge = visualRoot.AddComponent<CharacterAnimatorBridge>();
            var bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("actor").objectReferenceValue = actor;
            bridgeSo.FindProperty("animator").objectReferenceValue = spriteAnimator;
            bridgeSo.ApplyModifiedPropertiesWithoutUndo();

            var bodySprite = visualInstance.transform.Find("Sprite").GetComponent<SpriteRenderer>();

            var health = root.AddComponent<Health>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("maxHealth").floatValue = 640f;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            var target = root.AddComponent<CombatTarget>();
            var targetSo = new SerializedObject(target);
            targetSo.FindProperty("team").enumValueIndex = 1;
            targetSo.FindProperty("kind").intValue = (int)CombatTargetKind.Hero;
            targetSo.FindProperty("radius").floatValue = 0.375f;
            targetSo.FindProperty("health").objectReferenceValue = health;
            targetSo.ApplyModifiedPropertiesWithoutUndo();

            var combat = root.AddComponent<HeroCombat>();
            var combatSo = new SerializedObject(combat);
            combatSo.FindProperty("actor").objectReferenceValue = actor;
            combatSo.FindProperty("health").objectReferenceValue = health;
            combatSo.FindProperty("self").objectReferenceValue = target;
            combatSo.FindProperty("aimOriginOffset").vector2Value = new Vector2(0f, 0.5f);
            combatSo.FindProperty("basicAttack").objectReferenceValue = LoadAbilityConfig("Hero_BasicAttack");
            combatSo.FindProperty("ability1").objectReferenceValue = LoadAbilityConfig("Hero_Q_Projectile");
            combatSo.FindProperty("ability2").objectReferenceValue = LoadAbilityConfig("Hero_W_Shield");
            combatSo.FindProperty("ability3").objectReferenceValue = LoadAbilityConfig("Hero_E_Dash");
            combatSo.FindProperty("ultimate").objectReferenceValue = LoadAbilityConfig("Hero_R_Area");
            combatSo.ApplyModifiedPropertiesWithoutUndo();

            var flash = visualRoot.AddComponent<HealthFlashView>();
            var flashSo = new SerializedObject(flash);
            flashSo.FindProperty("renderers").arraySize = 1;
            flashSo.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = bodySprite;
            flashSo.ApplyModifiedPropertiesWithoutUndo();

            var combatBridge = visualRoot.AddComponent<CombatAnimatorBridge>();
            var combatBridgeSo = new SerializedObject(combatBridge);
            combatBridgeSo.FindProperty("combat").objectReferenceValue = combat;
            combatBridgeSo.FindProperty("animator").objectReferenceValue = spriteAnimator;
            combatBridgeSo.ApplyModifiedPropertiesWithoutUndo();

            var dashTrail = visualRoot.AddComponent<DashTrailView>();
            var dashTrailSo = new SerializedObject(dashTrail);
            dashTrailSo.FindProperty("actor").objectReferenceValue = actor;
            dashTrailSo.FindProperty("source").objectReferenceValue = bodySprite;
            dashTrailSo.ApplyModifiedPropertiesWithoutUndo();

            var audioSource = visualRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            var audioHooks = visualRoot.AddComponent<CombatAudioHooks>();
            var audioHooksSo = new SerializedObject(audioHooks);
            audioHooksSo.FindProperty("combat").objectReferenceValue = combat;
            audioHooksSo.FindProperty("source").objectReferenceValue = audioSource;
            audioHooksSo.ApplyModifiedPropertiesWithoutUndo();

            var shieldRingGO = new GameObject("ShieldRing");
            shieldRingGO.transform.SetParent(visualRoot.transform, false);
            shieldRingGO.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var shieldSr = shieldRingGO.AddComponent<SpriteRenderer>();
            shieldSr.sprite = LoadSprite(ShieldRingSpritePath);
            shieldSr.color = new Color(1f, 0.95f, 0.7f, 0.8f);
            shieldSr.sortingLayerName = "Actors";
            shieldSr.sortingOrder = 1;
            shieldSr.enabled = false;

            var shieldRingView = visualRoot.AddComponent<ShieldRingView>();
            var shieldRingViewSo = new SerializedObject(shieldRingView);
            shieldRingViewSo.FindProperty("health").objectReferenceValue = health;
            shieldRingViewSo.FindProperty("ring").objectReferenceValue = shieldSr;
            shieldRingViewSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, HeroPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildEnvironmentPrefabs()
        {
            string folder = "Assets/_Game/Prefabs/Environment/_Placeholder";
            var specs = new[]
            {
                ("Tree_0", EnvCollider.Tree), ("Tree_1", EnvCollider.Tree), ("Tree_2", EnvCollider.Tree),
                ("Bush_0", EnvCollider.None), ("Bush_1", EnvCollider.None),
                ("Mushroom_0", EnvCollider.None), ("Mushroom_1", EnvCollider.None),
                ("Flower_0", EnvCollider.None), ("Flower_1", EnvCollider.None), ("Flower_2", EnvCollider.None), ("Flower_3", EnvCollider.None),
                ("Rock_0", EnvCollider.Rock), ("Rock_1", EnvCollider.Rock), ("Rock_2", EnvCollider.Rock),
                ("Pillar_0", EnvCollider.Pillar), ("Pillar_1", EnvCollider.Pillar),
                ("RuinWall", EnvCollider.RuinWall),
                ("Crystal", EnvCollider.Crystal)
            };
            foreach (var spec in specs)
            {
                BuildEnvironmentPrefab(folder, spec.Item1, spec.Item2);
            }
        }

        private static void BuildEnvironmentPrefab(string folder, string name, EnvCollider colliderType)
        {
            string spritePath = SpritePathForName(name);
            var sprite = LoadSprite(spritePath);
            if (sprite == null)
            {
                LogWarning("Missing sprite for prefab: " + name);
                return;
            }
            string path = folder + "/" + name + ".prefab";
            var root = new GameObject(name);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Actors";
            sr.sortingOrder = 0;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            if (colliderType != EnvCollider.None)
            {
                var collision = new GameObject("Collision");
                collision.transform.SetParent(root.transform, false);
                collision.layer = LayerMask.NameToLayer("Obstacle");
                switch (colliderType)
                {
                    case EnvCollider.Tree:
                        var treeCollider = collision.AddComponent<CircleCollider2D>();
                        treeCollider.radius = 0.28f;
                        treeCollider.offset = new Vector2(0f, 0.1f);
                        break;
                    case EnvCollider.Rock:
                        var rockCollider = collision.AddComponent<CircleCollider2D>();
                        rockCollider.radius = 0.3f;
                        rockCollider.offset = new Vector2(0f, 0.15f);
                        break;
                    case EnvCollider.Pillar:
                        var pillarCollider = collision.AddComponent<BoxCollider2D>();
                        pillarCollider.size = new Vector2(0.55f, 0.3f);
                        pillarCollider.offset = new Vector2(0f, 0.12f);
                        break;
                    case EnvCollider.RuinWall:
                        var wallCollider = collision.AddComponent<BoxCollider2D>();
                        wallCollider.size = new Vector2(1.3f, 0.35f);
                        wallCollider.offset = new Vector2(0f, 0.15f);
                        break;
                    case EnvCollider.Crystal:
                        var crystalCollider = collision.AddComponent<CircleCollider2D>();
                        crystalCollider.radius = 0.3f;
                        crystalCollider.offset = new Vector2(0f, 0.15f);
                        break;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static string SpritePathForName(string name)
        {
            string artRoot = "Assets/_Game/Art/Environment/_Placeholder";
            if (name.StartsWith("Tree")) return artRoot + "/Trees/" + name + ".png";
            if (name.StartsWith("Bush") || name.StartsWith("Mushroom") || name.StartsWith("Flower")) return artRoot + "/Vegetation/" + name + ".png";
            if (name.StartsWith("Rock")) return artRoot + "/Rocks/" + name + ".png";
            if (name.StartsWith("Pillar") || name == "RuinWall" || name == "Crystal") return artRoot + "/Ruins/" + name + ".png";
            return null;
        }

        private static Dictionary<string, GameObject> LoadEnvironmentPrefabs()
        {
            string folder = "Assets/_Game/Prefabs/Environment/_Placeholder";
            var result = new Dictionary<string, GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    result[prefab.name] = prefab;
                }
            }
            return result;
        }
    }
}
