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
    public float readyForExecuteTimer = 0.0f;
    public float readyForExecuteDuration = 1.0f;
    public SpriteRenderer harvestHalo;
    public float debugHaloHeight = 0.2f;
    public float debugHaloSize = 0.2f;
    public Color debugHaloColour = Color.red;
    public Color haloExecuteColour = Color.red;
    public Color haloDefaultColour;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float recoveryDelay = 1.0f;
    [SerializeField] private float recoveryTimer = 0f;
    [SerializeField] private float readyDelay = 1.0f;
    [SerializeField] private float readyTimer = 0f;

    [Header("AI Detection")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask hiddenPlayerLayer;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(4f, 1f);
    [SerializeField] private Vector2 attackOffset =  new Vector2(2f, 0f);

    [Header("AI behaviour")]

    

    public Rigidbody2D rb;
    public Animator anim;

    [HideInInspector] public PlayerController PC; //does this need to be public for child scripts? if not, make private

    [Header("Hit Reaction Settings")]
    public float stunDuration = 0.5f;
    public GameObject bloodEffectPrefab;

    [SerializeField] private float weight = 1f;
    private bool isStunned = false;
    private bool isDead = false;
    private float stunTimer = 0f;

    public enum EnemyState { Feared, Patrol, Wait, Attack, Recovery, Ready, MoveToPlayer }
    public EnemyState currentState = EnemyState.Patrol;
    public EnemyState previousState = EnemyState.Patrol;

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

        if (isReadyForHarvest)
        {
            ActivateHarvestHalo();
        }

        knockback = gameObject.AddComponent<Knockback>();

        // Try to find the Player by tag first
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PC = playerObj.GetComponent<PlayerController>();
            if (PC == null)
            {
                Debug.LogWarning($"{name}: Found Player tagged object but no PlayerController component on it!");
            }
        }
        else
        {
            // fallback, just in case tag is missing or player not spawned yet
            PC = FindFirstObjectByType<PlayerController>();
            if (PC == null)
            {
                Debug.LogWarning($"{name}: Could not find any Player tagged object or PlayerController in scene!");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckForPlayer();

        switch (currentState)
        {
            //public enum EnemyState { Feared, Patrol, Wait, Attack, Recovery, Ready, MoveToPlayer }
            case EnemyState.Feared:
                //TODO: handle state
                break;

            case EnemyState.Patrol:
                PatrolLogic();
                break;

            case EnemyState.Wait:
                HandleReadyState();
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;

            case EnemyState.Recovery:
                MoveTowardPlayer();
                break;

            case EnemyState.Ready:
                MoveTowardPlayer();
                break;

            case EnemyState.MoveToPlayer:
                HandleAttack();
                break;
        }
    }

    private void FixedUpdate()
    {

        if (harvestHalo.enabled)
        {
            readyForExecuteTimer -= Time.fixedDeltaTime;
        }
        
        if (readyForExecuteTimer <= 0f)
        {
            DeactivateExecuteHalo();
        }

        // Update stun timer
        if (isStunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
            }
        }

        // Cancel movement if stunned or dead
        if (isDead)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            anim.SetFloat("speed", 0);
            return;
        }

        if (isStunned)
        {
            anim.SetFloat("speed", 0);
            return; // but don't zero velocity
        }

        //Attempting to handl via states now
        //PatrolLogic();
    }

    private void PatrolLogic()
    {
        if (patrolPoints.Length == 0) return;

        // Check if enemy is far enough from target point
        if (Mathf.Abs(transform.position.x - patrolPoints[currentPoint].position.x) > distanceThreshold)
        {
            float direction = Mathf.Sign(patrolPoints[currentPoint].position.x - transform.position.x);

            MoveEnemy(direction);
        }
        else
        {            
            StartWait();

            if (waitCounter <= 0)
            {
                waitCounter = waitAtPoints;
                currentPoint++;
                if (currentPoint >= patrolPoints.Length) currentPoint = 0;
            }
        }
    }

    private void MoveEnemy(float direction)
    {
        // Apply velocity
        rb.linearVelocity = new Vector2(moveSpeed * direction, rb.linearVelocity.y);

        // --- ROTATION LOGIC ---
        // Instant snap rotation:
        transform.rotation = (direction > 0) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 180f, 0f);

        // Smooth rotation: - broke due to unity security update, half the sprite goes missing when rotating
        //Quaternion targetRot = (direction > 0) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 180f, 0f);
        //transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);     

        // Handle vertical difference (jump to reach next point if needed)
        if (transform.position.y < patrolPoints[currentPoint].position.y - 0.5f && rb.linearVelocity.y < 0.1f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Set walking animation
        anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
    }

    private void StopEnemy()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Set idle animation (speed = 0)
        anim.SetFloat("speed", 0f);

    }

    private void StartWait() // wait for patrol
    {
        StopEnemy();
        previousState = currentState;
        currentState = EnemyState.Wait;

        waitCounter -= Time.deltaTime;
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
            //DeactivateExecuteHalo();
            readyForExecuteTimer = readyForExecuteDuration;
        }
    }

    public void ActivateHarvestHalo()
    {
        harvestHalo.enabled = true;
    }

    public void DeactivateHarvestHalo() //TODO: should make this a toggle but in a rush today
    {
        harvestHalo.enabled = false;
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
        if (isReadyForHarvest)
        {
            Gizmos.color = debugHaloColour;
            Vector3 pos = transform.position + Vector3.up * debugHaloHeight;
            Gizmos.DrawSphere(pos, debugHaloSize);
        }
    }

    // Public method to apply knockback (matches your Knockback component signature)
    public void ApplyKnockback(ForceMode2D forceType, Vector2 knockbackForce)
    {
        if (knockback != null && rb != null)
        {
            knockback.Apply(rb, knockbackForce, forceType);
        }
    }

    // Called when enemy is hit
    public void TakeHit(Vector2 knockbackForce, ForceMode2D forceType = ForceMode2D.Impulse)
    {
        if (isDead || isStunned) return;

        isStunned = true;
        stunTimer = stunDuration;

        // Spawn blood effect with direction
        if (bloodEffectPrefab)
        {
            // Determine direction based on knockback force
            float direction = Mathf.Sign(knockbackForce.x);

            GameObject blood = Instantiate(bloodEffectPrefab, transform.position, Quaternion.identity);

            // Flip the blood effect if hit from the right (so blood flies left)
            if (direction < 0)
            {
                blood.transform.localScale = new Vector3(-1, 1, 1);
            }

            // Optional: If your blood effect uses a particle system or rigidbody, you can also apply velocity
            Rigidbody2D bloodRb = blood.GetComponent<Rigidbody2D>();
            if (bloodRb != null)
            {
                bloodRb.linearVelocity = new Vector2(direction * 3f, 2f); // Adjust speed as needed
            }

            // Optional: If using particle system, rotate it
            ParticleSystem ps = blood.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startRotation = direction > 0 ? 0 : Mathf.PI; // 180 degrees if hit from right
            }
        }

        // Apply knockback using the Knockback component
        ApplyKnockback(forceType, knockbackForce);

        // Play hit animation
        anim.SetTrigger("Hit");
    }

    // Method to apply pushback based on weight
    public virtual void ApplyPushbackImpulse()
    {
        if (rb == null) return;

        // Pushback direction is away from the player (hit direction)
        Vector2 pushDirection = (transform.position - PC.transform.position).normalized;
        pushDirection.y = 0;

        // Apply force based on weight
        float pushStrength = 10.0f / weight;
        rb.AddForce(pushDirection * pushStrength, ForceMode2D.Impulse);

        // Debug log to track pushback
        Debug.Log($"Pushback Applied: Direction {pushDirection}, Strength {pushStrength}");
    }


    // Called on death
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Immediately stop all movement
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // freeze physics immediately

        // Stop stun so it doesn't interfere
        isStunned = false;

        // Set animator parameters
        anim.SetFloat("speed", 0f); // Make sure walk animation stops
        anim.ResetTrigger("Hit"); // Clear any pending hit triggers

        DeactivateHarvestHalo();

        // Trigger death animation
        anim.SetTrigger("Dead");

        // Disable collider so player doesn't collide during death
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    //AI handling
    //can and should probably be in another script as a re-usable component but dropping here for now

    void CheckForPlayer()
    {
        //may wish to use OverlapBox for initial detection while using BoxCast once player is detected
        bool playerDetected = Physics2D.OverlapBox(
            (Vector2)transform.position + attackOffset * Mathf.Sign(transform.localScale.x),
            attackBoxSize,
            0,
            playerLayer);


        if (playerDetected)
        {
            if (Physics2D.Linecast(transform.position, PC.transform.position, 0)) //TODO: should not be hard coded to default layer - figure out what is best here
            {
                currentState = EnemyState.Patrol;               
            } else
            {
                currentState = EnemyState.Ready;
                readyTimer = readyDelay;
            }
                
        }
    }

    void HandleReadyState()
    {
        readyTimer -= Time.deltaTime;
        if (readyTimer <= 0f)
        {
            float distance = Vector2.Distance(transform.position, PC.transform.position);

            if (distance <= attackRange)
                currentState = EnemyState.Attack;
            else if (distance <= detectionRange)
                currentState = EnemyState.MoveToPlayer;
            else
                currentState = EnemyState.Patrol;
        }
    }
    void MoveTowardPlayer()
    {
        //this would move calculate Y into distance, not needed right now
        //float distance = Vector2.Distance(transform.position, PC.transform.position);

        float xDistance = Mathf.Sign(PC.transform.position.x - transform.position.x);
        if (xDistance > detectionRange)
        {
            currentState = EnemyState.Patrol; //should wait before returning to patrol but keeping it simple for the first iteration
            return;
        }

        //Vector2 direction = (PC.transform.position - transform.position).normalized;
        MoveEnemy(xDistance);        

        if (xDistance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;
            currentState = EnemyState.Attack;
        }
    }

    void HandleAttack()
    {
        // Insert your attack animation or hitbox logic here
        Debug.Log("Enemy attacks!");
        // Optionally return to Idle or MoveToPlayer afterward
        currentState = EnemyState.Ready;
        readyTimer = readyDelay;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 offset = new Vector3(attackOffset.x * Mathf.Sign(transform.localScale.x), attackOffset.y, 0);
        Gizmos.DrawWireCube(transform.position + offset, attackBoxSize);
    }
}