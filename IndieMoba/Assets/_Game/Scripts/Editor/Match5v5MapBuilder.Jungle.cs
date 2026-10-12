using System.Collections.Generic;
using System.Linq;
using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;
using IndieMoba.Jungle;
using IndieMoba.Map;
using IndieMoba.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndieMoba.EditorTools
{
    // Phase 5B: 5. Wire Jungle (Non-Destructive).
    // - Jungle data assets and placeholder monster prefabs are created only when missing; existing assets
    //   (including hand-tuned values) are never overwritten.
    // - Camps are created under Match5v5_Systems/Jungle from the MapZoneMarkers listed in JungleSetup.
    //   Existing camps (found by name) are kept as they are; only scene references are re-wired.
    // - Map geometry, the gameplay/visual prefabs, waves and structures are not touched.
    public static partial class Match5v5MapBuilder
    {
        internal const string JungleDataFolder = "Assets/_Game/Data/Jungle";
        internal const string JunglePrefabFolder = "Assets/_Game/Prefabs/Jungle";
        private const string JungleStatusFolder = JungleDataFolder + "/StatusEffects";
        private const string JungleMonsterDataFolder = JungleDataFolder + "/Monsters";
        private const string JungleCampDataFolder = JungleDataFolder + "/Camps";
        private const string JungleRewardFolder = JungleDataFolder + "/Rewards";
        private const string JungleSetupPath = JungleDataFolder + "/JungleSetup_Match5v5.asset";
        private const string JungleMovementPath = JungleMonsterDataFolder + "/JNG_MonsterMovement.asset";
        private const string JungleVexSheetPath = "Assets/_Game/Art/Characters/_Placeholder/Vex/Vex_Sheet.png";
        private const string JungleShadowSpritePath = "Assets/_Game/Art/Characters/_Placeholder/Shared/CharacterShadow.png";
        private const string JungleDummyPrefabPath = "Assets/_Game/Prefabs/Characters/CombatDummy.prefab";
        private const string JungleArrowPrefabPath = "Assets/_Game/Prefabs/Gameplay/Projectiles/MinionArrowProjectile.prefab";
        private const string JungleRootName = "Jungle";
        private const string JunglePresentationName = "JunglePresentation";

        [MenuItem("IndieMoba/Map 5v5/5. Wire Jungle (Non-Destructive)", false, 104)]
        public static void WireJungle()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MatchScenePath) == null || AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath) == null)
            {
                EditorUtility.DisplayDialog("Wire Jungle", "Match5v5.unity or Map5v5_Gameplay.prefab is missing. Run '4. Wire Match5v5 Systems' first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            int createdAssets = 0;
            JungleSetup setup = EnsureJungleAssets(ref createdAssets);
            AssetDatabase.SaveAssets();
            if (setup == null)
            {
                EditorUtility.DisplayDialog("Wire Jungle", "Could not create or load " + JungleSetupPath + ". See the Console.", "OK");
                return;
            }
            // Pass the path, not the object: opening the scene in Single mode unloads assets no scene references,
            // which turns references created/loaded before OpenScene into destroyed objects.
            WireJungleScene(createdAssets);
        }

        // ---------------------------------------------------------------- assets

        private static JungleSetup EnsureJungleAssets(ref int created)
        {
            EnsureFolder(JungleStatusFolder);
            EnsureFolder(JungleMonsterDataFolder);
            EnsureFolder(JungleCampDataFolder);
            EnsureFolder(JungleRewardFolder);
            EnsureFolder(JunglePrefabFolder);

            // Status effects (Phase 5B approved values).
            var scorch = CreateAssetIfMissing<StatusEffectDefinition>(JungleStatusFolder + "/SE_Scorch.asset", so =>
            {
                so.FindProperty("displayName").stringValue = "Scorch";
                so.FindProperty("isBuff").boolValue = false;
                so.FindProperty("duration").floatValue = 3f;
                so.FindProperty("damagePerSecond").floatValue = 12f;
                so.FindProperty("damageTickInterval").floatValue = 0.5f;
                so.FindProperty("displayColor").colorValue = new Color(1f, 0.5f, 0.15f, 1f);
            }, ref created);
            var emberSlow = CreateAssetIfMissing<StatusEffectDefinition>(JungleStatusFolder + "/SE_EmberSlow.asset", so =>
            {
                so.FindProperty("displayName").stringValue = "Ember Slow";
                so.FindProperty("isBuff").boolValue = false;
                so.FindProperty("duration").floatValue = 1.5f;
                so.FindProperty("moveSpeedMultiplier").floatValue = 0.85f;
                so.FindProperty("displayColor").colorValue = new Color(0.65f, 0.6f, 1f, 1f);
            }, ref created);
            var verdantBloom = CreateAssetIfMissing<StatusEffectDefinition>(JungleStatusFolder + "/SE_VerdantBloom.asset", so =>
            {
                so.FindProperty("displayName").stringValue = "Verdant Bloom";
                so.FindProperty("isBuff").boolValue = true;
                so.FindProperty("duration").floatValue = 80f;
                so.FindProperty("cooldownRecoveryMultiplier").floatValue = 1.15f;
                so.FindProperty("healthRegenPerSecond").floatValue = 8f;
                so.FindProperty("displayColor").colorValue = new Color(0.35f, 1f, 0.45f, 1f);
            }, ref created);
            var emberBlight = CreateAssetIfMissing<StatusEffectDefinition>(JungleStatusFolder + "/SE_EmberBlight.asset", so =>
            {
                so.FindProperty("displayName").stringValue = "Ember Blight";
                so.FindProperty("isBuff").boolValue = true;
                so.FindProperty("duration").floatValue = 80f;
                so.FindProperty("displayColor").colorValue = new Color(1f, 0.35f, 0.2f, 1f);
                var onHit = so.FindProperty("onBasicAttackHit");
                onHit.arraySize = 2;
                ConfigureOnHit(onHit.GetArrayElementAtIndex(0), scorch);
                ConfigureOnHit(onHit.GetArrayElementAtIndex(1), emberSlow);
            }, ref created);

            var verdantReward = CreateAssetIfMissing<JungleRewardProfile>(JungleRewardFolder + "/JNG_Reward_VerdantBloom.asset", so =>
            {
                so.FindProperty("carrierBuff").objectReferenceValue = verdantBloom;
            }, ref created);
            var emberReward = CreateAssetIfMissing<JungleRewardProfile>(JungleRewardFolder + "/JNG_Reward_EmberBlight.asset", so =>
            {
                so.FindProperty("carrierBuff").objectReferenceValue = emberBlight;
            }, ref created);

            var movement = CreateAssetIfMissing<CharacterMovementConfig>(JungleMovementPath, so =>
            {
                so.FindProperty("maxSpeed").floatValue = 3.5f;
                so.FindProperty("acceleration").floatValue = 60f;
                so.FindProperty("deceleration").floatValue = 90f;
                so.FindProperty("stopDistance").floatValue = 0.1f;
                so.FindProperty("collisionRadius").floatValue = 0.35f;
                so.FindProperty("skinWidth").floatValue = 0.02f;
                so.FindProperty("maxSlideIterations").intValue = 3;
                so.FindProperty("obstacleMask").intValue = 1 << ObstacleLayer;
                so.FindProperty("facingThreshold").floatValue = 0.05f;
            }, ref created);

            // Monster archetypes (Phase 5B playtest values).
            var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(JungleArrowPrefabPath);
            var buffElite = MonsterConfig("JNG_Config_BuffElite", "Buff elite", JungleAttackType.Melee, 1500f, 22f, 1.5f, 1.2f, 0.6f, null, movement, ref created);
            var largeMelee = MonsterConfig("JNG_Config_LargeMelee", "Large melee", JungleAttackType.Melee, 900f, 22f, 1.25f, 1.1f, 0.55f, null, movement, ref created);
            var mediumMelee = MonsterConfig("JNG_Config_MediumMelee", "Medium melee", JungleAttackType.Melee, 520f, 16f, 1.2f, 1f, 0.45f, null, movement, ref created);
            var smallMelee = MonsterConfig("JNG_Config_SmallMelee", "Small melee", JungleAttackType.Melee, 220f, 10f, 1.2f, 0.9f, 0.3f, null, movement, ref created);
            var smallRanged = MonsterConfig("JNG_Config_SmallRanged", "Small ranged", JungleAttackType.Ranged, 200f, 9f, 1.5f, 4.5f, 0.3f, arrow, movement, ref created);

            // Placeholder monsters (provisional names, Vex sheet tinted/scaled; no final art).
            var blightWarden = MonsterPrefab("JNG_BlightWarden", buffElite, new Color(0.8f, 0.4f, 0.9f), 1.6f, ref created);
            var ashMite = MonsterPrefab("JNG_AshMite", smallMelee, new Color(0.9f, 0.55f, 0.4f), 0.75f, ref created);
            var rootGuardian = MonsterPrefab("JNG_RootGuardian", buffElite, new Color(0.45f, 0.85f, 0.45f), 1.6f, ref created);
            var sporeWisp = MonsterPrefab("JNG_SporeWisp", smallRanged, new Color(0.5f, 0.95f, 0.85f), 0.7f, ref created);
            var twistedBoar = MonsterPrefab("JNG_TwistedBoar", largeMelee, new Color(0.6f, 0.4f, 0.7f), 1.35f, ref created);
            var mossBoar = MonsterPrefab("JNG_MossBoar", largeMelee, new Color(0.55f, 0.8f, 0.4f), 1.35f, ref created);
            var shadeStalker = MonsterPrefab("JNG_ShadeStalker", mediumMelee, new Color(0.45f, 0.5f, 0.8f), 1.15f, ref created);
            var crystalMite = MonsterPrefab("JNG_CrystalMite", smallMelee, new Color(0.7f, 0.9f, 1f), 0.75f, ref created);
            var fungalBrute = MonsterPrefab("JNG_FungalBrute", mediumMelee, new Color(0.9f, 0.75f, 0.5f), 1.15f, ref created);

            Vector2 lead = new Vector2(0f, 0.6f);
            Vector2 left = new Vector2(-1.4f, -0.8f);
            Vector2 right = new Vector2(1.4f, -0.8f);
            var campBlight = CampConfig("JNG_Camp_BlightWarden", "Blight Warden", JungleCampType.Buff, emberReward,
                new[] { Member(blightWarden, lead, true), Member(ashMite, left, false), Member(ashMite, right, false) }, ref created);
            var campRoot = CampConfig("JNG_Camp_RootGuardian", "Root Guardian", JungleCampType.Buff, verdantReward,
                new[] { Member(rootGuardian, lead, true), Member(sporeWisp, left, false), Member(sporeWisp, right, false) }, ref created);
            var campTwisted = CampConfig("JNG_Camp_TwistedBoar", "Twisted Boar", JungleCampType.Small, null,
                new[] { Member(twistedBoar, Vector2.zero, false) }, ref created);
            var campMoss = CampConfig("JNG_Camp_MossBoar", "Moss Boar", JungleCampType.Small, null,
                new[] { Member(mossBoar, Vector2.zero, false) }, ref created);
            var campShade = CampConfig("JNG_Camp_ShadeStalker", "Shade Stalker", JungleCampType.Small, null,
                new[] { Member(shadeStalker, lead, false), Member(crystalMite, left, false), Member(crystalMite, right, false) }, ref created);
            var campFungal = CampConfig("JNG_Camp_FungalBrute", "Fungal Brute", JungleCampType.Small, null,
                new[] { Member(fungalBrute, lead, false), Member(sporeWisp, left, false), Member(sporeWisp, right, false) }, ref created);

            // Clash-side quadrants (BlueUpper, RedLower) get the Blight camps, Farm-side (BlueLower, RedUpper)
            // the Verdant camps; each team owns one buff of each kind. Optional clearings stay empty.
            return CreateAssetIfMissing<JungleSetup>(JungleSetupPath, so =>
            {
                var entries = new (string zone, JungleCampConfig camp)[]
                {
                    ("Clearing_BlueUpper_Buff", campBlight), ("Clearing_BlueUpper_SmallA", campTwisted), ("Clearing_BlueUpper_SmallB", campShade),
                    ("Clearing_BlueLower_Buff", campRoot), ("Clearing_BlueLower_SmallA", campMoss), ("Clearing_BlueLower_SmallB", campFungal),
                    ("Clearing_RedUpper_Buff", campRoot), ("Clearing_RedUpper_SmallA", campMoss), ("Clearing_RedUpper_SmallB", campFungal),
                    ("Clearing_RedLower_Buff", campBlight), ("Clearing_RedLower_SmallA", campTwisted), ("Clearing_RedLower_SmallB", campShade)
                };
                var prop = so.FindProperty("entries");
                prop.arraySize = entries.Length;
                for (int i = 0; i < entries.Length; i++)
                {
                    var element = prop.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("zoneId").stringValue = entries[i].zone;
                    element.FindPropertyRelative("camp").objectReferenceValue = entries[i].camp;
                }
            }, ref created);
        }

        private static T CreateAssetIfMissing<T>(string path, System.Action<SerializedObject> configure, ref int created) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            var so = new SerializedObject(asset);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            created++;
            return asset;
        }

        private static void ConfigureOnHit(SerializedProperty element, StatusEffectDefinition effect)
        {
            element.FindPropertyRelative("effect").objectReferenceValue = effect;
            var kinds = element.FindPropertyRelative("targetKinds");
            kinds.arraySize = 2;
            kinds.GetArrayElementAtIndex(0).intValue = (int)CombatTargetKind.Hero;
            kinds.GetArrayElementAtIndex(1).intValue = (int)CombatTargetKind.Monster;
        }

        private static JungleMonsterConfig MonsterConfig(string fileName, string displayName, JungleAttackType type, float hp, float damage,
            float interval, float range, float radius, GameObject projectilePrefab, CharacterMovementConfig movement, ref int created)
        {
            return CreateAssetIfMissing<JungleMonsterConfig>(JungleMonsterDataFolder + "/" + fileName + ".asset", so =>
            {
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("attackType").intValue = (int)type;
                so.FindProperty("maxHealth").floatValue = hp;
                so.FindProperty("damage").floatValue = damage;
                so.FindProperty("attackInterval").floatValue = interval;
                so.FindProperty("attackRange").floatValue = range;
                so.FindProperty("radius").floatValue = radius;
                so.FindProperty("projectileSpeed").floatValue = 10f;
                so.FindProperty("projectileHitRadius").floatValue = 0.125f;
                so.FindProperty("projectileOriginOffset").vector2Value = new Vector2(0f, 0.4f);
                so.FindProperty("projectileViewPrefab").objectReferenceValue = projectilePrefab;
                so.FindProperty("movementConfig").objectReferenceValue = movement;
                so.FindProperty("corpseDuration").floatValue = 0.75f;
                so.FindProperty("separationRadius").floatValue = 0.9f;
                so.FindProperty("separationStrength").floatValue = 1f;
            }, ref created);
        }

        private static JungleCampMember Member(GameObject prefab, Vector2 offset, bool carrier)
        {
            return new JungleCampMember { monsterPrefab = prefab, localOffset = offset, buffCarrier = carrier };
        }

        private static JungleCampConfig CampConfig(string fileName, string displayName, JungleCampType type, JungleRewardProfile reward,
            JungleCampMember[] members, ref int created)
        {
            bool buff = type == JungleCampType.Buff;
            return CreateAssetIfMissing<JungleCampConfig>(JungleCampDataFolder + "/" + fileName + ".asset", so =>
            {
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("campType").intValue = (int)type;
                so.FindProperty("firstSpawnTime").floatValue = 20f;
                so.FindProperty("respawnTime").floatValue = buff ? 90f : 55f;
                so.FindProperty("leashRadius").floatValue = buff ? 8f : 6.5f;
                so.FindProperty("acquireRadius").floatValue = buff ? 6f : 5f;
                so.FindProperty("resetAfterNoDamage").floatValue = 6f;
                so.FindProperty("returnSpeed").floatValue = 7f;
                so.FindProperty("resetHealDuration").floatValue = 1.5f;
                so.FindProperty("returnTimeout").floatValue = 4f;
                so.FindProperty("retargetIntervalTicks").intValue = 15;
                so.FindProperty("reward").objectReferenceValue = reward;
                var prop = so.FindProperty("members");
                prop.arraySize = members.Length;
                for (int i = 0; i < members.Length; i++)
                {
                    var element = prop.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("monsterPrefab").objectReferenceValue = members[i].monsterPrefab;
                    element.FindPropertyRelative("localOffset").vector2Value = members[i].localOffset;
                    element.FindPropertyRelative("buffCarrier").boolValue = members[i].buffCarrier;
                }
            }, ref created);
        }

        private static GameObject MonsterPrefab(string prefabName, JungleMonsterConfig config, Color tint, float scale, ref int created)
        {
            string path = JunglePrefabFolder + "/" + prefabName + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }
            var vexSprites = AssetDatabase.LoadAllAssetsAtPath(JungleVexSheetPath).OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                .ToArray();
            if (vexSprites.Length == 0)
            {
                LogWarning("Vex placeholder sheet not found at " + JungleVexSheetPath + "; monster " + prefabName + " has no sprite.");
            }

            var root = new GameObject(prefabName);
            var health = root.AddComponent<Health>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("maxHealth").floatValue = config.MaxHealth;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            var target = root.AddComponent<CombatTarget>();
            var targetSo = new SerializedObject(target);
            targetSo.FindProperty("team").intValue = (int)Team.Neutral;
            targetSo.FindProperty("kind").intValue = (int)CombatTargetKind.Monster;
            targetSo.FindProperty("radius").floatValue = config.Radius;
            targetSo.FindProperty("health").objectReferenceValue = health;
            targetSo.ApplyModifiedPropertiesWithoutUndo();

            var status = root.AddComponent<StatusEffectController>();
            var statusSo = new SerializedObject(status);
            statusSo.FindProperty("health").objectReferenceValue = health;
            statusSo.FindProperty("target").objectReferenceValue = target;
            statusSo.ApplyModifiedPropertiesWithoutUndo();

            var monster = root.AddComponent<JungleMonster>();
            var monsterSo = new SerializedObject(monster);
            monsterSo.FindProperty("config").objectReferenceValue = config;
            monsterSo.FindProperty("target").objectReferenceValue = target;
            monsterSo.FindProperty("health").objectReferenceValue = health;
            monsterSo.FindProperty("statusEffects").objectReferenceValue = status;
            monsterSo.ApplyModifiedPropertiesWithoutUndo();

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            var shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(visualRoot.transform, false);
            shadowGO.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadowGO.transform.localScale = new Vector3(scale, scale, 1f);
            var shadowSr = shadowGO.AddComponent<SpriteRenderer>();
            shadowSr.sprite = LoadSprite(JungleShadowSpritePath);
            shadowSr.sortingLayerName = "Actors";
            shadowSr.sortingOrder = -1;

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(visualRoot.transform, false);
            bodyGO.transform.localScale = new Vector3(scale, scale, 1f);
            var bodySr = bodyGO.AddComponent<SpriteRenderer>();
            bodySr.sprite = vexSprites.Length > 0 ? vexSprites[0] : null;
            bodySr.flipX = true;
            bodySr.color = tint;
            bodySr.sortingLayerName = "Actors";
            bodySr.sortingOrder = 0;
            bodySr.spriteSortPoint = SpriteSortPoint.Pivot;
            float bodyTop = bodySr.sprite != null ? bodySr.sprite.bounds.max.y * scale : scale;

            var circle = LoadSprite(VfxFolder + "Circle.png");
            var immuneGO = new GameObject("ImmuneIndicator");
            immuneGO.transform.SetParent(visualRoot.transform, false);
            float bubble = Mathf.Max(config.Radius * 2.6f, bodyTop * 0.9f);
            float circleSize = circle != null ? Mathf.Max(0.0001f, circle.bounds.size.x) : 1f;
            immuneGO.transform.localPosition = new Vector3(0f, bodyTop * 0.45f, 0f);
            immuneGO.transform.localScale = new Vector3(bubble / circleSize, bubble / circleSize, 1f);
            var immuneSr = immuneGO.AddComponent<SpriteRenderer>();
            immuneSr.sprite = circle;
            immuneSr.color = new Color(0.6f, 0.85f, 1f, 0.3f);
            immuneSr.sortingLayerName = "Actors";
            immuneSr.sortingOrder = 2;
            immuneGO.SetActive(false);

            var markerGO = new GameObject("CarrierMarker");
            markerGO.transform.SetParent(visualRoot.transform, false);
            markerGO.transform.localPosition = new Vector3(0f, bodyTop + 0.55f, 0f);
            var markerSr = markerGO.AddComponent<SpriteRenderer>();
            markerSr.sprite = LoadSprite(VfxFolder + "Ember.png");
            markerSr.color = new Color(1f, 0.85f, 0.3f, 1f);
            markerSr.sortingLayerName = "Overhead";
            markerSr.sortingOrder = 40;
            markerGO.SetActive(false);

            var presenter = visualRoot.AddComponent<JungleMonsterPresenter>();
            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("monster").objectReferenceValue = monster;
            presenterSo.FindProperty("flipRoot").objectReferenceValue = bodyGO.transform;
            presenterSo.FindProperty("immuneIndicator").objectReferenceValue = immuneGO;
            presenterSo.FindProperty("carrierMarker").objectReferenceValue = markerGO;
            presenterSo.FindProperty("interpolate").boolValue = true;
            presenterSo.FindProperty("artFacesRight").boolValue = true;
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var flash = visualRoot.AddComponent<HealthFlashView>();
            var flashSo = new SerializedObject(flash);
            flashSo.FindProperty("renderers").arraySize = 1;
            flashSo.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = bodySr;
            flashSo.ApplyModifiedPropertiesWithoutUndo();

            var barGO = new GameObject("HealthBar");
            barGO.transform.SetParent(visualRoot.transform, false);
            barGO.transform.localPosition = new Vector3(0f, bodyTop + 0.2f, 0f);
            var bar = barGO.AddComponent<HealthBarView>();
            var barSo = new SerializedObject(bar);
            barSo.FindProperty("health").objectReferenceValue = health;
            barSo.FindProperty("barSprite").objectReferenceValue = LoadSprite(VfxFolder + "Bar.png");
            barSo.FindProperty("size").vector2Value = new Vector2(Mathf.Clamp(0.7f * scale, 0.6f, 1.4f), 0.1f);
            barSo.FindProperty("backgroundColor").colorValue = new Color(0f, 0f, 0f, 0.7f);
            barSo.FindProperty("fillColor").colorValue = new Color(1f, 0.75f, 0.25f, 1f);
            barSo.FindProperty("hideWhenFull").boolValue = false;
            barSo.FindProperty("hideWhenDead").boolValue = true;
            barSo.FindProperty("sortingLayer").stringValue = "Overhead";
            barSo.FindProperty("sortingOrder").intValue = 50;
            barSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            created++;
            return prefab;
        }

        // ---------------------------------------------------------------- scene

        private static void WireJungleScene(int createdAssets)
        {
            Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            // Reload after OpenScene (same pattern as menu 4); see WireJungle.
            var setup = AssetDatabase.LoadAssetAtPath<JungleSetup>(JungleSetupPath);
            if (setup == null)
            {
                EditorUtility.DisplayDialog("Wire Jungle", "Could not load " + JungleSetupPath + " after opening Match5v5.unity.", "OK");
                return;
            }
            var systemsRoot = FindRoot(scene, "Match5v5_Systems");
            var runner = systemsRoot != null ? systemsRoot.GetComponentInChildren<SimulationTickRunner>(true) : null;
            var world = systemsRoot != null ? systemsRoot.GetComponentInChildren<CombatWorld>(true) : null;
            if (runner == null || world == null)
            {
                EditorUtility.DisplayDialog("Wire Jungle", "Match5v5_Systems (SimulationTickRunner, CombatWorld) not found in Match5v5.unity. Run '4. Wire Match5v5 Systems' first.", "OK");
                return;
            }
            var gameplayAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath);
            GameObject gameplayInstance = scene.GetRootGameObjects().FirstOrDefault(r => IsPrefabInstanceOf(r, gameplayAsset));
            if (gameplayInstance == null)
            {
                EditorUtility.DisplayDialog("Wire Jungle", "Map5v5_Gameplay instance not found in Match5v5.unity. Run '4. Wire Match5v5 Systems' first.", "OK");
                return;
            }
            var zones = gameplayInstance.GetComponentsInChildren<MapZoneMarker>(true);
            Log("Map5v5_Gameplay clearings found: " +
                zones.Count(z => z != null && z.ZoneType == MapZoneType.BuffClearing) + " buff, " +
                zones.Count(z => z != null && z.ZoneType == MapZoneType.SmallClearing) + " small, " +
                zones.Count(z => z != null && z.ZoneType == MapZoneType.OptionalClearing) + " optional (left empty).");

            var jungleRoot = FindOrCreateChild(systemsRoot.transform, JungleRootName);
            var director = jungleRoot.GetComponent<JungleDirector>();
            if (director == null)
            {
                director = jungleRoot.AddComponent<JungleDirector>();
            }

            var camps = new List<JungleCamp>();
            int createdCamps = 0;
            int keptCamps = 0;
            var entries = setup.Entries ?? new JungleSetupEntry[0];
            for (int i = 0; i < entries.Length; i++)
            {
                JungleSetupEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.zoneId) || entry.camp == null)
                {
                    LogWarning("JungleSetup entry " + i + " is incomplete (" +
                        (entry == null ? "null entry" : string.IsNullOrEmpty(entry.zoneId) ? "missing zoneId" : "missing camp config for " + entry.zoneId) + "); skipped.");
                    continue;
                }
                MapZoneMarker marker = zones.FirstOrDefault(z => z != null && z.ZoneId == entry.zoneId);
                if (marker == null)
                {
                    LogWarning("Zone '" + entry.zoneId + "' not found in Map5v5_Gameplay; camp skipped.");
                    continue;
                }
                if (marker.ZoneType != MapZoneType.BuffClearing && marker.ZoneType != MapZoneType.SmallClearing)
                {
                    LogWarning("Zone '" + entry.zoneId + "' is " + marker.ZoneType + "; only Buff/Small clearings receive camps. Skipped.");
                    continue;
                }
                string campName = "Camp_" + entry.zoneId.Replace("Clearing_", "");
                Transform existing = FindChildRecursive(jungleRoot.transform, campName);
                JungleCamp camp = existing != null ? existing.GetComponent<JungleCamp>() : null;
                if (camp != null)
                {
                    keptCamps++;
                }
                else
                {
                    camp = CreateCamp(scene, jungleRoot.transform, campName, entry, marker.Position);
                    createdCamps++;
                }
                WireCampMembers(camp, runner, world);
                camps.Add(camp);
            }

            var allCamps = jungleRoot.GetComponentsInChildren<JungleCamp>(true);
            for (int i = 0; i < allCamps.Length; i++)
            {
                if (!camps.Contains(allCamps[i]))
                {
                    LogWarning("Camp '" + allCamps[i].name + "' is not listed in JungleSetup; left untouched and not driven by the director.");
                }
            }

            var directorSo = new SerializedObject(director);
            directorSo.FindProperty("runner").objectReferenceValue = runner;
            directorSo.FindProperty("world").objectReferenceValue = world;
            var campsProp = directorSo.FindProperty("camps");
            campsProp.arraySize = camps.Count;
            for (int i = 0; i < camps.Count; i++)
            {
                campsProp.GetArrayElementAtIndex(i).objectReferenceValue = camps[i];
            }
            directorSo.ApplyModifiedPropertiesWithoutUndo();

            // Every hero in the scene gets a StatusEffectController (buffs, slows, cooldown recovery).
            var heroes = new List<HeroCombat>();
            foreach (var root in scene.GetRootGameObjects())
            {
                heroes.AddRange(root.GetComponentsInChildren<HeroCombat>(true));
            }
            StatusEffectController playerEffects = null;
            HeroCombat playerCombat = null;
            var playerTransform = FindChildRecursive(systemsRoot.transform, "Hero");
            foreach (var hero in heroes)
            {
                var controller = EnsureHeroStatusController(hero, runner, world);
                if (playerTransform != null && hero.transform == playerTransform)
                {
                    playerEffects = controller;
                    playerCombat = hero;
                }
            }
            if (playerEffects == null && heroes.Count > 0)
            {
                playerCombat = heroes[0];
                playerEffects = playerCombat.GetComponent<StatusEffectController>();
            }

            var presentation = FindOrCreateChild(systemsRoot.transform, JunglePresentationName);
            var aura = presentation.GetComponent<StatusEffectAuraSpawner>();
            if (aura == null)
            {
                aura = presentation.AddComponent<StatusEffectAuraSpawner>();
            }
            var auraSo = new SerializedObject(aura);
            if (auraSo.FindProperty("ringSprite").objectReferenceValue == null)
            {
                auraSo.FindProperty("ringSprite").objectReferenceValue = LoadSprite(VfxFolder + "Ring.png");
            }
            auraSo.ApplyModifiedPropertiesWithoutUndo();
            var testTool = presentation.GetComponent<JungleTestHeroTool>();
            if (testTool == null)
            {
                testTool = presentation.AddComponent<JungleTestHeroTool>();
            }
            var testSo = new SerializedObject(testTool);
            testSo.FindProperty("dummyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(JungleDummyPrefabPath);
            testSo.FindProperty("player").objectReferenceValue = playerCombat != null ? playerCombat.transform : null;
            testSo.ApplyModifiedPropertiesWithoutUndo();
            var overlay = presentation.GetComponent<JungleDebugOverlay>();
            if (overlay == null)
            {
                overlay = presentation.AddComponent<JungleDebugOverlay>();
            }
            var overlaySo = new SerializedObject(overlay);
            overlaySo.FindProperty("director").objectReferenceValue = director;
            overlaySo.FindProperty("playerEffects").objectReferenceValue = playerEffects;
            overlaySo.FindProperty("playerCombat").objectReferenceValue = playerCombat;
            overlaySo.FindProperty("testHeroTool").objectReferenceValue = testTool;
            overlaySo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, MatchScenePath))
            {
                throw new System.InvalidOperationException("Failed to save scene: " + MatchScenePath);
            }
            Log("Wired jungle: " + camps.Count + " camps (" + createdCamps + " created, " + keptCamps + " kept), " +
                heroes.Count + " hero(es) with StatusEffectController, " + createdAssets + " jungle asset(s)/prefab(s) created.");
            if (camps.Count != 12)
            {
                LogWarning("Expected 12 active camps, found " + camps.Count + ".");
            }
            if (heroes.Count < 2)
            {
                Log("Only " + heroes.Count + " hero in Match5v5; use F3 > 'Spawn test hero (Red)' to test hero-vs-hero status effects (runtime only).");
            }
        }

        private static JungleCamp CreateCamp(Scene scene, Transform parent, string campName, JungleSetupEntry entry, Vector2 position)
        {
            var go = new GameObject(campName);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var camp = go.AddComponent<JungleCamp>();
            var members = new List<JungleMonster>();
            JungleMonster carrier = null;
            JungleCampMember[] memberConfigs = entry.camp.Members ?? new JungleCampMember[0];
            for (int i = 0; i < memberConfigs.Length; i++)
            {
                JungleCampMember member = memberConfigs[i];
                if (member == null || member.monsterPrefab == null)
                {
                    LogWarning(entry.camp.name + " member " + i + " has no prefab; skipped.");
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(member.monsterPrefab, scene);
                instance.name = member.monsterPrefab.name + "_" + i;
                instance.transform.SetParent(go.transform, false);
                Vector2 p = position + member.localOffset;
                instance.transform.position = new Vector3(p.x, p.y, 0f);
                var monster = instance.GetComponent<JungleMonster>();
                if (monster == null)
                {
                    LogWarning(member.monsterPrefab.name + " has no JungleMonster component.");
                    continue;
                }
                members.Add(monster);
                if (member.buffCarrier)
                {
                    carrier = monster;
                }
            }
            var so = new SerializedObject(camp);
            so.FindProperty("config").objectReferenceValue = entry.camp;
            so.FindProperty("campId").stringValue = entry.zoneId;
            var prop = so.FindProperty("members");
            prop.arraySize = members.Count;
            for (int i = 0; i < members.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = members[i];
            }
            so.FindProperty("buffCarrier").objectReferenceValue = carrier;
            so.ApplyModifiedPropertiesWithoutUndo();
            return camp;
        }

        private static void WireCampMembers(JungleCamp camp, SimulationTickRunner runner, CombatWorld world)
        {
            foreach (var monster in camp.GetComponentsInChildren<JungleMonster>(true))
            {
                var healthSo = new SerializedObject(monster.GetComponent<Health>());
                healthSo.FindProperty("runner").objectReferenceValue = runner;
                healthSo.ApplyModifiedPropertiesWithoutUndo();
                var targetSo = new SerializedObject(monster.GetComponent<CombatTarget>());
                targetSo.FindProperty("world").objectReferenceValue = world;
                targetSo.ApplyModifiedPropertiesWithoutUndo();
                var status = monster.GetComponent<StatusEffectController>();
                if (status != null)
                {
                    var statusSo = new SerializedObject(status);
                    statusSo.FindProperty("runner").objectReferenceValue = runner;
                    statusSo.FindProperty("world").objectReferenceValue = world;
                    statusSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static StatusEffectController EnsureHeroStatusController(HeroCombat hero, SimulationTickRunner runner, CombatWorld world)
        {
            var controller = hero.GetComponent<StatusEffectController>();
            if (controller == null)
            {
                controller = hero.gameObject.AddComponent<StatusEffectController>();
            }
            var so = new SerializedObject(controller);
            so.FindProperty("runner").objectReferenceValue = runner;
            so.FindProperty("world").objectReferenceValue = world;
            so.FindProperty("health").objectReferenceValue = hero.GetComponent<Health>();
            so.FindProperty("target").objectReferenceValue = hero.GetComponent<CombatTarget>();
            so.FindProperty("heroCombat").objectReferenceValue = hero;
            so.FindProperty("heroActor").objectReferenceValue = hero.GetComponent<HeroActor>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }
    }
}
