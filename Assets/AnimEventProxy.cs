using UnityEngine;

public class AnimEventProxy : MonoBehaviour
{
    private PlayerController pc;
    private EnemyPatroller ep;

    //TODO: make this genericly typed between player and enemy controllers, consider use of an Interface
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pc = gameObject.GetComponentInParent<PlayerController>(); //or ref from Game Manager
        ep = gameObject.GetComponentInParent<EnemyPatroller>(); 
    }

    public void AnimationApplyKnockback ()
    {
        Debug.Log("animeventproxy - cultist knockback impulse applied here");
        ep.ApplyPushbackImpulse();
    }

    public void AnimationAttackTrigger(AnimationEvent animEvent)
    {
        Debug.Log("animeventproxy - play the players attack animation here");
        pc.AnimationAttackTrigger(animEvent);
    }

    public void AnimationAttackSwingTrigger(AnimationEvent animEvent)
    {
        Debug.Log("animeventproxy - sync any effects that start at the end of the players swing here");
        pc.AnimationAttackSwingTrigger(animEvent);
    }

    public void AnimationAttackCompleteTrigger(AnimationEvent animEvent)
    {
        Debug.Log("animeventproxy - play any effects that start at the end of the players attack animation here");
        pc.AnimationAttackCompleteTrigger(animEvent);
    }

    public void EnemyAttackCompleteTrigger(AnimationEvent animEvent)
    {
        Debug.Log("animeventproxy - play any effects that start at the end of the enemies attack animation here");
        ep.OnAttackComplete(animEvent);

    }
}
