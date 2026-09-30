using System.Collections.Generic;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;
using IndieMoba.Match;
using IndieMoba.Minions;
using IndieMoba.Presentation;
using IndieMoba.Structures;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndieMoba.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private static readonly float[] LaneWaypointsX = { 10f, 21.25f, 35f, 48.75f, 60f, 62.1875f };

        private static void BuildLaneScene(Scene scene, GameObject gameplay, SimulationTickRunner runner, CombatWorld world, GameObject heroInstance, Vector2 heroSpawn)
        {
            var laneGO = new GameObject("Lane_Mid");
            laneGO.transform.SetParent(gameplay.transform, false);
            var lane = laneGO.AddComponent<LanePath>();
            var waypoints = new Transform[LaneWaypointsX.Length];
            for (int i = 0; i < LaneWaypointsX.Length; i++)
            {
                var wp = new GameObject("Waypoint_" + i);
                wp.transform.SetParent(laneGO.transform, false);
                wp.transform.position = new Vector3(LaneWaypointsX[i], LaneY, 0f);
                waypoints[i] = wp.transform;
            }
            var laneSo = new SerializedObject(lane);
            laneSo.FindProperty("laneId").intValue = (int)LaneId.Mid;
            var wpProp = laneSo.FindProperty("waypoints");
            wpProp.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                wpProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
            }
            laneSo.ApplyModifiedPropertiesWithoutUndo();

            var structuresGO = new GameObject("Structures");
            structuresGO.transform.SetParent(gameplay.transform, false);
            var heroTarget = heroInstance.GetComponent<CombatTarget>();
            var blueTower = PlaceStructure(scene, structuresGO.transform, "Tower", "Blue", 21.25f, runner, world, heroTarget);
            var redTower = PlaceStructure(scene, structuresGO.transform, "Tower", "Red", 48.75f, runner, world, heroTarget);
            var blueNexus = PlaceStructure(scene, structuresGO.transform, "Nexus", "Blue", 7.8125f, runner, world, heroTarget);
            var redNexus = PlaceStructure(scene, structuresGO.transform, "Nexus", "Red", 62.1875f, runner, world, heroTarget);
            SetRequiredStructure(blueNexus, blueTower);
            SetRequiredStructure(redNexus, redTower);

            var registryGO = new GameObject("MinionRegistry");
            registryGO.transform.SetParent(gameplay.transform, false);
            var registry = registryGO.AddComponent<MinionRegistry>();

            var minionsGO = new GameObject("Minions");
            minionsGO.transform.SetParent(gameplay.transform, false);

            var spawnerGO = new GameObject("WaveSpawner");
            spawnerGO.transform.SetParent(gameplay.transform, false);
            var blueSpawn = new GameObject("Spawn_Blue");
            blueSpawn.transform.SetParent(spawnerGO.transform, false);
            blueSpawn.transform.position = new Vector3(10f, LaneY, 0f);
            var redSpawn = new GameObject("Spawn_Red");
            redSpawn.transform.SetParent(spawnerGO.transform, false);
            redSpawn.transform.position = new Vector3(60f, LaneY, 0f);
            var spawner = spawnerGO.AddComponent<WaveSpawner>();
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("runner").objectReferenceValue = runner;
            spawnerSo.FindProperty("world").objectReferenceValue = world;
            spawnerSo.FindProperty("registry").objectReferenceValue = registry;
            spawnerSo.FindProperty("config").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
            spawnerSo.FindProperty("lane").objectReferenceValue = lane;
            spawnerSo.FindProperty("minionParent").objectReferenceValue = minionsGO.transform;
            spawnerSo.FindProperty("spawningEnabled").boolValue = true;
            var teams = spawnerSo.FindProperty("teams");
            teams.arraySize = 2;
            ConfigureTeamSpawn(teams.GetArrayElementAtIndex(0), Team.Blue, blueSpawn.transform, "Blue");
            ConfigureTeamSpawn(teams.GetArrayElementAtIndex(1), Team.Red, redSpawn.transform, "Red");
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            var matchGO = new GameObject("MatchController");
            matchGO.transform.SetParent(gameplay.transform, false);
            var match = matchGO.AddComponent<MatchController>();
            var matchSo = new SerializedObject(match);
            matchSo.FindProperty("runner").objectReferenceValue = runner;
            var nexuses = matchSo.FindProperty("nexuses");
            nexuses.arraySize = 2;
            nexuses.GetArrayElementAtIndex(0).objectReferenceValue = blueNexus.GetComponent<NexusObjective>();
            nexuses.GetArrayElementAtIndex(1).objectReferenceValue = redNexus.GetComponent<NexusObjective>();
            matchSo.FindProperty("waveSpawner").objectReferenceValue = spawner;
            matchSo.FindProperty("localTeam").intValue = (int)Team.Blue;
            matchSo.FindProperty("pauseSimulationOnEnd").boolValue = true;
            matchSo.ApplyModifiedPropertiesWithoutUndo();

            var presentationGO = new GameObject("LanePresentation");
            presentationGO.transform.SetParent(gameplay.transform, false);
            var respawnPoint = new GameObject("HeroRespawnPoint");
            respawnPoint.transform.SetParent(presentationGO.transform, false);
            respawnPoint.transform.position = new Vector3(heroSpawn.x, heroSpawn.y, 0f);

            var revive = presentationGO.AddComponent<HeroReviveTool>();
            var reviveSo = new SerializedObject(revive);
            reviveSo.FindProperty("runner").objectReferenceValue = runner;
            reviveSo.FindProperty("heroHealth").objectReferenceValue = heroInstance.GetComponent<Health>();
            reviveSo.FindProperty("heroActor").objectReferenceValue = heroInstance.GetComponent<HeroActor>();
            reviveSo.FindProperty("respawnPoint").objectReferenceValue = respawnPoint.transform;
            reviveSo.FindProperty("enableShortcut").boolValue = true;
            reviveSo.ApplyModifiedPropertiesWithoutUndo();

            var result = presentationGO.AddComponent<MatchResultView>();
            var resultSo = new SerializedObject(result);
            resultSo.FindProperty("match").objectReferenceValue = match;
            resultSo.FindProperty("showRestartButton").boolValue = true;
            resultSo.ApplyModifiedPropertiesWithoutUndo();

            var overlay = presentationGO.AddComponent<LaneDebugOverlay>();
            var overlaySo = new SerializedObject(overlay);
            overlaySo.FindProperty("waveSpawner").objectReferenceValue = spawner;
            overlaySo.FindProperty("registry").objectReferenceValue = registry;
            overlaySo.FindProperty("match").objectReferenceValue = match;
            overlaySo.FindProperty("reviveTool").objectReferenceValue = revive;
            var structures = overlaySo.FindProperty("structures");
            var all = new List<GameObject> { blueTower, blueNexus, redTower, redNexus };
            structures.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++)
            {
                structures.GetArrayElementAtIndex(i).objectReferenceValue = all[i].GetComponent<Structure>();
            }
            overlaySo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTeamSpawn(SerializedProperty element, Team team, Transform spawnPoint, string teamName)
        {
            element.FindPropertyRelative("team").intValue = (int)team;
            element.FindPropertyRelative("spawnPoint").objectReferenceValue = spawnPoint;
            element.FindPropertyRelative("meleePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath("Melee", teamName)).GetComponent<MinionController>();
            element.FindPropertyRelative("rangedPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(MinionPrefabPath("Ranged", teamName)).GetComponent<MinionController>();
            element.FindPropertyRelative("enabled").boolValue = true;
        }

        private static GameObject PlaceStructure(Scene scene, Transform parent, string kind, string teamName, float x,
            SimulationTickRunner runner, CombatWorld world, CombatTarget heroTarget)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StructurePrefabPath(kind, teamName));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(parent, false);
            instance.transform.position = new Vector3(x, LaneY, 0f);

            var healthSo = new SerializedObject(instance.GetComponent<Health>());
            healthSo.FindProperty("runner").objectReferenceValue = runner;
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            var targetSo = new SerializedObject(instance.GetComponent<CombatTarget>());
            targetSo.FindProperty("world").objectReferenceValue = world;
            targetSo.ApplyModifiedPropertiesWithoutUndo();
            var structureSo = new SerializedObject(instance.GetComponent<Structure>());
            structureSo.FindProperty("world").objectReferenceValue = world;
            structureSo.ApplyModifiedPropertiesWithoutUndo();

            var targeting = instance.GetComponent<TowerTargeting>();
            if (targeting != null)
            {
                var targetingSo = new SerializedObject(targeting);
                targetingSo.FindProperty("world").objectReferenceValue = world;
                targetingSo.FindProperty("runner").objectReferenceValue = runner;
                targetingSo.ApplyModifiedPropertiesWithoutUndo();
                var towerCombatSo = new SerializedObject(instance.GetComponent<TowerCombat>());
                towerCombatSo.FindProperty("world").objectReferenceValue = world;
                towerCombatSo.FindProperty("runner").objectReferenceValue = runner;
                towerCombatSo.ApplyModifiedPropertiesWithoutUndo();
                var rangeView = instance.GetComponentInChildren<TowerRangeView>(true);
                if (rangeView != null)
                {
                    var rangeSo = new SerializedObject(rangeView);
                    rangeSo.FindProperty("player").objectReferenceValue = heroTarget;
                    rangeSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            return instance;
        }

        private static void SetRequiredStructure(GameObject nexus, GameObject tower)
        {
            var so = new SerializedObject(nexus.GetComponent<Structure>());
            var required = so.FindProperty("requiredStructures");
            required.arraySize = 1;
            required.GetArrayElementAtIndex(0).objectReferenceValue = tower.GetComponent<Structure>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
