using System.Collections.Generic;
using IndieMoba.CameraSystem;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;
using IndieMoba.Input;
using IndieMoba.Map;
using IndieMoba.Match;
using IndieMoba.Minions;
using IndieMoba.Presentation;
using IndieMoba.Structures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace IndieMoba.EditorTools
{
    public static partial class Match5v5MapBuilder
    {
        private static void ValidateSpawner(WaveSpawner spawner)
        {
            var so = new SerializedObject(spawner);
            string[] required = { "runner", "world", "registry", "config", "lane", "minionParent" };
            for (int i = 0; i < required.Length; i++)
            {
                if (so.FindProperty(required[i]).objectReferenceValue == null)
                {
                    Debug.LogError("[Match5v5MapBuilder] " + spawner.name + ": '" + required[i] + "' is not assigned.", spawner);
                }
            }
            var teams = so.FindProperty("teams");
            for (int i = 0; i < teams.arraySize; i++)
            {
                var element = teams.GetArrayElementAtIndex(i);
                string[] teamRequired = { "spawnPoint", "meleePrefab", "rangedPrefab" };
                for (int j = 0; j < teamRequired.Length; j++)
                {
                    if (element.FindPropertyRelative(teamRequired[j]).objectReferenceValue == null)
                    {
                        Debug.LogError("[Match5v5MapBuilder] " + spawner.name + ": teams[" + i + "]." + teamRequired[j] + " is not assigned.", spawner);
                    }
                }
            }
        }

        private static void WireSystemsInternal()
        {
            var gameplayAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath);
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            var waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);
            string missing = "";
            if (gameplayAsset == null)
            {
                missing += "\n- " + GameplayPrefabPath;
            }
            if (heroPrefab == null)
            {
                missing += "\n- " + HeroPrefabPath;
            }
            if (waveConfig == null)
            {
                missing += "\n- " + WaveConfigPath;
            }
            string[] structurePrefabs = { StructurePrefabPath(false, "Blue"), StructurePrefabPath(false, "Red"), StructurePrefabPath(true, "Blue"), StructurePrefabPath(true, "Red") };
            for (int i = 0; i < structurePrefabs.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(structurePrefabs[i]) == null)
                {
                    missing += "\n- " + structurePrefabs[i];
                }
            }
            string[] minionPrefabs = { MinionPrefabPath("Melee", "Blue"), MinionPrefabPath("Melee", "Red"), MinionPrefabPath("Ranged", "Blue"), MinionPrefabPath("Ranged", "Red") };
            for (int i = 0; i < minionPrefabs.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(minionPrefabs[i]) == null)
                {
                    missing += "\n- " + minionPrefabs[i];
                }
            }
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("Wire Match5v5 Systems",
                    "Missing required assets:" + missing + "\n\nRun 'IndieMoba/Build Prototype' first.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            EnsureFolder("Assets/_Game/Scenes/Match");
            Scene scene;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MatchScenePath) != null)
            {
                scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            gameplayAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath);
            heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPrefabPath);
            waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>(WaveConfigPath);

            var visualAsset = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
            bool visualFound = visualAsset != null;
            GameObject gameplayInstance = null;
            GameObject visualInstance = null;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (gameplayInstance == null && IsPrefabInstanceOf(roots[i], gameplayAsset))
                {
                    gameplayInstance = roots[i];
                }
                if (visualAsset != null && visualInstance == null && IsPrefabInstanceOf(roots[i], visualAsset))
                {
                    visualInstance = roots[i];
                }
            }
            if (gameplayInstance == null)
            {
                gameplayInstance = (GameObject)PrefabUtility.InstantiatePrefab(gameplayAsset, scene);
                gameplayInstance.transform.position = Vector3.zero;
            }
            if (visualAsset != null)
            {
                if (visualInstance == null)
                {
                    visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visualAsset, scene);
                    visualInstance.transform.position = Vector3.zero;
                }
            }
            else
            {
                LogWarning("Visual prefab missing; run 3. Generate Graybox Visuals");
            }

            var slotMarkers = gameplayInstance.GetComponentsInChildren<StructureSlotMarker>(true);
            var spawnMarkers = gameplayInstance.GetComponentsInChildren<MapSpawnMarker>(true);
            var lanePaths = gameplayInstance.GetComponentsInChildren<LanePath>(true);
            var cameraSettings = gameplayInstance.GetComponentInChildren<MapCameraSettings>(true);
            if (lanePaths.Length < 3)
            {
                LogWarning("Gameplay instance has " + lanePaths.Length + " LanePaths; expected 3.");
            }
            if (slotMarkers.Length < 20)
            {
                LogWarning("Gameplay instance has " + slotMarkers.Length + " structure slots; expected 20.");
            }

            var systemsRoot = FindRoot(scene, "Match5v5_Systems");
            if (systemsRoot == null)
            {
                systemsRoot = new GameObject("Match5v5_Systems");
            }

            var simulationGO = FindOrCreateChild(systemsRoot.transform, "Simulation");
            var runner = simulationGO.GetComponent<SimulationTickRunner>();
            if (runner == null)
            {
                runner = simulationGO.AddComponent<SimulationTickRunner>();
                var runnerSo = new SerializedObject(runner);
                runnerSo.FindProperty("tickRate").floatValue = 60f;
                runnerSo.FindProperty("maxTicksPerFrame").intValue = 5;
                runnerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var combatWorldGO = FindOrCreateChild(systemsRoot.transform, "CombatWorld");
            var combatWorld = combatWorldGO.GetComponent<CombatWorld>();
            if (combatWorld == null)
            {
                combatWorld = combatWorldGO.AddComponent<CombatWorld>();
            }
            var combatWorldSo = new SerializedObject(combatWorld);
            combatWorldSo.FindProperty("runner").objectReferenceValue = runner;
            combatWorldSo.ApplyModifiedPropertiesWithoutUndo();

            var cameraGO = FindRoot(scene, "Main Camera");
            bool cameraCreated = cameraGO == null;
            if (cameraCreated)
            {
                cameraGO = new GameObject("Main Camera");
            }
            cameraGO.tag = "MainCamera";
            var cam = cameraGO.GetComponent<Camera>();
            if (cam == null)
            {
                cam = cameraGO.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(29f / 255f, 61f / 255f, 42f / 255f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            if (cameraGO.GetComponent<AudioListener>() == null)
            {
                cameraGO.AddComponent<AudioListener>();
            }
            cam.GetUniversalAdditionalCameraData();
            if (cameraGO.GetComponent<PixelPerfectCamera>() != null)
            {
                LogWarning("Main Camera already has a PixelPerfectCamera; leaving it (it may override the orthographic size).");
            }
            cam.orthographicSize = cameraSettings != null ? cameraSettings.OrthographicSize : 9.375f;
            var rig = cameraGO.GetComponent<MobaCameraRig>();
            bool rigCreated = rig == null;
            if (rigCreated)
            {
                rig = cameraGO.AddComponent<MobaCameraRig>();
            }
            var rigSo = new SerializedObject(rig);
            rigSo.FindProperty("clampToBounds").boolValue = true;
            rigSo.FindProperty("worldBounds").rectValue = cameraSettings != null ? cameraSettings.Bounds : new Rect(-6f, -6f, 140f, 140f);
            if (rigCreated)
            {
                rigSo.FindProperty("followSharpness").floatValue = 10f;
            }
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            var playerInputGO = FindOrCreateChild(systemsRoot.transform, "PlayerInput");
            var inputSource = playerInputGO.GetComponent<KeyboardMouseInputSource>();
            if (inputSource == null)
            {
                inputSource = playerInputGO.AddComponent<KeyboardMouseInputSource>();
            }
            var inputSo = new SerializedObject(inputSource);
            inputSo.FindProperty("moveAction").objectReferenceValue = FindInputActionReference("Move");
            inputSo.FindProperty("moveToPointAction").objectReferenceValue = FindInputActionReference("MoveToPoint");
            inputSo.FindProperty("pointerPositionAction").objectReferenceValue = FindInputActionReference("PointerPosition");
            inputSo.FindProperty("basicAttackAction").objectReferenceValue = FindInputActionReference("BasicAttack");
            inputSo.FindProperty("ability1Action").objectReferenceValue = FindInputActionReference("Ability1");
            inputSo.FindProperty("ability2Action").objectReferenceValue = FindInputActionReference("Ability2");
            inputSo.FindProperty("ability3Action").objectReferenceValue = FindInputActionReference("Ability3");
            inputSo.FindProperty("ultimateAction").objectReferenceValue = FindInputActionReference("Ultimate");
            inputSo.FindProperty("worldCamera").objectReferenceValue = cam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            var fountainBlue = FindSpawnMarker(spawnMarkers, MapSpawnKind.Fountain, Team.Blue, LaneId.Mid);
            var heroTransform = FindChildRecursive(systemsRoot.transform, "Hero");
            GameObject heroInstance;
            bool heroCreated = heroTransform == null;
            if (heroCreated)
            {
                heroInstance = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, scene);
                heroInstance.name = "Hero";
                heroInstance.transform.SetParent(systemsRoot.transform, false);
                Vector2 fountain = fountainBlue != null ? (Vector2)fountainBlue.position : new Vector2(7f, 7f);
                heroInstance.transform.position = new Vector3(fountain.x, fountain.y, 0f);
            }
            else
            {
                heroInstance = heroTransform.gameObject;
            }
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

            var registryGO = FindOrCreateChild(systemsRoot.transform, "MinionRegistry");
            var registry = registryGO.GetComponent<MinionRegistry>();
            if (registry == null)
            {
                registry = registryGO.AddComponent<MinionRegistry>();
            }
            var minionsParent = FindOrCreateChild(systemsRoot.transform, "Minions");

            var structuresParent = FindOrCreateChild(systemsRoot.transform, "Structures");
            var structureByName = new Dictionary<string, GameObject>();
            for (int i = 0; i < slotMarkers.Length; i++)
            {
                StructureSlotMarker marker = slotMarkers[i];
                if (marker == null)
                {
                    continue;
                }
                bool nexus = marker.Tier == StructureTier.Nexus;
                string teamName = marker.Team == Team.Blue ? "Blue" : "Red";
                string instanceName = nexus ? "Nexus_" + teamName : "Tower_" + teamName + "_" + marker.Lane + "_" + marker.Tier;
                var existing = FindChildRecursive(structuresParent.transform, instanceName);
                GameObject instance;
                if (existing != null)
                {
                    instance = existing.gameObject;
                }
                else
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StructurePrefabPath(nexus, teamName));
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = instanceName;
                    instance.transform.SetParent(structuresParent.transform, false);
                }
                Vector2 markerPosition = marker.Position;
                instance.transform.position = new Vector3(markerPosition.x, markerPosition.y, 0f);
                WireStructure(instance, runner, combatWorld, heroTarget);
                structureByName[instanceName] = instance;
            }
            string[] laneNames = { "Top", "Mid", "Bottom" };
            string[] tierNames = { "Outer", "Inner", "Base" };
            for (int t = 0; t < 2; t++)
            {
                string teamName = t == 0 ? "Blue" : "Red";
                for (int l = 0; l < laneNames.Length; l++)
                {
                    string laneName = laneNames[l];
                    Structure outer = GetStructure(structureByName, "Tower_" + teamName + "_" + laneName + "_Outer");
                    Structure inner = GetStructure(structureByName, "Tower_" + teamName + "_" + laneName + "_Inner");
                    Structure baseTower = GetStructure(structureByName, "Tower_" + teamName + "_" + laneName + "_Base");
                    if (inner != null && outer != null)
                    {
                        SetRequiredStructures(inner, new[] { outer }, 0);
                    }
                    else
                    {
                        LogWarning("Missing required structure for " + teamName + " " + laneName + " Inner.");
                    }
                    if (baseTower != null && inner != null)
                    {
                        SetRequiredStructures(baseTower, new[] { inner }, 0);
                    }
                    else
                    {
                        LogWarning("Missing required structure for " + teamName + " " + laneName + " Base.");
                    }
                    if (outer != null)
                    {
                        SetRequiredStructures(outer, new Structure[0], 0);
                    }
                }
                Structure nexus = GetStructure(structureByName, "Nexus_" + teamName);
                if (nexus != null)
                {
                    Structure baseTop = GetStructure(structureByName, "Tower_" + teamName + "_Top_Base");
                    Structure baseMid = GetStructure(structureByName, "Tower_" + teamName + "_Mid_Base");
                    Structure baseBottom = GetStructure(structureByName, "Tower_" + teamName + "_Bottom_Base");
                    if (baseTop != null && baseMid != null && baseBottom != null)
                    {
                        SetRequiredStructures(nexus, new[] { baseTop, baseMid, baseBottom }, 1);
                    }
                    else
                    {
                        LogWarning("Missing base towers for " + teamName + " nexus.");
                    }
                }
            }

            var spawnersParent = FindOrCreateChild(systemsRoot.transform, "Spawners");
            var spawnerList = new List<WaveSpawner>();
            for (int i = 0; i < lanePaths.Length; i++)
            {
                LanePath lanePath = lanePaths[i];
                if (lanePath == null)
                {
                    continue;
                }
                string laneName = lanePath.LaneId.ToString();
                var spawnerGO = FindOrCreateChild(spawnersParent.transform, "WaveSpawner_" + laneName);
                var spawner = spawnerGO.GetComponent<WaveSpawner>();
                bool spawnerCreated = spawner == null;
                if (spawnerCreated)
                {
                    spawner = spawnerGO.AddComponent<WaveSpawner>();
                }
                var spawnerSo = new SerializedObject(spawner);
                spawnerSo.FindProperty("runner").objectReferenceValue = runner;
                spawnerSo.FindProperty("world").objectReferenceValue = combatWorld;
                spawnerSo.FindProperty("registry").objectReferenceValue = registry;
                spawnerSo.FindProperty("config").objectReferenceValue = waveConfig;
                spawnerSo.FindProperty("lane").objectReferenceValue = lanePath;
                spawnerSo.FindProperty("minionParent").objectReferenceValue = minionsParent.transform;
                var teams = spawnerSo.FindProperty("teams");
                teams.arraySize = 2;
                ConfigureTeamSpawn(teams.GetArrayElementAtIndex(0), Team.Blue, FindSpawnMarker(spawnMarkers, MapSpawnKind.MinionSpawn, Team.Blue, lanePath.LaneId), "Blue");
                ConfigureTeamSpawn(teams.GetArrayElementAtIndex(1), Team.Red, FindSpawnMarker(spawnMarkers, MapSpawnKind.MinionSpawn, Team.Red, lanePath.LaneId), "Red");
                if (spawnerCreated)
                {
                    spawnerSo.FindProperty("spawningEnabled").boolValue = true;
                    spawnerSo.FindProperty("waveSpawnOffset").floatValue = ResolveWaveSpawnOffset(lanePath);
                }
                spawnerSo.ApplyModifiedPropertiesWithoutUndo();
                ValidateSpawner(spawner);
                spawnerList.Add(spawner);
            }

            var matchGO = FindOrCreateChild(systemsRoot.transform, "MatchController");
            var match = matchGO.GetComponent<MatchController>();
            bool matchCreated = match == null;
            if (matchCreated)
            {
                match = matchGO.AddComponent<MatchController>();
            }
            var matchSo = new SerializedObject(match);
            matchSo.FindProperty("runner").objectReferenceValue = runner;
            var nexuses = matchSo.FindProperty("nexuses");
            nexuses.arraySize = 2;
            GameObject nexusBlueGO;
            GameObject nexusRedGO;
            structureByName.TryGetValue("Nexus_Blue", out nexusBlueGO);
            structureByName.TryGetValue("Nexus_Red", out nexusRedGO);
            nexuses.GetArrayElementAtIndex(0).objectReferenceValue = nexusBlueGO != null ? nexusBlueGO.GetComponent<NexusObjective>() : null;
            nexuses.GetArrayElementAtIndex(1).objectReferenceValue = nexusRedGO != null ? nexusRedGO.GetComponent<NexusObjective>() : null;
            matchSo.FindProperty("waveSpawner").objectReferenceValue = null;
            var waveSpawners = matchSo.FindProperty("waveSpawners");
            waveSpawners.arraySize = spawnerList.Count;
            for (int i = 0; i < spawnerList.Count; i++)
            {
                waveSpawners.GetArrayElementAtIndex(i).objectReferenceValue = spawnerList[i];
            }
            if (matchCreated)
            {
                matchSo.FindProperty("localTeam").intValue = (int)Team.Blue;
                matchSo.FindProperty("pauseSimulationOnEnd").boolValue = true;
            }
            matchSo.ApplyModifiedPropertiesWithoutUndo();

            var lanePresentationGO = FindOrCreateChild(systemsRoot.transform, "LanePresentation");
            var respawnPoint = FindOrCreateChild(lanePresentationGO.transform, "HeroRespawnPoint");
            Vector2 fountainPosition = fountainBlue != null ? (Vector2)fountainBlue.position : new Vector2(7f, 7f);
            respawnPoint.transform.position = new Vector3(fountainPosition.x, fountainPosition.y, 0f);
            var revive = lanePresentationGO.GetComponent<HeroReviveTool>();
            bool reviveCreated = revive == null;
            if (reviveCreated)
            {
                revive = lanePresentationGO.AddComponent<HeroReviveTool>();
            }
            var reviveSo = new SerializedObject(revive);
            reviveSo.FindProperty("runner").objectReferenceValue = runner;
            reviveSo.FindProperty("heroHealth").objectReferenceValue = heroHealth;
            reviveSo.FindProperty("heroActor").objectReferenceValue = heroActor;
            reviveSo.FindProperty("respawnPoint").objectReferenceValue = respawnPoint.transform;
            if (reviveCreated)
            {
                reviveSo.FindProperty("enableShortcut").boolValue = true;
            }
            reviveSo.ApplyModifiedPropertiesWithoutUndo();
            var result = lanePresentationGO.GetComponent<MatchResultView>();
            bool resultCreated = result == null;
            if (resultCreated)
            {
                result = lanePresentationGO.AddComponent<MatchResultView>();
            }
            var resultSo = new SerializedObject(result);
            resultSo.FindProperty("match").objectReferenceValue = match;
            if (resultCreated)
            {
                resultSo.FindProperty("showRestartButton").boolValue = true;
            }
            resultSo.ApplyModifiedPropertiesWithoutUndo();
            var overlay = lanePresentationGO.GetComponent<LaneDebugOverlay>();
            if (overlay == null)
            {
                overlay = lanePresentationGO.AddComponent<LaneDebugOverlay>();
            }
            var overlaySo = new SerializedObject(overlay);
            overlaySo.FindProperty("waveSpawner").objectReferenceValue = null;
            var overlaySpawners = overlaySo.FindProperty("waveSpawners");
            overlaySpawners.arraySize = spawnerList.Count;
            for (int i = 0; i < spawnerList.Count; i++)
            {
                overlaySpawners.GetArrayElementAtIndex(i).objectReferenceValue = spawnerList[i];
            }
            overlaySo.FindProperty("registry").objectReferenceValue = registry;
            overlaySo.FindProperty("match").objectReferenceValue = match;
            overlaySo.FindProperty("reviveTool").objectReferenceValue = revive;
            var allStructures = new List<Structure>();
            for (int t = 0; t < 2; t++)
            {
                string teamName = t == 0 ? "Blue" : "Red";
                for (int l = 0; l < laneNames.Length; l++)
                {
                    for (int ti = 0; ti < tierNames.Length; ti++)
                    {
                        Structure structure = GetStructure(structureByName, "Tower_" + teamName + "_" + laneNames[l] + "_" + tierNames[ti]);
                        if (structure != null)
                        {
                            allStructures.Add(structure);
                        }
                    }
                }
            }
            Structure nexusBlue = GetStructure(structureByName, "Nexus_Blue");
            Structure nexusRed = GetStructure(structureByName, "Nexus_Red");
            if (nexusBlue != null)
            {
                allStructures.Add(nexusBlue);
            }
            if (nexusRed != null)
            {
                allStructures.Add(nexusRed);
            }
            var structures = overlaySo.FindProperty("structures");
            structures.arraySize = allStructures.Count;
            for (int i = 0; i < allStructures.Count; i++)
            {
                structures.GetArrayElementAtIndex(i).objectReferenceValue = allStructures[i];
            }
            overlaySo.ApplyModifiedPropertiesWithoutUndo();

            var combatPresentationGO = FindOrCreateChild(systemsRoot.transform, "CombatPresentation");
            var projectileSpawner = combatPresentationGO.GetComponent<ProjectileViewSpawner>();
            if (projectileSpawner == null)
            {
                projectileSpawner = combatPresentationGO.AddComponent<ProjectileViewSpawner>();
            }
            var projectileSpawnerSo = new SerializedObject(projectileSpawner);
            projectileSpawnerSo.FindProperty("world").objectReferenceValue = combatWorld;
            projectileSpawnerSo.FindProperty("container").objectReferenceValue = combatPresentationGO.transform;
            projectileSpawnerSo.ApplyModifiedPropertiesWithoutUndo();
            var telegraphSpawner = combatPresentationGO.GetComponent<AreaTelegraphSpawner>();
            if (telegraphSpawner == null)
            {
                telegraphSpawner = combatPresentationGO.AddComponent<AreaTelegraphSpawner>();
            }
            var telegraphSpawnerSo = new SerializedObject(telegraphSpawner);
            telegraphSpawnerSo.FindProperty("world").objectReferenceValue = combatWorld;
            telegraphSpawnerSo.FindProperty("circleSprite").objectReferenceValue = LoadSprite(VfxFolder + "Circle.png");
            telegraphSpawnerSo.FindProperty("ringSprite").objectReferenceValue = LoadSprite(VfxFolder + "Ring.png");
            telegraphSpawnerSo.ApplyModifiedPropertiesWithoutUndo();
            var feedbackSpawner = combatPresentationGO.GetComponent<CombatFeedbackSpawner>();
            if (feedbackSpawner == null)
            {
                feedbackSpawner = combatPresentationGO.AddComponent<CombatFeedbackSpawner>();
            }
            var feedbackSpawnerSo = new SerializedObject(feedbackSpawner);
            feedbackSpawnerSo.FindProperty("world").objectReferenceValue = combatWorld;
            feedbackSpawnerSo.FindProperty("playerTarget").objectReferenceValue = heroInstance.transform;
            feedbackSpawnerSo.FindProperty("sparkSprite").objectReferenceValue = LoadSprite(VfxFolder + "Spark.png");
            feedbackSpawnerSo.ApplyModifiedPropertiesWithoutUndo();
            var debugOverlay = combatPresentationGO.GetComponent<CombatDebugOverlay>();
            if (debugOverlay == null)
            {
                debugOverlay = combatPresentationGO.AddComponent<CombatDebugOverlay>();
            }
            var debugOverlaySo = new SerializedObject(debugOverlay);
            debugOverlaySo.FindProperty("hero").objectReferenceValue = heroCombat;
            debugOverlaySo.FindProperty("trackedHealth").arraySize = 0;
            debugOverlaySo.ApplyModifiedPropertiesWithoutUndo();

            rigSo = new SerializedObject(rig);
            rigSo.FindProperty("target").objectReferenceValue = heroInstance.transform.Find("VisualRoot");
            rigSo.ApplyModifiedPropertiesWithoutUndo();
            if (cameraCreated)
            {
                Vector3 heroPosition = heroInstance.transform.position;
                cameraGO.transform.position = new Vector3(heroPosition.x, heroPosition.y, -10f);
            }

            var lightGO = FindRoot(scene, "Global Light 2D");
            if (lightGO == null)
            {
                lightGO = new GameObject("Global Light 2D");
                var light = lightGO.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.intensity = 1f;
                light.color = Color.white;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, MatchScenePath))
            {
                throw new System.InvalidOperationException("Failed to save scene: " + MatchScenePath);
            }
            var buildScenes = EditorBuildSettings.scenes;
            bool present = false;
            for (int i = 0; i < buildScenes.Length; i++)
            {
                if (buildScenes[i].path == MatchScenePath)
                {
                    present = true;
                    break;
                }
            }
            if (!present)
            {
                var list = new List<EditorBuildSettingsScene>(buildScenes);
                list.Add(new EditorBuildSettingsScene(MatchScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
            }
            Log("Wired Match5v5 systems: " + structureByName.Count + " structures, " + spawnerList.Count + " spawners, visual prefab " + (visualFound ? "found" : "missing"));
        }

        private static bool IsPrefabInstanceOf(GameObject go, GameObject asset)
        {
            if (go == null || asset == null)
            {
                return false;
            }
            if (PrefabUtility.GetCorrespondingObjectFromSource(go) == asset)
            {
                return true;
            }
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == AssetDatabase.GetAssetPath(asset);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }
            return null;
        }

        private static GameObject FindOrCreateChild(Transform parent, string name)
        {
            var existing = FindChildRecursive(parent, name);
            if (existing != null)
            {
                return existing.gameObject;
            }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Transform FindSpawnMarker(MapSpawnMarker[] markers, MapSpawnKind kind, Team team, LaneId lane)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                MapSpawnMarker marker = markers[i];
                if (marker != null && marker.Kind == kind && marker.Team == team && marker.Lane == lane)
                {
                    return marker.transform;
                }
            }
            return null;
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

        private static float ResolveWaveSpawnOffset(LanePath lanePath)
        {
            float offset = lanePath.LaneId == LaneId.Mid ? 7.8f : 0f;
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutData>(LayoutAssetPath);
            if (layout != null && layout.Lanes != null)
            {
                for (int i = 0; i < layout.Lanes.Length; i++)
                {
                    if (layout.Lanes[i] != null && layout.Lanes[i].lane == lanePath.LaneId)
                    {
                        offset = layout.Lanes[i].waveSpawnOffset;
                        break;
                    }
                }
            }
            return offset;
        }

        private static void WireStructure(GameObject instance, SimulationTickRunner runner, CombatWorld world, CombatTarget heroTarget)
        {
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
        }

        private static Structure GetStructure(Dictionary<string, GameObject> byName, string name)
        {
            GameObject go;
            if (byName.TryGetValue(name, out go) && go != null)
            {
                return go.GetComponent<Structure>();
            }
            return null;
        }

        private static void SetRequiredStructures(Structure structure, Structure[] required, int mode)
        {
            var so = new SerializedObject(structure);
            var prop = so.FindProperty("requiredStructures");
            prop.arraySize = required.Length;
            for (int i = 0; i < required.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = required[i];
            }
            so.FindProperty("requirementMode").intValue = mode;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
