using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieMoba.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Prototype/GameplayPrototype.unity";
        private const string InputAssetPath = "Assets/_Game/Input/MobaControls.inputactions";
        private const string LayoutPath = "Assets/_Game/Data/Prototype/PrototypeLayout.json";
        private const string HeroPrefabPath = "Assets/_Game/Prefabs/Characters/Hero_Placeholder.prefab";
        private const string VisualPrefabPath = "Assets/_Game/Prefabs/Characters/Visuals/Nilo_Visual_Placeholder.prefab";
        private const string MovementConfigPath = "Assets/_Game/Data/Characters/HeroMovementConfig.asset";
        private const string CollisionTilePath = "Assets/_Game/Art/Environment/_Placeholder/Debug/CollisionTile.asset";
        private const string AnimatorControllerPath = "Assets/_Game/Art/Characters/_Placeholder/Nilo/Animations/Nilo_Placeholder.controller";
        private const string IdleAnimPath = "Assets/_Game/Art/Characters/_Placeholder/Nilo/Animations/Nilo_Idle.anim";
        private const string WalkAnimPath = "Assets/_Game/Art/Characters/_Placeholder/Nilo/Animations/Nilo_Walk.anim";
        private const string NiloSheetPath = "Assets/_Game/Art/Characters/_Placeholder/Nilo/Nilo_Sheet.png";
        private const string TerrainSpritePath = "Assets/_Game/Art/Environment/_Placeholder/Terrain/Terrain_PrototypeArea.png";
        private const string ShadowSpritePath = "Assets/_Game/Art/Characters/_Placeholder/Shared/CharacterShadow.png";
        private const string CollisionCellSpritePath = "Assets/_Game/Art/Environment/_Placeholder/Debug/CollisionCell.png";

        private const float Ppu = 32f;
        private const float CropX = 400f;
        private const float CropY = 696f;
        private const float CropH = 864f;
        private const float MapWidth = 40f;
        private const float MapHeight = 27f;

        [MenuItem("IndieMoba/Prototype/Rebuild Gameplay Prototype")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Rebuild Gameplay Prototype",
                "This regenerates placeholder import settings, prefabs and overwrites GameplayPrototype.unity",
                "OK", "Cancel"))
            {
                return;
            }
            Build();
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[PrototypeSceneBuilder] Build failed: " + e);
                EditorApplication.Exit(1);
            }
        }

        public static void Build()
        {
            Log("Build started");
            ConfigureProjectSettings();
            ConfigureTextureImports();
            BuildAssets();
            BuildPrefabs();
            BuildScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Log("Done");
        }

        private static void ConfigureProjectSettings()
        {
            Log("Step 1/5: Project settings");
            Physics2D.gravity = Vector2.zero;
            ConfigureTagsAndSortingLayers();
            ConfigureRenderer2D();
            RemoveTemplateInputActions();
            DeleteSampleScene();
        }

        private static void ConfigureTagsAndSortingLayers()
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var so = new SerializedObject(tagManager);
            var layers = so.FindProperty("layers");
            SetLayerName(layers, 6, "Obstacle");
            SetLayerName(layers, 7, "Hero");

            var sortingLayers = so.FindProperty("m_SortingLayers");
            string[] desiredOrder = { "Default", "Ground", "GroundDecor", "Actors", "Overhead", "VFX", "UI" };
            var usedIds = new HashSet<uint>();
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                usedIds.Add(sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").uintValue);
            }
            foreach (string name in desiredOrder)
            {
                int index = FindSortingLayerIndex(sortingLayers, name);
                if (index < 0)
                {
                    sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
                    index = sortingLayers.arraySize - 1;
                    var element = sortingLayers.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("name").stringValue = name;
                    element.FindPropertyRelative("locked").intValue = 0;
                }
                var layerElement = sortingLayers.GetArrayElementAtIndex(index);
                uint id = layerElement.FindPropertyRelative("uniqueID").uintValue;
                if (name != "Default" && (id == 0 || usedIds.Contains(id)))
                {
                    uint fresh = HashName(name);
                    while (fresh == 0 || usedIds.Contains(fresh))
                    {
                        fresh++;
                    }
                    usedIds.Add(fresh);
                    layerElement.FindPropertyRelative("uniqueID").uintValue = fresh;
                }
                else
                {
                    usedIds.Add(id);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (SortingLayer.NameToID("Actors") == 0)
            {
                throw new InvalidOperationException("Sorting layer 'Actors' did not resolve after apply.");
            }
        }

        private static int FindSortingLayerIndex(SerializedProperty sortingLayers, string name)
        {
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void SetLayerName(SerializedProperty layers, int index, string name)
        {
            if (index < 0 || index >= layers.arraySize)
            {
                return;
            }
            var element = layers.GetArrayElementAtIndex(index);
            string current = element.stringValue;
            if (string.IsNullOrEmpty(current) || current == name)
            {
                element.stringValue = name;
            }
            else
            {
                LogWarning("Layer " + index + " is already '" + current + "'; not overwriting with '" + name + "'.");
            }
        }

        private static uint HashName(string name)
        {
            uint hash = 2166136261;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }

        private static void ConfigureRenderer2D()
        {
            const string path = "Assets/Settings/Renderer2D.asset";
            var renderer = AssetDatabase.LoadAllAssetsAtPath(path)[0];
            var so = new SerializedObject(renderer);
            var mode = so.FindProperty("m_TransparencySortMode");
            mode.enumValueIndex = 3;
            mode.intValue = 3;
            var axis = so.FindProperty("m_TransparencySortAxis");
            axis.vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void RemoveTemplateInputActions()
        {
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
            const string path = "Assets/Settings/InputSystem_Actions.inputactions";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                Log("Deleted template input actions asset.");
            }
        }

        private static void DeleteSampleScene()
        {
            const string path = "Assets/Scenes/SampleScene.unity";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                Log("Deleted SampleScene.");
            }
            if (AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                string[] assets = AssetDatabase.FindAssets("t:Object", new[] { "Assets/Scenes" });
                if (assets.Length == 0)
                {
                    AssetDatabase.DeleteAsset("Assets/Scenes");
                    Log("Deleted empty Assets/Scenes folder.");
                }
            }
        }

        private static void Log(string message)
        {
            Debug.Log("[PrototypeSceneBuilder] " + message);
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning("[PrototypeSceneBuilder] " + message);
        }

        private static Vector2 DemoToWorld(float x, float y)
        {
            return new Vector2((x - CropX) / Ppu, (CropH - (y - CropY)) / Ppu);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string name = path.Substring(lastSlash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite[] LoadNiloSprites()
        {
            return AssetDatabase.LoadAllAssetsAtPath(NiloSheetPath).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                .ToArray();
        }

        private static string PropPrefabName(string key)
        {
            if (key.StartsWith("tree-")) return "Tree_" + key.Substring(5);
            if (key.StartsWith("bush-")) return "Bush_" + key.Substring(5);
            if (key.StartsWith("rock-")) return "Rock_" + key.Substring(5);
            if (key.StartsWith("mush-")) return "Mushroom_" + key.Substring(5);
            if (key.StartsWith("flower-")) return "Flower_" + key.Substring(7);
            if (key == "pillar-0") return "Pillar_0";
            if (key == "pillar-1") return "Pillar_1";
            if (key == "ruin-wall") return "RuinWall";
            if (key == "crystal-deco") return "Crystal";
            return null;
        }

        private static InputActionReference FindInputActionReference(string actionName)
        {
            var references = AssetDatabase.LoadAllAssetsAtPath(InputAssetPath).OfType<InputActionReference>();
            foreach (var reference in references)
            {
                if (reference.action != null && reference.action.name == actionName)
                {
                    return reference;
                }
            }
            return null;
        }

        private static LayoutData LoadLayout()
        {
            string json = File.ReadAllText(LayoutPath);
            return JsonUtility.FromJson<LayoutData>(json);
        }

        [Serializable]
        private class LayoutData
        {
            public LayoutCrop crop;
            public int cellSize;
            public LayoutAnim[] anims;
            public LayoutProp[] props;
            public string[] blocked;
            public LayoutPoint heroSpawn;
        }

        [Serializable]
        private class LayoutCrop
        {
            public int x;
            public int y;
            public int w;
            public int h;
        }

        [Serializable]
        private class LayoutAnim
        {
            public string key;
            public int[] frames;
            public int frameRate;
            public int repeat;
        }

        [Serializable]
        private class LayoutProp
        {
            public string key;
            public int x;
            public int y;
            public bool flip;
        }

        [Serializable]
        private class LayoutPoint
        {
            public int x;
            public int y;
        }
    }
}
