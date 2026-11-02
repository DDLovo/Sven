using SvenMod.Modules;
using R2API;
using RoR2;
using UnityEngine;

namespace SvenMod
{
    internal static class SvenSurvivor
    {
        public const string PREFIX = "SVEN_";

        public static GameObject bodyPrefab;
        public static GameObject displayPrefab;
        public static CharacterBody body;
        public static CharacterModel model;
        public static GameObject defaultVisual;

        public static void Init(GameObject huntressBody = null)
        {

            if (!SvenAssets.Load<GameObject>("SvenModel"))
            {
                Log.Error("SvenModel prefab missing from the asset bundle, aborting survivor.");
                return;
            }

            if (!SvenAssets.Load<GameObject>("SvenImmortalModel")) throw new System.InvalidOperationException("Sven Immortal model missing");
            bodyPrefab = BuildBody(huntressBody);
            if (!bodyPrefab) return;

            displayPrefab = BuildDisplay();
            SvenSkills.Init(bodyPrefab);

            Content.CreateSurvivor(bodyPrefab, displayPrefab, Color.white, PREFIX, null, 102f);
            Log.Info($"survivor registered: body={bodyPrefab.name}, display={displayPrefab.name}, " +
                     $"skills={bodyPrefab.GetComponent<SkillLocator>().primary.skillFamily.variants.Length}");
        }

        private static GameObject BuildBody(GameObject original)
        {
            if (!original)
            {
                original = AddressablesUtil.Load<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab");
            }
            if (!original)
            {
                Log.Error("HuntressBody not found in Addressables, cannot build Sven body.");
                return null;
            }

            var prefab = original.InstantiateClone("SvenBody", true);
            var locator = prefab.GetComponent<ModelLocator>();
            var modelBase = locator.modelBaseTransform;

            var tracker = prefab.GetComponent<HuntressTracker>();
            if (tracker) Object.DestroyImmediate(tracker);

            Prefabs.ClearEntityStateMachines(prefab);
            Prefabs.AddMainEntityStateMachine(prefab, "Body",
                typeof(EntityStates.GenericCharacterMain), typeof(EntityStates.SpawnTeleporterState));
            Prefabs.AddEntityStateMachine(prefab, "Weapon");
            Prefabs.AddEntityStateMachine(prefab, "Stance");

            if (locator.modelTransform)
            {
                Object.DestroyImmediate(locator.modelTransform.gameObject);
            }

            var root = new GameObject("SvenModel");
            root.transform.SetParent(modelBase, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * SvenBalance.ModelScale;

            defaultVisual = AttachVisual(root.transform, SvenAssets.Load<GameObject>("SvenModel"), "Default");
            var arcana = AttachVisual(root.transform, SvenAssets.Load<GameObject>("SvenImmortalModel"), "Arcana");
            arcana.AddComponent<SvenAnimator>();
            arcana.SetActive(false);

            locator.modelTransform = root.transform;
            model = SetupModel(root, prefab);
            if (defaultVisual)
            {
                var animator = defaultVisual.GetComponent<Animator>();
                if (animator)
                {
                    // This serialized reference survives the spawned body's Awake.
                    locator.forcedAnimatorAtStart = animator;
                    locator.SetAnimator(animator);
                }
            }

            var direction = prefab.GetComponent<CharacterDirection>();
            if (direction)
            {
                direction.targetTransform = modelBase;
                direction.modelAnimator = defaultVisual ? defaultVisual.GetComponent<Animator>() : null;
            }

            body = prefab.GetComponent<CharacterBody>();
            SetupBody(prefab, body);
            prefab.AddComponent<SvenController>();

            var aimOrigin = new GameObject("AimOrigin").transform;
            aimOrigin.SetParent(root.transform, false);
            aimOrigin.localPosition = new Vector3(0f, 1.45f, 0f);
            body.aimOriginTransform = aimOrigin;

            var cameraPivot = prefab.transform.Find("CameraPivot");
            var cameraParams = prefab.GetComponent<CameraTargetParams>();
            if (cameraParams && cameraPivot) cameraParams.cameraPivotTransform = cameraPivot;

            Content.AddCharacterBodyPrefab(prefab);
            return prefab;
        }

        private static GameObject AttachVisual(Transform parent, GameObject prefab, string name)
        {
            if (!prefab) return null;
            var go = Object.Instantiate(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(t.name, boneName, System.StringComparison.OrdinalIgnoreCase)) return t;
            }
            return null;
        }

        private static CharacterModel SetupModel(GameObject root, GameObject bodyPrefab)
        {
            var childLocator = root.AddComponent<ChildLocator>();
            var pairs = new System.Collections.Generic.List<ChildLocator.NameTransformPair>();

            Transform Child(string name, Vector3 localPos, Transform parent = null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent ? parent : root.transform, false);
                go.transform.localPosition = localPos;
                pairs.Add(new ChildLocator.NameTransformPair { name = name, transform = go.transform });
                return go.transform;
            }

            var mainHurtbox = Child("MainHurtbox", new Vector3(0f, 0.95f, 0f));
            var headHurtbox = Child("HeadHurtbox", new Vector3(0f, 1.62f, 0f));
            Child("Head", new Vector3(0f, 1.6f, 0f));
            Child("Chest", new Vector3(0f, 1.25f, 0f));
            Child("FootL", new Vector3(0.16f, 0.08f, 0f));
            Child("FootR", new Vector3(-0.16f, 0.08f, 0f));

            // The weapon is bound to sword_1 in the FBX, so the muzzle rides
            // on that bone and follows every animation.
            var hand = defaultVisual ? FindBone(defaultVisual.transform, "sword_1") : null;
            if (hand)
            {
                // Imported bone coordinates are not world-space meters.
                // Start at the hand; firing applies its small offset in world space.
                Child("Muzzle", Vector3.zero, hand);
            }
            else
            {
                Child("Muzzle", new Vector3(0.25f, 1.3f, 0.45f));
            }

            childLocator.transformPairs = pairs.ToArray();

            var characterModel = root.AddComponent<CharacterModel>();
            characterModel.body = bodyPrefab.GetComponent<CharacterBody>();
            characterModel.autoPopulateLightInfos = true;

            var infos = new System.Collections.Generic.List<CharacterModel.RendererInfo>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i]) materials[i] = materials[i].ConvertDefaultShaderToHopoo();
                renderer.sharedMaterials = materials;

                infos.Add(new CharacterModel.RendererInfo
                {
                    renderer = renderer,
                    defaultMaterial = renderer.sharedMaterial,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false,
                    hideOnDeath = false,
                });
            }
            characterModel.baseRendererInfos = infos.ToArray();
            characterModel.mainSkinnedMeshRenderer = root.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (defaultVisual && !defaultVisual.GetComponent<SvenAnimator>())
            {
                defaultVisual.AddComponent<SvenAnimator>();
            }

            var hurtBoxGroup = root.AddComponent<HurtBoxGroup>();

            var capsule = mainHurtbox.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = Vector3.zero;
            capsule.radius = 0.32f;
            capsule.height = 1.5f;
            capsule.direction = 1;
            var mainHb = mainHurtbox.gameObject.AddComponent<HurtBox>();

            var sphere = headHurtbox.gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.2f;
            var headHb = headHurtbox.gameObject.AddComponent<HurtBox>();

            var hc = bodyPrefab.GetComponent<HealthComponent>();
            foreach (var hb in new[] { mainHb, headHb })
            {
                hb.gameObject.layer = LayerIndex.entityPrecise.intVal;
                hb.healthComponent = hc;
                hb.hurtBoxGroup = hurtBoxGroup;
                hb.damageModifier = HurtBox.DamageModifier.Normal;
            }

            mainHb.isBullseye = true;
            mainHb.indexInGroup = 0;
            headHb.isSniperTarget = true;
            headHb.indexInGroup = 1;

            hurtBoxGroup.hurtBoxes = new[] { mainHb, headHb };
            hurtBoxGroup.mainHurtBox = mainHb;
            hurtBoxGroup.bullseyeCount = 1;
            SetupDefaultSkin(root, characterModel);

            return characterModel;
        }

        private static void SetupDefaultSkin(GameObject root, CharacterModel characterModel)
        {
            var controller = root.AddComponent<ModelSkinController>();
            var variant = root.AddComponent<SvenModelVariant>();
            variant.normal = root.transform.Find("Default").gameObject;
            variant.arcana = root.transform.Find("Arcana").gameObject;
            variant.muzzle = root.GetComponent<ChildLocator>()?.FindChild("Muzzle");
            controller.skins = new[]
            {
                R2API.Skins.CreateNewSkinDef(new SkinDefParamsInfo
                {
                    Name = root.name + "DefaultSkin",
                    NameToken = PREFIX + "SKIN_DEFAULT_NAME",
                    Icon = SvenAssets.Icon("iconSvenPortrait"),
                    RootObject = root,
                    RendererInfos = characterModel.baseRendererInfos,
                    GameObjectActivations = new[] {
                        new SkinDefParams.GameObjectActivation { gameObject = variant.normal, shouldActivate = true },
                        new SkinDefParams.GameObjectActivation { gameObject = variant.arcana, shouldActivate = false } },
                }),
                R2API.Skins.CreateNewSkinDef(new SkinDefParamsInfo
                {
                    Name = root.name + "ArcanaSkin", NameToken = PREFIX + "SKIN_IMMORTAL_NAME",
                    Icon = SvenAssets.Icon("iconSvenImmortalPortrait"), RootObject = root,
                    RendererInfos = characterModel.baseRendererInfos,
                    GameObjectActivations = new[] {
                        new SkinDefParams.GameObjectActivation { gameObject = variant.normal, shouldActivate = false },
                        new SkinDefParams.GameObjectActivation { gameObject = variant.arcana, shouldActivate = true } },
                }),
            };
        }

        private static void SetupBody(GameObject prefab, CharacterBody characterBody)
        {
            characterBody.baseNameToken = PREFIX + "NAME";
            characterBody.subtitleNameToken = PREFIX + "SUBTITLE";
            characterBody.portraitIcon = SvenAssets.Load<Texture2D>("iconSvenPortrait");

            characterBody.baseMaxHealth = 160f;
            characterBody.levelMaxHealth = 48f;
            characterBody.baseRegen = 1.2f;
            characterBody.levelRegen = 0.24f;
            characterBody.baseArmor = 15f;
            characterBody.baseMaxShield = 0f;
            characterBody.baseDamage = 12f;
            characterBody.levelDamage = 2.4f;
            characterBody.baseAttackSpeed = 1f;
            characterBody.baseCrit = 1f;
            characterBody.baseMoveSpeed = 7f;
            characterBody.levelMoveSpeed = 0f;
            characterBody.baseJumpPower = 15f;
            characterBody.levelJumpPower = 0f;

            characterBody.baseJumpCount = 1;
            var motor = prefab.GetComponent<CharacterMotor>();
            if (motor) motor.jumpCount = 1;
        }

        private static GameObject BuildDisplay()
        {
            // Store under R2API's inactive prefab root. A live DontDestroyOnLoad
            // display updates CharacterModel before elite catalogs exist.
            var template = new GameObject("SvenDisplayTemplate");
            var display = PrefabAPI.InstantiateClone(template, "SvenDisplay", false);
            Object.DestroyImmediate(template);
            display.transform.localScale = Vector3.one * SvenBalance.ModelScale;

            var normal = AttachVisual(display.transform, SvenAssets.Load<GameObject>("SvenModel"), "Default");
            var arcana = AttachVisual(display.transform, SvenAssets.Load<GameObject>("SvenImmortalModel"), "Arcana");
            foreach (var visual in new[] { normal, arcana }) {
                visual.AddComponent<SvenDisplayMarker>();
                visual.AddComponent<SvenAnimator>();
            }
            arcana.SetActive(false);

            var characterModel = display.AddComponent<CharacterModel>();
            var infos = new System.Collections.Generic.List<CharacterModel.RendererInfo>();
            foreach (var renderer in display.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i]) materials[i] = materials[i].ConvertDefaultShaderToHopoo();
                renderer.sharedMaterials = materials;
                infos.Add(new CharacterModel.RendererInfo
                {
                    renderer = renderer,
                    defaultMaterial = renderer.sharedMaterial,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ignoreOverlays = false,
                    hideOnDeath = false,
                });
            }
            characterModel.baseRendererInfos = infos.ToArray();

            characterModel.mainSkinnedMeshRenderer = display.GetComponentInChildren<SkinnedMeshRenderer>(true);
            SetupDefaultSkin(display, characterModel);
            return display;
        }
    }
}
