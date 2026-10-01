using System.Linq;
using IndieMoba.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IndieMoba.EditorTools
{
    public static partial class Match5v5MapBuilder
    {
        internal const string LayoutAssetPath = "Assets/_Game/Data/Map/Map5v5Layout.asset";
        internal const string MapPrefabFolder = "Assets/_Game/Prefabs/Map";
        internal const string GameplayPrefabPath = MapPrefabFolder + "/Map5v5_Gameplay.prefab";
        internal const string VisualPrefabPath = MapPrefabFolder + "/Map5v5_Visual.prefab";
        internal const string MatchScenePath = "Assets/_Game/Scenes/Match/Match5v5.unity";
        internal const string GrayboxArtFolder = "Assets/_Game/Art/Environment/_Graybox";
        internal const string InputAssetPath = "Assets/_Game/Input/MobaControls.inputactions";
        internal const string HeroPrefabPath = "Assets/_Game/Prefabs/Characters/Hero_Placeholder.prefab";
        internal const string WaveConfigPath = "Assets/_Game/Data/Match/Wave_Default.asset";
        internal const string StructurePrefabFolder = "Assets/_Game/Prefabs/Structures/";
        internal const string MinionPrefabFolder = "Assets/_Game/Prefabs/Minions/";
        internal const string VfxFolder = "Assets/_Game/Art/VFX/_Placeholder/";
        internal const string EnvironmentArtFolder = "Assets/_Game/Art/Environment/_Placeholder";
        internal const int GeneratorVersion = 1;
        internal const int ObstacleLayer = 6;

        [MenuItem("IndieMoba/Map 5v5/1. Create Layout Asset (Non-Destructive)", false, 100)]
        public static void CreateLayoutAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MapLayoutData>(LayoutAssetPath);
            if (existing != null)
            {
                Log("Layout asset already exists; values preserved: " + LayoutAssetPath);
                Selection.activeObject = existing;
                return;
            }
            EnsureFolder("Assets/_Game/Data/Map");
            var layout = ScriptableObject.CreateInstance<MapLayoutData>();
            Match5v5DefaultLayout.Populate(layout);
            AssetDatabase.CreateAsset(layout, LayoutAssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = layout;
            Log("Created layout asset with default graybox values: " + LayoutAssetPath);
        }

        [MenuItem("IndieMoba/Map 5v5/2. Generate Map Geometry (Destructive)", false, 101)]
        public static void GenerateMapGeometry()
        {
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutData>(LayoutAssetPath);
            if (layout == null || !layout.HasContent)
            {
                EditorUtility.DisplayDialog("Generate Map Geometry", "Layout asset is missing or empty. Run '1. Create Layout Asset' first.", "OK");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog("Overwrite Map5v5_Gameplay?",
                    "Map5v5_Gameplay.prefab already exists and is the hand-authored source of truth for gameplay geometry.\n\n" +
                    "Regenerating from Map5v5Layout.asset will OVERWRITE all collision shapes, lanes, structure slots, spawns and zones, including manual edits.",
                    "Continue", "Cancel"))
                {
                    return;
                }
                if (!EditorUtility.DisplayDialog("Confirm destructive regeneration",
                    "Are you absolutely sure? Manual edits in Map5v5_Gameplay.prefab will be lost (recoverable only through version control).",
                    "Overwrite Geometry", "Cancel"))
                {
                    return;
                }
            }
            EnsureFolder(MapPrefabFolder);
            string guid = AssetDatabase.AssetPathToGUID(LayoutAssetPath);
            BuildGameplayPrefab(layout, guid);
            AssetDatabase.SaveAssets();
            Log("Generated " + GameplayPrefabPath);
        }

        [MenuItem("IndieMoba/Map 5v5/3. Generate Graybox Visuals", false, 102)]
        public static void GenerateGrayboxVisuals()
        {
            var gameplayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath);
            if (gameplayPrefab == null)
            {
                EditorUtility.DisplayDialog("Generate Graybox Visuals", "Map5v5_Gameplay.prefab is missing. Run '2. Generate Map Geometry' first.", "OK");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath) != null &&
                !EditorUtility.DisplayDialog("Overwrite Map5v5_Visual?",
                    "Map5v5_Visual.prefab already exists. Regenerating replaces the graybox visuals (gameplay geometry is not touched).",
                    "Overwrite Visuals", "Cancel"))
            {
                return;
            }
            EnsureFolder(MapPrefabFolder);
            EnsureFolder(GrayboxArtFolder);
            BuildVisualPrefab(gameplayPrefab);
            AssetDatabase.SaveAssets();
            Log("Generated " + VisualPrefabPath);
        }

        [MenuItem("IndieMoba/Map 5v5/4. Wire Match5v5 Systems (Non-Destructive)", false, 103)]
        public static void WireSystems()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath) == null)
            {
                EditorUtility.DisplayDialog("Wire Match5v5 Systems", "Map5v5_Gameplay.prefab is missing. Run '2. Generate Map Geometry' first.", "OK");
                return;
            }
            WireSystemsInternal();
        }

        [MenuItem("IndieMoba/Map 5v5/Collision Mode/Filled Polygons", false, 120)]
        public static void SetCollisionModeFilled()
        {
            SetCollisionMode(MapCollisionMode.FilledPolygons);
        }

        [MenuItem("IndieMoba/Map 5v5/Collision Mode/Outlines", false, 121)]
        public static void SetCollisionModeOutlines()
        {
            SetCollisionMode(MapCollisionMode.Outlines);
        }

        [MenuItem("IndieMoba/Map 5v5/Danger/Reset Layout To Defaults (Destructive)", false, 140)]
        public static void ResetLayoutToDefaults()
        {
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutData>(LayoutAssetPath);
            if (layout == null)
            {
                CreateLayoutAsset();
                return;
            }
            if (!EditorUtility.DisplayDialog("Reset Map5v5Layout?",
                "This overwrites ALL values in Map5v5Layout.asset with the default graybox layout. Manually tuned values will be lost.",
                "Continue", "Cancel"))
            {
                return;
            }
            if (!EditorUtility.DisplayDialog("Confirm layout reset",
                "Are you absolutely sure? This does not change Map5v5_Gameplay.prefab until you explicitly regenerate geometry.",
                "Reset Layout", "Cancel"))
            {
                return;
            }
            Undo.RecordObject(layout, "Reset Map5v5 Layout");
            Match5v5DefaultLayout.Populate(layout);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();
            Log("Layout reset to defaults: " + LayoutAssetPath);
        }

        private static void SetCollisionMode(MapCollisionMode mode)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath) == null)
            {
                EditorUtility.DisplayDialog("Collision Mode", "Map5v5_Gameplay.prefab is missing.", "OK");
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);
            try
            {
                var composites = root.GetComponentsInChildren<CompositeCollider2D>(true);
                foreach (var composite in composites)
                {
                    composite.geometryType = mode == MapCollisionMode.Outlines
                        ? CompositeCollider2D.GeometryType.Outlines
                        : CompositeCollider2D.GeometryType.Polygons;
                    composite.GenerateGeometry();
                }
                PrefabUtility.SaveAsPrefabAsset(root, GameplayPrefabPath);
                Log("Collision geometry type set to " + mode + " on " + composites.Length + " composite(s). Shapes unchanged.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static void Log(string message)
        {
            Debug.Log("[Match5v5MapBuilder] " + message);
        }

        internal static void LogWarning(string message)
        {
            Debug.LogWarning("[Match5v5MapBuilder] " + message);
        }

        internal static void EnsureFolder(string path)
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

        internal static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        internal static InputActionReference FindInputActionReference(string actionName)
        {
            var references = AssetDatabase.LoadAllAssetsAtPath(InputAssetPath).OfType<InputActionReference>();
            foreach (var reference in references)
            {
                if (reference.action != null && reference.action.name == actionName)
                {
                    return reference;
                }
            }
            LogWarning("Missing input action reference: " + actionName);
            return null;
        }

        internal static string StructurePrefabPath(bool nexus, string teamName)
        {
            return StructurePrefabFolder + (nexus ? "Nexus_" : "Tower_") + teamName + ".prefab";
        }

        internal static string MinionPrefabPath(string type, string teamName)
        {
            return MinionPrefabFolder + "Minion_" + type + "_" + teamName + ".prefab";
        }
    }
}
