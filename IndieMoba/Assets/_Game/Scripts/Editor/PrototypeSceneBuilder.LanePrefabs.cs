using IndieMoba.Characters;
using IndieMoba.Combat;
using IndieMoba.Core;
using IndieMoba.Minions;
using IndieMoba.Presentation;
using IndieMoba.Structures;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace IndieMoba.EditorTools
{
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildLanePrefabs()
        {
            EnsureFolder(MinionPrefabFolder.TrimEnd('/'));
            EnsureFolder(StructurePrefabFolder.TrimEnd('/'));
            BuildProjectilePrefab(MinionArrowPrefabPath, "MinionArrowProjectile", ArrowSpritePath, Vector3.one, Color.white, true);
            BuildProjectilePrefab(TowerProjectilePrefabPath, "TowerProjectile", TowerProjectileSpritePath, Vector3.one, Color.white, false);
            BuildProjectilePrefab(NexusProjectilePrefabPath, "NexusProjectile", TowerProjectileSpritePath, new Vector3(1.3f, 1.3f, 1f), new Color(1f, 0.9f, 0.6f, 1f), false);

            var movement = AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>(MinionMovementConfigPath);
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinionArrowPrefabPath);
            var towerProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TowerProjectilePrefabPath);
            var nexusProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NexusProjectilePrefabPath);

            var melee = BuildConfigAsset<MinionConfig>(MinionMeleeConfigPath, so =>
            {
                ConfigureMinion(so, MinionAttackType.Melee, 300f, 40f, 1.3125f, 1f, 0.28125f, null, movement);
            });
            var ranged = BuildConfigAsset<MinionConfig>(MinionRangedConfigPath, so =>
            {
                ConfigureMinion(so, MinionAttackType.Ranged, 200f, 30f, 6.25f, 1.3f, 0.25f, arrowPrefab, movement);
            });
            var tower = BuildConfigAsset<TowerConfig>(TowerConfigPath, so =>
            {
                so.FindProperty("range").floatValue = 7.8125f;
                so.FindProperty("heroDamage").floatValue = 145f;
                so.FindProperty("minionDamage").floatValue = 115f;
                so.FindProperty("attackInterval").floatValue = 1.1f;
                so.FindProperty("projectileSpeed").floatValue = 13.4375f;
                so.FindProperty("projectileHitRadius").floatValue = 0.1875f;
                so.FindProperty("projectileMaxLifetime").floatValue = 3f;
                so.FindProperty("projectileOriginOffset").vector2Value = new Vector2(0f, 1.3f);
                so.FindProperty("warningRange").floatValue = 13.125f;
                so.FindProperty("projectileViewPrefab").objectReferenceValue = towerProjectilePrefab;
            });
            var nexusDefense = BuildConfigAsset<NexusConfig>(NexusConfigPath, so =>
            {
                so.FindProperty("range").floatValue = 9f;
                so.FindProperty("heroDamage").floatValue = 180f;
                so.FindProperty("minionDamage").floatValue = 140f;
                so.FindProperty("attackInterval").floatValue = 1.2f;
                so.FindProperty("projectileSpeed").floatValue = 14f;
                so.FindProperty("projectileHitRadius").floatValue = 0.1875f;
                so.FindProperty("projectileMaxLifetime").floatValue = 3f;
                so.FindProperty("projectileOriginOffset").vector2Value = new Vector2(0f, 1.5f);
                so.FindProperty("warningRange").floatValue = 14f;
                so.FindProperty("projectileViewPrefab").objectReferenceValue = nexusProjectilePrefab;
            });

            for (int t = 0; t < TeamNames.Length; t++)
            {
                Team team = t == 0 ? Team.Blue : Team.Red;
                BuildMinionPrefab("Melee", t, team, melee);
                BuildMinionPrefab("Ranged", t, team, ranged);
                BuildStructurePrefab(false, t, team, tower);
                BuildStructurePrefab(true, t, team, nexusDefense);
            }
        }

        private static void ConfigureMinion(SerializedObject so, MinionAttackType type, float hp, float damage, float range, float interval,
            float radius, GameObject projectilePrefab, CharacterMovementConfig movement)
        {
            so.FindProperty("attackType").intValue = (int)type;
            so.FindProperty("maxHealth").floatValue = hp;
            so.FindProperty("damage").floatValue = damage;
            so.FindProperty("attackRange").floatValue = range;
            so.FindProperty("attackInterval").floatValue = interval;
            so.FindProperty("radius").floatValue = radius;
            so.FindProperty("aggroRange").floatValue = 7.8125f;
            so.FindProperty("leashRange").floatValue = 10f;
            so.FindProperty("structureDamageMultiplier").floatValue = 0.6f;
            so.FindProperty("projectileSpeed").floatValue = 11.875f;
            so.FindProperty("projectileHitRadius").floatValue = 0.125f;
            so.FindProperty("projectileOriginOffset").vector2Value = new Vector2(0f, 0.4f);
            so.FindProperty("projectileViewPrefab").objectReferenceValue = projectilePrefab;
            so.FindProperty("movementConfig").objectReferenceValue = movement;
            so.FindProperty("retargetIntervalTicks").intValue = 15;
            so.FindProperty("targetStickiness").floatValue = 1.25f;
            so.FindProperty("corpseDuration").floatValue = 1f;
            so.FindProperty("waypointReachDistance").floatValue = 0.5f;
            so.FindProperty("separationRadius").floatValue = 0.6f;
            so.FindProperty("separationStrength").floatValue = 1.2f;
            so.FindProperty("structureAvoidanceDistance").floatValue = 1f;
            so.FindProperty("structureAvoidanceStrength").floatValue = 1.5f;
            var priority = so.FindProperty("targetPriority");
            var defaults = MinionTargetPolicy.CreateDefaultPriority();
            priority.arraySize = defaults.Length;
            for (int i = 0; i < defaults.Length; i++)
            {
                priority.GetArrayElementAtIndex(i).intValue = (int)defaults[i];
            }
        }

        private static void BuildMinionPrefab(string type, int teamIndex, Team team, MinionConfig config)
        {
            string teamName = TeamNames[teamIndex];
            var sprites = LoadSheetSprites(MinionSheetPath(type, teamName));
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MinionControllerPath(type, teamName));

            var root = new GameObject("Minion_" + type + "_" + teamName);
            var health = root.AddComponent<Health>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("maxHealth").floatValue = config.MaxHealth;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            var target = root.AddComponent<CombatTarget>();
            var targetSo = new SerializedObject(target);
            targetSo.FindProperty("team").intValue = (int)team;
            targetSo.FindProperty("kind").intValue = (int)CombatTargetKind.Minion;
            targetSo.FindProperty("radius").floatValue = config.Radius;
            targetSo.FindProperty("health").objectReferenceValue = health;
            targetSo.ApplyModifiedPropertiesWithoutUndo();

            var combat = root.AddComponent<MinionCombat>();
            var controllerComponent = root.AddComponent<MinionController>();
            var minionSo = new SerializedObject(controllerComponent);
            minionSo.FindProperty("config").objectReferenceValue = config;
            minionSo.FindProperty("target").objectReferenceValue = target;
            minionSo.FindProperty("health").objectReferenceValue = health;
            minionSo.FindProperty("combat").objectReferenceValue = combat;
            minionSo.ApplyModifiedPropertiesWithoutUndo();

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);

            var shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(visualRoot.transform, false);
            shadowGO.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadowGO.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            var shadowSr = shadowGO.AddComponent<SpriteRenderer>();
            shadowSr.sprite = LoadSprite(ShadowSpritePath);
            shadowSr.sortingLayerName = "Actors";
            shadowSr.sortingOrder = -1;

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(visualRoot.transform, false);
            var bodySr = bodyGO.AddComponent<SpriteRenderer>();
            bodySr.sprite = sprites.Length > 0 ? sprites[0] : null;
            bodySr.sortingLayerName = "Actors";
            bodySr.sortingOrder = 0;
            bodySr.spriteSortPoint = SpriteSortPoint.Pivot;
            var animator = bodyGO.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;

            var presenter = visualRoot.AddComponent<MinionPresenter>();
            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("minion").objectReferenceValue = controllerComponent;
            presenterSo.FindProperty("flipRoot").objectReferenceValue = bodyGO.transform;
            presenterSo.FindProperty("animator").objectReferenceValue = animator;
            presenterSo.FindProperty("interpolate").boolValue = true;
            presenterSo.FindProperty("artFacesRight").boolValue = true;
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var flash = visualRoot.AddComponent<HealthFlashView>();
            var flashSo = new SerializedObject(flash);
            flashSo.FindProperty("renderers").arraySize = 1;
            flashSo.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = bodySr;
            flashSo.ApplyModifiedPropertiesWithoutUndo();

            AddHealthBar(visualRoot.transform, health, new Vector3(0f, 1.0f, 0f), new Vector2(0.8f, 0.1f), team == Team.Blue ? BlueColor : RedColor, true);

            PrefabUtility.SaveAsPrefabAsset(root, MinionPrefabPath(type, teamName));
            Object.DestroyImmediate(root);
        }

        private static void AddHealthBar(Transform parent, Health health, Vector3 localPosition, Vector2 size, Color fill, bool hideWhenFull)
        {
            var barGO = new GameObject("HealthBar");
            barGO.transform.SetParent(parent, false);
            barGO.transform.localPosition = localPosition;
            var bar = barGO.AddComponent<HealthBarView>();
            var barSo = new SerializedObject(bar);
            barSo.FindProperty("health").objectReferenceValue = health;
            barSo.FindProperty("barSprite").objectReferenceValue = LoadSprite(BarSpritePath);
            barSo.FindProperty("size").vector2Value = size;
            barSo.FindProperty("backgroundColor").colorValue = new Color(0f, 0f, 0f, 0.7f);
            barSo.FindProperty("fillColor").colorValue = fill;
            barSo.FindProperty("hideWhenFull").boolValue = hideWhenFull;
            barSo.FindProperty("hideWhenDead").boolValue = true;
            barSo.FindProperty("sortingLayer").stringValue = "Overhead";
            barSo.FindProperty("sortingOrder").intValue = 50;
            barSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildStructurePrefab(bool nexus, int teamIndex, Team team, StructureDefenseConfig defenseConfig)
        {
            string teamName = TeamNames[teamIndex];
            string kindName = nexus ? "Nexus" : "Tower";
            float radius = nexus ? NexusRadius : TowerRadius;

            var root = new GameObject(kindName + "_" + teamName);
            root.layer = 6;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = radius;

            var health = root.AddComponent<Health>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("maxHealth").floatValue = nexus ? 5000f : 3000f;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            var target = root.AddComponent<CombatTarget>();
            var targetSo = new SerializedObject(target);
            targetSo.FindProperty("team").intValue = (int)team;
            targetSo.FindProperty("kind").intValue = (int)CombatTargetKind.Structure;
            targetSo.FindProperty("radius").floatValue = radius;
            targetSo.FindProperty("health").objectReferenceValue = health;
            targetSo.ApplyModifiedPropertiesWithoutUndo();

            var structure = root.AddComponent<Structure>();
            var structureSo = new SerializedObject(structure);
            structureSo.FindProperty("health").objectReferenceValue = health;
            structureSo.FindProperty("target").objectReferenceValue = target;
            structureSo.FindProperty("blockingCollider").objectReferenceValue = collider;
            var slots = structureSo.FindProperty("allowedDamageSlots");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).intValue = (int)AbilitySlot.BasicAttack;
            structureSo.FindProperty("protectionEnabled").boolValue = true;
            structureSo.FindProperty("protectionRadius").floatValue = 7.8125f;
            structureSo.FindProperty("protectedDamageMultiplier").floatValue = 0.2f;
            structureSo.ApplyModifiedPropertiesWithoutUndo();

            if (nexus)
            {
                var objective = root.AddComponent<NexusObjective>();
                var objectiveSo = new SerializedObject(objective);
                objectiveSo.FindProperty("structure").objectReferenceValue = structure;
                objectiveSo.ApplyModifiedPropertiesWithoutUndo();
            }
            var targeting = root.AddComponent<TowerTargeting>();
            var targetingSo = new SerializedObject(targeting);
            targetingSo.FindProperty("config").objectReferenceValue = defenseConfig;
            targetingSo.FindProperty("structure").objectReferenceValue = structure;
            targetingSo.ApplyModifiedPropertiesWithoutUndo();
            var towerCombat = root.AddComponent<TowerCombat>();
            var towerCombatSo = new SerializedObject(towerCombat);
            towerCombatSo.FindProperty("config").objectReferenceValue = defenseConfig;
            towerCombatSo.FindProperty("targeting").objectReferenceValue = targeting;
            towerCombatSo.FindProperty("structure").objectReferenceValue = structure;
            towerCombatSo.ApplyModifiedPropertiesWithoutUndo();

            var visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.layer = 0;

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(visualRoot.transform, false);
            var bodySr = bodyGO.AddComponent<SpriteRenderer>();
            bodySr.sprite = LoadSprite(StructureArtFolder + kindName + "_" + teamName + ".png");
            bodySr.sortingLayerName = "Actors";
            bodySr.sortingOrder = 0;
            bodySr.spriteSortPoint = SpriteSortPoint.Pivot;

            var crystalGO = new GameObject("Crystal");
            crystalGO.transform.SetParent(visualRoot.transform, false);
            crystalGO.transform.localPosition = new Vector3(0f, nexus ? 27f / Ppu : 42f / Ppu, 0f);
            var crystalSr = crystalGO.AddComponent<SpriteRenderer>();
            crystalSr.sprite = LoadSprite(StructureArtFolder + kindName + "Crystal_" + teamName + ".png");
            crystalSr.sortingLayerName = "Actors";
            crystalSr.sortingOrder = 1;

            var bubbleGO = new GameObject("InvulnerableIndicator");
            bubbleGO.transform.SetParent(visualRoot.transform, false);
            float bubbleSize = radius * 2.6f;
            bubbleGO.transform.localPosition = new Vector3(0f, radius * 0.9f, 0f);
            bubbleGO.transform.localScale = new Vector3(bubbleSize / 2f, bubbleSize / 2f, 1f);
            var bubbleSr = bubbleGO.AddComponent<SpriteRenderer>();
            bubbleSr.sprite = LoadSprite(CircleSpritePath);
            bubbleSr.color = new Color(0.6f, 0.85f, 1f, 0.22f);
            bubbleSr.sortingLayerName = "Actors";
            bubbleSr.sortingOrder = 2;
            bubbleGO.SetActive(false);

            var view = visualRoot.AddComponent<StructureView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("structure").objectReferenceValue = structure;
            viewSo.FindProperty("bodyRenderer").objectReferenceValue = bodySr;
            viewSo.FindProperty("crystalRenderer").objectReferenceValue = crystalSr;
            viewSo.FindProperty("rubbleSprite").objectReferenceValue = LoadSprite(StructureArtFolder + "Rubble.png");
            viewSo.FindProperty("invulnerableIndicator").objectReferenceValue = bubbleGO;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var flash = visualRoot.AddComponent<HealthFlashView>();
            var flashSo = new SerializedObject(flash);
            flashSo.FindProperty("renderers").arraySize = 1;
            flashSo.FindProperty("renderers").GetArrayElementAtIndex(0).objectReferenceValue = bodySr;
            flashSo.ApplyModifiedPropertiesWithoutUndo();

            float barHeight = nexus ? 118f / Ppu + 0.25f : 86f / Ppu + 0.25f;
            AddHealthBar(visualRoot.transform, health, new Vector3(0f, barHeight, 0f), new Vector2(nexus ? 2.4f : 1.6f, 0.16f),
                team == Team.Blue ? BlueColor : RedColor, false);

            var ringGO = new GameObject("RangeIndicator");
            ringGO.transform.SetParent(visualRoot.transform, false);
            var ringSr = ringGO.AddComponent<SpriteRenderer>();
            ringSr.sprite = LoadSprite(RingSpritePath);
            ringSr.sortingLayerName = "GroundDecor";
            ringSr.sortingOrder = 10;
            ringSr.enabled = false;
            var rangeView = ringGO.AddComponent<TowerRangeView>();
            var rangeSo = new SerializedObject(rangeView);
            rangeSo.FindProperty("tower").objectReferenceValue = targeting;
            rangeSo.FindProperty("ringRenderer").objectReferenceValue = ringSr;
            rangeSo.FindProperty("showWarning").boolValue = true;
            rangeSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, StructurePrefabPath(kindName, teamName));
            Object.DestroyImmediate(root);
        }
    }
}
