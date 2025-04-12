using UnityEngine;

public class Autoloopanimation : MonoBehaviour
{

    void Start()
    {
        // Get the Legacy Animation component on this GameObject
        Animation anim = GetComponent<Animation>();

        if (anim == null)
        {
            Debug.LogError("No Animation component found on " + gameObject.name);
            return;
        }

        // Set all animation states to loop
        foreach (AnimationState state in anim)
        {
            state.wrapMode = WrapMode.Loop;
        }

        // Play the default animation (the one set as default in the Animation component)
        anim.Play();
    }
}
