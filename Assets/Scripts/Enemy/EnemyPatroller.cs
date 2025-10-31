using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
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
    [SerializeField] private float moveToPlayerSpeed = 5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float recoveryDelay = 1.0f;
    [SerializeField] private float recoveryTimer = 0f;
    [SerializeField] private float readyDelay = 0.25f;
    [SerializeField] private float readyTimer = 0f;

    [Header("AI Detection")]
    [SerializeField] private string playerLayerName = "Player";
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private string hiddenPlayerLayerName = "HiddenPlayer";
    [SerializeField] private LayerMask hiddenPlayerLayer;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private Vector2 detectionBoxSize = new Vector2(5f, 1f);
    [SerializeField] private Vector2 detectionOffset = new Vector2(5f, 0f);
    [SerializeField] private Vector2 attackBoxSize = new Vector2(2f, 1f);
    [SerializeField] private Vector2 attackOffset =  new Vector2(2f, 0f);


    public bool isFacingRight;

    [Header("Anim Names")]
    public Rigidbody2D rb;
    public Animator anim;
    public string attackTriggerName = "Attack";
    public string deathTriggerName = "Dead";
    public string hitTriggerName = "Hit";


    [HideInInspector] public PlayerController PC; //does this need to be public for child scripts? if not, make private
    [HideInInspector] public PlayerHealthController playerHealthController; //does this need to be public for child scripts? if not, make private

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
    public bool isAttacking = false;

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

        playerHealthController = playerObj.GetComponent<PlayerHealthController>();

        playerLayer = LayerMask.GetMask(playerLayerName);
        hiddenPlayerLayer = LayerMask.GetMask(hiddenPlayerLayerName);
        isFacingRight = GetFacingDirection();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState != EnemyState.Recovery && currentState != EnemyState.Attack && currentState != EnemyState.Feared)
        {            
            CheckForPlayer();
        }

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
                HandleReadyState(); //what is wait going to be?
                break;

            case EnemyState.Recovery:
                HandleRecoveryState();
                break;

            case EnemyState.Ready:
                HandleReadyState();
                break;

            case EnemyState.MoveToPlayer:
                MoveTowardPlayer();
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;
        }
    }

    private void HandleRecoveryState()
    {
        //TODO: add a generic timer class
        if (recoveryTimer <= 0)
        {            
            SetPreviousState(currentState);
            InitReadyTimer();
            currentState = EnemyState.Ready;

            return;
        }

        recoveryTimer -= Time.deltaTime;
            
    }

    private void FixedUpdate()
    {

        if (harvestHalo.enabled && readyForExecuteTimer > 0f)
        {
            readyForExecuteTimer -= Time.fixedDeltaTime;
        } else if (readyForExecuteTimer <= 0f)
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

        isFacingRight = GetFacingDirection();
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
            StartWait(); //Update to use wait state?

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

        // --- ROTATION LOGIC ---
        // Instant snap rotation:
        transform.rotation = (direction > 0) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 180f, 0f);

        // Smooth rotation: - broke due to unity security update, half the sprite goes missing when rotating
        //Quaternion targetRot = (direction > 0) ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 180f, 0f);
        //transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);     

        //do we want separate movement calls for patrol and other behaviour instead of an if/else chain?
        if(currentState == EnemyState.Patrol)
        {
            // Apply velocity
            rb.linearVelocity = new Vector2(moveSpeed * direction, rb.linearVelocity.y);

            // Handle vertical difference (jump to reach next point if needed)
            if (transform.position.y < patrolPoints[currentPoint].position.y - 0.5f && rb.linearVelocity.y < 0.1f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        } else if(currentState == EnemyState.MoveToPlayer)
        {           
            // Apply velocity            
            rb.linearVelocity = new Vector2(moveToPlayerSpeed * direction, rb.linearVelocity.y);

            // Handle vertical difference (jump to reach next point if needed)
            if (transform.position.y < PC.transform.position.y - 0.5f && rb.linearVelocity.y < 0.1f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            Debug.Log($"States: MoveToPlayer speed {moveToPlayerSpeed} at linear velocity {rb.linearVelocity.x}");
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
        
        if (currentState != EnemyState.Ready && currentState != EnemyState.Recovery && currentState != EnemyState.MoveToPlayer)
        {
            //Debug.Log($"States: IsPlayerInAttackRange - {IsPlayerInAttackRange().ToString()}");            
            //Debug.Log($"States: IsPlayerInDetectionRange - {IsPlayerInDetectionRange().ToString()}");
            //Debug.Log($"States: IsPlayerInLineOfSight - {IsPlayerInLineOfSight().ToString()}");
            Debug.Log("States: Check for player ....");
            if (IsPlayerInAttackRange() && IsPlayerInLineOfSight())
            {
                Debug.Log("States: Player in attack range and line of sight");
                previousState = currentState;
                currentState = EnemyState.Ready;
                InitReadyTimer();
            }
            else if (IsPlayerInDetectionRange() && IsPlayerInLineOfSight())
            {
                Debug.Log("States: Player in detection range and line of sight");
                previousState = currentState;
                currentState = EnemyState.Ready;
                
            }
            else if (currentState != EnemyState.Patrol)
            {
                Debug.Log("States: Return to Patrol state");
                //add a wait here first once wait state is implemented
                previousState = currentState;
                currentState = EnemyState.Patrol;
            }
        }

    }

    void InitReadyTimer()
    {
        readyTimer = readyDelay;
    }

    void InitRecoveryTimer()
    {
        recoveryTimer = recoveryDelay;
    }

    void HandleReadyState()
    {
        //Debug.Log("States: Enemy ready");
        readyTimer -= Time.deltaTime;
        if (readyTimer <= 0f)
        {
            float distance = Vector2.Distance(transform.position, PC.transform.position);

            if (distance <= detectionRange)
            {
                SetPreviousState(currentState);
                Debug.Log("States: ready - starting MoveToPlayer");
                currentState = EnemyState.MoveToPlayer;
            }                
            else
            {
                SetPreviousState(currentState);
                Debug.Log("States: ready - returning to patrol");
                currentState = EnemyState.Patrol;
            }
    
        }
    }

    void SetPreviousState(EnemyState oldState)
    {
        if (previousState != oldState)
        {
            previousState = oldState;
        }
    }

    void MoveTowardPlayer()
    {
        //this would move calculate Y into distance, not needed right now
        //float distance = Vector2.Distance(transform.position, PC.transform.position);

        float xDist = PC.transform.position.x - transform.position.x;
        //Debug.Log($"States: MoveToPlayer {xDist} to player - is this within detection range {detectionRange} or attack range {attackRange}");
        //Debug.Log($"States: MoveToPlayer {xDist} to player - is this within detection range {IsPlayerInDetectionRange()} or attack range {IsPlayerInAttackRange()}");

        Collider2D dTarget = BoxCastDetectionRange();
        float dxDistance = dTarget.transform.position.x - transform.position.x;

        Collider2D target = BoxCastAttackRange();
        float xDistance = target.transform.position.x - transform.position.x;

        //Debug.Log($"States:  detect player - is {dxDistance} close to {xDistance}");
        //Debug.Log($"States:  attack player - is {xDist} close to {xDistance}");

/*
        if (!dTarget)
        {
            SetPreviousState(currentState);
            Debug.Log("States: MoveToPlayer - switch to Patrol");
            currentState = EnemyState.Patrol; //should wait before returning to patrol but keeping it simple for the first iteration
            return;
        } else if (dTarget && !target)
        {
            Debug.Log("States: MoveToPlayer - charge!");
            //Vector2 direction = (PC.transform.position - transform.position).normalized;            
            MoveEnemy(xDistance);
        } else if (target)
        {
            Debug.Log("States: MoveToPlayer - switch to Attack");
                        
            SetPreviousState(currentState);
            rb.linearVelocity = Vector2.zero;
            currentState = EnemyState.Attack;
        }
*/
        if (target != null)
        {
            Debug.Log("States: MoveToPlayer - switch to Attack");

            SetPreviousState(currentState);
            rb.linearVelocity = Vector2.zero;
            currentState = EnemyState.Attack;

        }
        else if (dTarget != null)
        {
            Debug.Log("States: MoveToPlayer - charge!");
            //Vector2 direction = (PC.transform.position - transform.position).normalized;            
            MoveEnemy(xDistance);
        }
        else
        {
            SetPreviousState(currentState);
            Debug.Log("States: MoveToPlayer - switch to Patrol");
            currentState = EnemyState.Patrol; //should wait before returning to patrol but keeping it simple for the first iteration
            return;
        }


    }

    void HandleAttack()
    {
        //Debug.Log($"States: HandleAttack - isAttacking = {isAttacking.ToString()}");

        if (!isAttacking)
        {
            // Insert your attack animation or hitbox logic here
            Debug.Log("States: Enemy attacks!");
            // Optionally return to Idle or MoveToPlayer afterward
            anim.SetTrigger(attackTriggerName);
            isAttacking = true;
        }


    }

    public void OnAttackComplete(AnimationEvent animEvent)
    {
        Debug.Log("States: AttackComplete - Switch to Recovery");
        SetPreviousState(currentState);
        currentState = EnemyState.Recovery;
        InitRecoveryTimer();
        isAttacking = false;
    }

    private bool GetFacingDirection()
    {
        return transform.rotation.y == 0f;        
    }

    private Vector2 GetAttackBoxRange()
    {
        Vector2 facingSign = isFacingRight ? Vector2.right : Vector2.left;

        return (Vector2)transform.position + new Vector2(attackOffset.x * facingSign.x, attackOffset.y);    
    }

    private Vector2 GetDetectionBoxRange()
    {
        Vector2 facingSign = isFacingRight ? Vector2.right : Vector2.left;
        Vector2 boxOffset = GetDetectionBoxOffset();        

        return (Vector2)transform.position + new Vector2(boxOffset.x * facingSign.x, boxOffset.y) + new Vector2(detectionBoxSize.x * facingSign.x, detectionOffset.y);
    }

    private Vector2 GetAttackBoxOffset()
    {
        Vector2 facingSign = isFacingRight ? Vector2.right : Vector2.left;

        return (Vector2)transform.position + new Vector2(attackOffset.x * facingSign.x, attackOffset.y);
    }

    private Vector2 GetDetectionBoxOffset()
    {
        Vector2 facingSign = isFacingRight ? Vector2.right : Vector2.left;
        Debug.Log($"States: DetectionBoxRange: {(Vector2)transform.position + new Vector2(detectionOffset.x * facingSign.x, detectionOffset.y)}");
        return (Vector2)transform.position + new Vector2(detectionOffset.x * facingSign.x, detectionOffset.y);
    }

    
    private bool IsPlayerInDetectionRange()
    {
        bool facingRight = transform.rotation.y == 0f; // or use localScale.x > 0
        Vector2 dOffset = detectionOffset;
        if (!facingRight)
            dOffset.x *= -1;

        Vector2 boxCenter = (Vector2)transform.position + dOffset;

        return Physics2D.OverlapBox(boxCenter, detectionBoxSize, 0, playerLayer);
    }

    private bool IsPlayerInAttackRange()
    {
        bool facingRight = transform.rotation.y == 0f; // or use localScale.x > 0
        Vector2 aOffset = attackOffset;
        if (!facingRight)
            aOffset.x *= -1;

        Vector2 boxCenter = (Vector2)transform.position + aOffset;

        return Physics2D.OverlapBox(boxCenter, attackBoxSize, 0, playerLayer);
    }

    //TODO: make generic box cast function that accepts any box and a layer mask, move to its own class
    private Collider2D BoxCastDetectionRange()
    {
        bool facingRight = transform.rotation.y == 0f; // or use localScale.x > 0
        Vector2 dOffset = detectionOffset;
        if (!facingRight)
            dOffset.x *= -1;

        Vector2 boxCenter = (Vector2)transform.position + dOffset;

        return Physics2D.OverlapBox(boxCenter, detectionBoxSize, 0, playerLayer);
    }

    private Collider2D BoxCastAttackRange()
    {
        bool facingRight = transform.rotation.y == 0f; // or use localScale.x > 0
        Vector2 offset = attackOffset;
        if (!facingRight)
            offset.x *= -1;

        Vector2 boxCenter = (Vector2)transform.position + offset;

        return Physics2D.OverlapBox(boxCenter, attackBoxSize, 0, playerLayer);
    }

    private bool IsPlayerInLineOfSight()
    {
        bool checkSight = Physics2D.Linecast(transform.position, PC.transform.position, playerLayer);
        Debug.Log($"Player in line of sight? {checkSight.ToString()}");
        return (Physics2D.Linecast(transform.position, PC.transform.position, 8)); //TODO: should not be hard coded to default layer - figure out what is best here

    }

    void OnDrawGizmosSelected()
    {

        bool facingRight = transform.rotation.y == 0f;

        Gizmos.color = Color.red;
        Vector2 aOffset = attackOffset;
        if (!facingRight)
            aOffset.x *= -1;

        Vector2 aBoxCenter = (Vector2)transform.position + aOffset;
        Gizmos.DrawWireCube(aBoxCenter, attackBoxSize);

        Gizmos.color = Color.green;
        Vector2 dOffset = detectionOffset;
        if (!facingRight)
            dOffset.x *= -1;

        Vector2 dBoxCenter = (Vector2)transform.position + dOffset;
        Gizmos.DrawWireCube(dBoxCenter, detectionBoxSize);
    }

    internal void OnAttackSwing(AnimationEvent animEvent)
    {
        playerHealthController.DamagePlayer(attackDamage);
    }
}