using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    // Called from the animation event at the end of the animation
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
