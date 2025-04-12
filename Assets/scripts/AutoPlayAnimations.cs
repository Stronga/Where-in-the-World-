using UnityEngine;
using UnityEditor.Animations; // Required for creating Animator Controllers programmatically

public class AutoPlayAnimations : MonoBehaviour
{
    [SerializeField] private Transform target; // The parent object (Planet) containing child objects with animations

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Please assign a target to the AutoPlayAnimations script!");
            return;
        }

        SetupAndPlayChildAnimations();
    }

    private void SetupAndPlayChildAnimations()
    {
        // Get all child objects
        Transform[] children = target.GetComponentsInChildren<Transform>();

        // Get the single "Scene" animation clip
        AnimationClip sceneClip = null;
        AnimationClip[] clips = target.GetComponent<Animator>().runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "Scene")
            {
                sceneClip = clip;
                break;
            }
        }

        if (sceneClip == null)
        {
            Debug.LogError("Could not find the 'Scene' animation clip!");
            return;
        }

        foreach (Transform child in children)
        {
            // Skip the parent object itself
            if (child == target) continue;

            string childName = child.name;

            // Add an Animator component if it doesn't exist
            Animator animator = child.gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = child.gameObject.AddComponent<Animator>();
            }

            // Create a new Animator Controller
            AnimatorController controller = new AnimatorController();
            controller.name = $"{childName}_Animator";
            controller.AddLayer("Base Layer");

            // Add the "Scene" animation clip as a state
            AnimatorState state = controller.layers[0].stateMachine.AddState(sceneClip.name);
            state.motion = sceneClip;

            // Create and apply an Animation Mask for this child
            AnimatorControllerLayer layer = controller.layers[0];
            layer.avatarMask = CreateAvatarMaskForChild(child);
            controller.layers[0] = layer;

            // Assign the controller to the Animator
            animator.runtimeAnimatorController = controller;

            // Play the animation
            animator.Play(state.nameHash, 0, 0f);
        }
    }

    private AvatarMask CreateAvatarMaskForChild(Transform child)
    {
        AvatarMask mask = new AvatarMask();
        mask.AddTransformPath(child);
        return mask;
    }
}