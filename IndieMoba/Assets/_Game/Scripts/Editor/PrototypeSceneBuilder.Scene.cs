using System.Linq;
using IndieMoba.CameraSystem;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;
using IndieMoba.Input;
using IndieMoba.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace IndieMoba.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildScene()
        {
            Log("Step 5/5: Scene");
            EnsureFolder("Assets/_Game/Scenes");
            EnsureFolder("Assets/_Game/Scenes/Prototype");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var layout = LoadLayout();
            var collisionTile = AssetDatabase.LoadAssetAtPath<Tile>(CollisionTilePath);
            var terrainSprite = LoadSprite(TerrainSpritePath);
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            Vector2 spawn = DemoToWorld(layout.heroSpawn.x, layout.heroSpawn.y);

            // Environment
            var environment = new GameObject("Environment");

            var terrainGO = new GameObject("Terrain");
            terrainGO.transform.SetParent(environment.transform, false);
            var terrainSr = terrainGO.AddComponent<SpriteRenderer>();
            terrainSr.sprite = terrainSprite;
            terrainSr.sortingLayerName = "Ground";
            terrainSr.sortingOrder = 0;

            var gridGO = new GameObject("CollisionGrid");
            gridGO.transform.SetParent(environment.transform, false);
            var grid = gridGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(0.5f, 0.5f, 0f);

            var collisionGO = new GameObject("Collision");
            collisionGO.transform.SetParent(gridGO.transform, false);
            collisionGO.layer = LayerMask.NameToLayer("Obstacle");
            var tilemap = collisionGO.AddComponent<Tilemap>();
            var tilemapRenderer = collisionGO.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingLayerName = "Overhead";
            tilemapRenderer.sortingOrder = 0;
            tilemapRenderer.enabled = false;
            var rigidbody = collisionGO.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            var tilemapCollider = collisionGO.AddComponent<TilemapCollider2D>();
            var composite = collisionGO.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            int rows = layout.blocked.Length;
            for (int r = 0; r < rows; r++)
            {
                string row = layout.blocked[r];
                for (int c = 0; c < row.Length; c++)
                {
                    if (row[c] == '#')
                    {
                        tilemap.SetTile(new Vector3Int(c, rows - 1 - r, 0), collisionTile);
                    }
                }
            }
            tilemapCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();

            var propsGO = new GameObject("Props");
            propsGO.transform.SetParent(environment.transform, false);
            var propPrefabs = LoadEnvironmentPrefabs();
            foreach (var prop in layout.props)
            {
                string prefabName = PropPrefabName(prop.key);
                if (prefabName == null)
                {
                    LogWarning("Unknown prop key: " + prop.key);
                    continue;
                }
                if (!propPrefabs.TryGetValue(prefabName, out var prefab) || prefab == null)
                {
                    LogWarning("Missing prefab for prop key: " + prop.key);
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(propsGO.transform, false);
                Vector2 world = DemoToWorld(prop.x, prop.y);
                instance.transform.localPosition = new Vector3(world.x, world.y, 0f);
                if (prop.flip)
                {
                    var visual = instance.transform.Find("Visual");
                    if (visual != null)
                    {
                        Vector3 ls = visual.localScale;
                        ls.x = -1f;
                        visual.localScale = ls;
                    }
                }
            }

            // Gameplay
            var gameplay = new GameObject("Gameplay");

            // Simulation
            var simulationGO = new GameObject("Simulation");
            simulationGO.transform.SetParent(gameplay.transform, false);
            var runner = simulationGO.AddComponent<SimulationTickRunner>();
            var runnerSo = new SerializedObject(runner);
            runnerSo.FindProperty("tickRate").floatValue = 60f;
            runnerSo.FindProperty("maxTicksPerFrame").intValue = 5;
            runnerSo.ApplyModifiedPropertiesWithoutUndo();

            // CombatWorld
            var combatWorldGO = new GameObject("CombatWorld");
            combatWorldGO.transform.SetParent(gameplay.transform, false);
            var combatWorld = combatWorldGO.AddComponent<CombatWorld>();
            var combatWorldSo = new SerializedObject(combatWorld);
            combatWorldSo.FindProperty("runner").objectReferenceValue = runner;
            combatWorldSo.ApplyModifiedPropertiesWithoutUndo();

            // Main Camera
            var cameraGO = new GameObject("Main Camera");
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8.4375f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(29f / 255f, 61f / 255f, 42f / 255f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            cameraGO.transform.position = new Vector3(spawn.x, spawn.y, -10f);
            cameraGO.AddComponent<AudioListener>();
            cam.GetUniversalAdditionalCameraData();
            var ppc = cameraGO.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = 32;
            ppc.refResolutionX = ReferenceResolutionX;
            ppc.refResolutionY = ReferenceResolutionY;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            var rig = cameraGO.AddComponent<MobaCameraRig>();
            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("followSharpness").floatValue = 10f;
            rigSo.FindProperty("clampToBounds").boolValue = true;
            rigSo.FindProperty("worldBounds").rectValue = new Rect(0f, 0f, MapWidth, MapHeight);
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            // PlayerInput
            var playerInputGO = new GameObject("PlayerInput");
            playerInputGO.transform.SetParent(gameplay.transform, false);
            var inputSource = playerInputGO.AddComponent<KeyboardMouseInputSource>();
            var inputSo = new SerializedObject(inputSource);
            inputSo.FindProperty("moveAction").objectReferenceValue = FindInputActionReference("Move");
            inputSo.FindProperty("moveToPointAction").objectReferenceValue = FindInputActionReference("MoveToPoint");
            inputSo.FindProperty("pointerPositionAction").objectReferenceValue = FindInputActionReference("PointerPosition");
            inputSo.FindProperty("basicAttackAction").objectReferenceValue = FindInputActionReferenceOrWarn("BasicAttack");
            inputSo.FindProperty("ability1Action").objectReferenceValue = FindInputActionReferenceOrWarn("Ability1");
            inputSo.FindProperty("ability2Action").objectReferenceValue = FindInputActionReferenceOrWarn("Ability2");
            inputSo.FindProperty("ability3Action").objectReferenceValue = FindInputActionReferenceOrWarn("Ability3");
            inputSo.FindProperty("ultimateAction").objectReferenceValue = FindInputActionReferenceOrWarn("Ultimate");
            inputSo.FindProperty("worldCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            // Hero
            var heroInstance = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, scene);
            heroInstance.transform.SetParent(gameplay.transform, false);
            heroInstance.transform.localPosition = new Vector3(spawn.x, spawn.y, 0f);
            var heroActor = heroInstance.GetComponent<HeroActor>();
            var heroSo = new SerializedObject(heroActor);
            heroSo.FindProperty("inputSourceBehaviour").objectReferenceValue = inputSource;
            heroSo.FindProperty("runner").objectReferenceValue = runner;
            heroSo.ApplyModifiedPropertiesWithoutUndo();

            var heroHealth = heroInstance.GetComponent<Health>();
            var heroHealthSo = new SerializedObject(heroHealth);
            heroHealthSo.FindProperty("runner").objectReferenceValue = runner;
            heroHealthSo.ApplyModifiedPropertiesWithoutUndo();

            var heroTarget = heroInstance.GetComponent<CombatTarget>();
            var heroTargetSo = new SerializedObject(heroTarget);
            heroTargetSo.FindProperty("world").objectReferenceValue = combatWorld;
            heroTargetSo.ApplyModifiedPropertiesWithoutUndo();

            var heroCombat = heroInstance.GetComponent<HeroCombat>();
            var heroCombatSo = new SerializedObject(heroCombat);
            heroCombatSo.FindProperty("runner").objectReferenceValue = runner;
            heroCombatSo.FindProperty("world").objectReferenceValue = combatWorld;
            heroCombatSo.FindProperty("abilityInputBehaviour").objectReferenceValue = inputSource;
            heroCombatSo.ApplyModifiedPropertiesWithoutUndo();

            var heroAudioHooks = heroInstance.transform.Find("VisualRoot").GetComponent<CombatAudioHooks>();
            var heroAudioHooksSo = new SerializedObject(heroAudioHooks);
            heroAudioHooksSo.FindProperty("world").objectReferenceValue = combatWorld;
            heroAudioHooksSo.ApplyModifiedPropertiesWithoutUndo();

            rigSo = new SerializedObject(rig);
            rigSo.FindProperty("target").objectReferenceValue = heroInstance.transform.Find("VisualRoot");
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            // Combat dummies
            var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CombatDummyPrefabPath);
            var dummyA = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, scene);
            dummyA.name = "CombatDummy_A";
            dummyA.transform.SetParent(gameplay.transform, false);
            dummyA.transform.localPosition = new Vector3(10.5f, 11.25f, 0f);
            ConfigureDummyInstance(dummyA, runner, combatWorld);
            var dummyB = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, scene);
            dummyB.name = "CombatDummy_B";
            dummyB.transform.SetParent(gameplay.transform, false);
            dummyB.transform.localPosition = new Vector3(13.5f, 9.75f, 0f);
            ConfigureDummyInstance(dummyB, runner, combatWorld);

            // CombatPresentation
            var combatPresentation = new GameObject("CombatPresentation");
            combatPresentation.transform.SetParent(gameplay.transform, false);
            var projectileSpawner = combatPresentation.AddComponent<ProjectileViewSpawner>();
            var projectileSpawnerSo = new SerializedObject(projectileSpawner);
            projectileSpawnerSo.FindProperty("world").objectReferenceValue = combatWorld;
            projectileSpawnerSo.FindProperty("container").objectReferenceValue = combatPresentation.transform;
            projectileSpawnerSo.ApplyModifiedPropertiesWithoutUndo();
            var telegraphSpawner = combatPresentation.AddComponent<AreaTelegraphSpawner>();
            var telegraphSpawnerSo = new SerializedObject(telegraphSpawner);
            telegraphSpawnerSo.FindProperty("world").objectReferenceValue = combatWorld;
            telegraphSpawnerSo.FindProperty("circleSprite").objectReferenceValue = LoadSprite(CircleSpritePath);
            telegraphSpawnerSo.FindProperty("ringSprite").objectReferenceValue = LoadSprite(RingSpritePath);
            telegraphSpawnerSo.ApplyModifiedPropertiesWithoutUndo();
            var feedbackSpawner = combatPresentation.AddComponent<CombatFeedbackSpawner>();
            var feedbackSpawnerSo = new SerializedObject(feedbackSpawner);
            feedbackSpawnerSo.FindProperty("world").objectReferenceValue = combatWorld;
            feedbackSpawnerSo.FindProperty("playerTarget").objectReferenceValue = heroInstance.transform;
            feedbackSpawnerSo.FindProperty("sparkSprite").objectReferenceValue = LoadSprite(SparkSpritePath);
            feedbackSpawnerSo.ApplyModifiedPropertiesWithoutUndo();
            var debugOverlay = combatPresentation.AddComponent<CombatDebugOverlay>();
            var debugOverlaySo = new SerializedObject(debugOverlay);
            debugOverlaySo.FindProperty("hero").objectReferenceValue = heroCombat;
            debugOverlaySo.FindProperty("trackedHealth").arraySize = 2;
            debugOverlaySo.FindProperty("trackedHealth").GetArrayElementAtIndex(0).objectReferenceValue = dummyA.GetComponent<Health>();
            debugOverlaySo.FindProperty("trackedHealth").GetArrayElementAtIndex(1).objectReferenceValue = dummyB.GetComponent<Health>();
            debugOverlaySo.ApplyModifiedPropertiesWithoutUndo();

            // Global Light 2D
            var lightGO = new GameObject("Global Light 2D");
            var light = lightGO.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save scene: " + ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static InputActionReference FindInputActionReferenceOrWarn(string actionName)
        {
            var reference = FindInputActionReference(actionName);
            if (reference == null)
            {
                LogWarning("Missing input action reference: " + actionName);
            }
            return reference;
        }

        private static void ConfigureDummyInstance(GameObject instance, SimulationTickRunner runner, CombatWorld world)
        {
            var health = instance.GetComponent<Health>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("runner").objectReferenceValue = runner;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            var dummy = instance.GetComponent<CombatDummy>();
            var dummySo = new SerializedObject(dummy);
            dummySo.FindProperty("runner").objectReferenceValue = runner;
            dummySo.ApplyModifiedPropertiesWithoutUndo();

            var target = instance.GetComponent<CombatTarget>();
            var targetSo = new SerializedObject(target);
            targetSo.FindProperty("world").objectReferenceValue = world;
            targetSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
