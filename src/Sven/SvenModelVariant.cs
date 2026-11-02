using RoR2;
using UnityEngine;

namespace SvenMod
{
    public sealed class SvenModelVariant : MonoBehaviour
    {
        public GameObject normal, arcana;
        public Transform muzzle;
        private ModelSkinController controller;
        private void Awake()
        {
            controller = GetComponent<ModelSkinController>();
            if (controller) controller.onSkinApplied += Apply;
        }
        private void Start()
        {
            if (controller && controller.currentSkinIndex >= 0) Apply(controller.currentSkinIndex);
        }
        private void OnDestroy()
        {
            if (controller) controller.onSkinApplied -= Apply;
        }
        public void Apply(int index)
        {
            bool useArcana = index == 1 && arcana;
            normal.SetActive(!useArcana);
            if (arcana) arcana.SetActive(useArcana);
            var visual = useArcana ? arcana : normal;
            var animator = visual.GetComponent<Animator>();
            var character = GetComponent<CharacterModel>();
            if (character)
            {
                character.mainSkinnedMeshRenderer = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                var body = character.body;
                if (body && body.modelLocator)
                {
                    body.modelLocator.forcedAnimatorAtStart = animator;
                    body.modelLocator.SetAnimator(animator);
                    if (body.characterDirection) body.characterDirection.modelAnimator = animator;
                }
            }
            if (muzzle)
                foreach (var bone in visual.GetComponentsInChildren<Transform>(true))
                    if (bone.name == (useArcana ? "sword_1" : "sword_1")) { muzzle.SetParent(bone, false); muzzle.localPosition = Vector3.zero; break; }
        }
    }
}
