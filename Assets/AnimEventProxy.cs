using UnityEngine;

public class AnimEventProxy : MonoBehaviour
{
    private PlayerController pc;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pc = gameObject.GetComponentInParent<PlayerController>(); //or ref from Game Manager
    }

    public void AnimationAttackTrigger(AnimationEvent animEvent)
    {
        Debug.Log("animeventproxy - play the players attack animation here");
        pc.AnimationAttackTrigger(animEvent);
    }
}
