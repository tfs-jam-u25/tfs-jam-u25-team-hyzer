using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatroller : MonoBehaviour
{
    public Transform[] patrolPoints;
    private int currentPoint;

    public float moveSpeed, waitAtPoints;
    public float distanceThreshold = 0.2f;
    private float waitCounter;

    public float jumpForce;

    public bool isReadyForHarvest = false;
    public bool isReadyForExecute = false;
    public SpriteRenderer harvestHalo;
    public float debugHaloHeight = 0.2f;
    public float debugHaloSize = 0.2f;
    public Color debugHaloColour = Color.red;
    public Color haloExecuteColour = Color.red;
    public Color haloDefaultColour;

    public Rigidbody2D rb;
    public Animator anim;

    public PlayerController PC;

    public enum EnemyState { Feared, Patrol, Wait, Attack }
    public EnemyState currentState = EnemyState.Wait;
    public EnemyState previousState = EnemyState.Wait;

    private Knockback knockback;

    // Start is called before the first frame update
    void Start()
    {
        waitCounter = waitAtPoints;
        haloDefaultColour = harvestHalo.color;

        foreach (Transform patrolPoint in patrolPoints)
        {
            patrolPoint.SetParent(null);
        }

        if(isReadyForHarvest)
        {
            ActivateHarvestHalo();
        }

        knockback = gameObject.AddComponent<Knockback>();

    }

    // Update is called once per frame
    void Update()
    {
        if (patrolPoints.Length == 0)
        {
            throw new System.Exception("Enemy instance has no Patrol Points assigned");
        }

        // Check if enemy is far enough from target point
        if (Mathf.Abs(transform.position.x - patrolPoints[currentPoint].position.x) > distanceThreshold)
        {
            float direction = Mathf.Sign(patrolPoints[currentPoint].position.x - transform.position.x);

            // Apply velocity
            rb.linearVelocity = new Vector2(moveSpeed * direction, rb.linearVelocity.y);

            // --- ROTATION LOGIC ---
            // Instant snap rotation:
            // transform.rotation = (direction > 0) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 180f, 0f);

            // Smooth rotation:
            Quaternion targetRot = (direction > 0)
                ? Quaternion.Euler(0f, 0f, 0f)
                : Quaternion.Euler(0f, 180f, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);

            // Handle vertical difference (jump to reach next point if needed)
            if (transform.position.y < patrolPoints[currentPoint].position.y - 0.5f && rb.linearVelocity.y < 0.1f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }
        else
        {
            // Stop horizontal movement
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Wait before moving to next point
            waitCounter -= Time.deltaTime;
            if (waitCounter < 0)
            {
                waitCounter = waitAtPoints;
                currentPoint++;

                if (currentPoint >= patrolPoints.Length)
                {
                    currentPoint = 0;
                }
            }
        }

        // Update animator with movement speed
        anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GameManager.Instance.PlayerInstance.IsHidden())
        {
            ActivateExecuteHalo();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (PC.GetPlayerState() == PlayerController.PlayerState.Rage)
            {
                if (currentState != EnemyState.Feared)
                {
                    currentState = EnemyState.Feared;
                    Debug.Log("Runaway my man");
                }

            } 
            else if (currentState == EnemyState.Feared)
            {
                currentState = previousState;
                //TODO: Set a fear cooldown Timer, add a countdown
            }

        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (PC.GetPlayerState() == PlayerController.PlayerState.Rage)
            {
                if (currentState == EnemyState.Feared)
                {
                    currentState = previousState;
                    Debug.Log("Runaway my man");
                }

            }
        }

        if (GameManager.Instance.PlayerInstance.IsHidden())
        {
            DeactivateExecuteHalo();
        }
    }

    public void ActivateHarvestHalo()
    {
        harvestHalo.enabled = true;
    }

    public void ActivateExecuteHalo()
    {
        harvestHalo.color = haloExecuteColour;
    }

    public void DeactivateExecuteHalo()
    {
        harvestHalo.color = haloDefaultColour;
    }
    private void OnDrawGizmos()
    {
        if(isReadyForHarvest)
        {
            Gizmos.color = debugHaloColour;
            Vector3 pos = transform.position + Vector3.up * debugHaloHeight;
            Gizmos.DrawSphere(pos, debugHaloSize);
        }
    }

    public void Knockback(ForceMode2D forceType, Vector2 knockbackForce)
    {
        knockback.apply(rb, forceType, knockbackForce, 0);        
    }
}
