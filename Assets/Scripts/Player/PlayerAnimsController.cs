using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Animations;

[RequireComponent(typeof(Animator))]
public class PlayerAnimsController : MonoBehaviour
{
    private Animator animator;
    public PlayerController pc;
    private PlayerHider hider; //balanced

    private float idleTimer = 3f;
    private float idleThreshold = 3f; // Time in seconds before switching to idle animation

    void Start()
    {
        animator = GetComponent<Animator>();
        hider = GetComponentInParent<PlayerHider>();
        //pc = GetComponent<PlayerController>();
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("Idle_Weapon") || stateInfo.IsName("Walking"))
            {
                animator.SetTrigger("IsAttacking");
            }
        }

        Vector2 inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Target is either moving (1) or idle (0)
        float targetSpeed = inputVector.magnitude > 0 ? 1f : 0f;

        // Smoothly interpolate current speed toward target
        float currentSpeed = animator.GetFloat("Speed");
        float speed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        animator.SetFloat("Speed", speed);

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        bool isMoving = moveX != 0 || moveY != 0;

        float verticalVelocity = pc.rb.linearVelocity.y;
        animator.SetFloat("VerticalVelocity", verticalVelocity);

        if (isMoving || Input.GetMouseButtonDown(0) || hider.IsHidden)
        {
            idleTimer = 0f; // Reset idle timer when moving
            animator.SetBool("IsIdleLong", true);
        }
        else
        {

            idleTimer += Time.deltaTime;


            if(idleTimer >= idleThreshold)
            {
                animator.SetBool("IsIdleLong", false);
            }
        }
    }
}
