using System.Linq;
using IndieMoba.CameraSystem;
using IndieMoba.Characters;
using IndieMoba.Input;
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
            inputSo.FindProperty("worldCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            // Hero
            var heroInstance = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, scene);
            heroInstance.transform.SetParent(gameplay.transform, false);
            heroInstance.transform.localPosition = new Vector3(spawn.x, spawn.y, 0f);
            var heroActor = heroInstance.GetComponent<HeroActor>();
            var heroSo = new SerializedObject(heroActor);
            heroSo.FindProperty("inputSourceBehaviour").objectReferenceValue = inputSource;
            heroSo.ApplyModifiedPropertiesWithoutUndo();

            rigSo = new SerializedObject(rig);
            rigSo.FindProperty("target").objectReferenceValue = heroInstance.transform.Find("VisualRoot");
            rigSo.ApplyModifiedPropertiesWithoutUndo();

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
    }
}
