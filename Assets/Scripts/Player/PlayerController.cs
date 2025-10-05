using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHider))]
[RequireComponent(typeof(PlayerAbilityTracker))] 
public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Regular, Disguise, Rage }
    public enum DisguiseMode { Normal, Ultra }


    [Header("State")]
    public PlayerState currentState = PlayerState.Regular;
    public DisguiseMode disguiseMode = DisguiseMode.Normal;
    private PlayerHider hider;
    private bool isFacingLeft;

    [Header("References")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    [Header("Movement Settings")]
    public float baseMoveSpeed = 8;
    public float currentMoveSpeed = 8;
    public float disguiseSpeed = 2.5f;
    public float rageSpeed = 10f;

    [Header("Jump Settings")]
    public float jumpForce;
    public float rageJumpForce;// = 11f;

    [Header("Rage Settings")]
    public float rageDuration = 5f;
    private float rageTimer;
    public int maxRage = 3;
    public int rageBar = 0;

    [Header("Disguise Settings")]
    public float disguiseDuration = 10f; // duration of normal disguise
    private float disguiseTimer;
    [SerializeField] private GameObject normalCharacter;
    [SerializeField] private GameObject disguisedCharacter;
    [SerializeField] private GameObject smokeBomb;

    private float xInput;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool canDoubleJump; 
    private float groundCheckSize = 0.2f;

    [Header("Enemy Detection")]
    //for rage detection(?)
    public float enemyCheckRadius = 1.5f;
    public string enemyTag = "Enemy";

    [Header("Execution Sensor")]
    private Vector2 sensorPos; //TODO: rename me to executionSensorPos to reduce confusion with newly added attackSensorPos
    Vector2 sensorSize = new Vector2(1f, 1f); // width/height of sensor area    
    private HashSet<EnemyPatroller> executableEnemiesInRange = new HashSet<EnemyPatroller>();
    public LayerMask enemyLayer;

    [Header("Attack Settings")]
    public Collider2D attackTrigger; //assigned in inspector, should be stored in prefab
    private Vector2 attackSensorPos;
    private Vector2 attackSensorOffset;
    public Vector2 attackSensorSize = new Vector2(5f, 1f);
    [Tooltip("1-1 ratio of damage that will be inflected upon enenmy health controller")]
    public int attackDamage = 1;
    public float attackTimer = 0.25f;
    private float nextAttackCountdown = 0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fire1Sound; // assign in inspector

    public Animator anim;

    public BulletController shotToFire;
    public Transform shotOrigin;

    public float dashSpeed; //TODO: dash should be an ability added via builder pattern, so should jump and double jump and shoot TBH, maybe even base movement
    public float dashTime;
    private float dashCounter;
    public float waitAfterDashing;
    private float dashRechargeCounter;

    public SpriteRenderer sr;
    public SpriteRenderer afterImageSR;
    public float afterImageLifetime;
    public float timeBetweenAfterImages;
    private float afterImageCounter;
    public Color afterImageColor;

    public GameObject standing;
    public GameObject ball;
    public float waitToBall;
    private float ballCounter;
    public float ballThreshold = 0.9f;
    public Animator ballAnim;

    public Transform bombPoint;
    public GameObject bomb;

    private PlayerAbilityTracker abilities;

    public bool canMove;

    public int nextRoomDoorID;

    // Start is called before the first frame update
    void Start()
    {
        abilities = GetComponent<PlayerAbilityTracker>();
        hider = GetComponent<PlayerHider>();
        canMove = true;
        sensorPos = (Vector2)transform.position;
        //attackSensorPos = (Vector2)transform.position + attackSensorOffset + Vector2.right;
        //attackSensorPos = (Vector2)transform.position + (isFacingLeft ? Vector2.left : Vector2.right) * attackSensorOffset.x;
        //attackSensorPos = (Vector2)transform.position + Vector2.right * Mathf.Sign(transform.localScale.x);

    }

    private void FixedUpdate()
    {
        HashSet<EnemyPatroller> currentFrameEnemies = new HashSet<EnemyPatroller>();

        if (hider.IsHidden)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(sensorPos, sensorSize, 0f, enemyLayer); //multiple targets
            currentFrameEnemies = new HashSet<EnemyPatroller>();
            foreach (var hit in hits)
            {
                EnemyPatroller enemy = hit.GetComponent<EnemyPatroller>();
                if (enemy != null)
                {
                    currentFrameEnemies.Add(enemy);

                    // New enemy entered this frame
                    if (!executableEnemiesInRange.Contains(enemy))
                    {
                        Debug.Log("Enemy ENTERED: " + enemy.name);
                        MarkEnemyForExecution(enemy);
                    }
                }

                /*Collider2D hit = Physics2D.OverlapBox(sensorPos, sensorSize, 0f, LayerMask.GetMask("Enemy")); 
                if (hit != null)
                {
                    Debug.Log("Enemy in front detected!");
                    //change enemy execution state
                    EnemyPatroller enemy = hit.GetComponent<EnemyPatroller>();
                    MarkEnemyForExecution(enemy);
                }*/
            }

            /* this isn't working so let's leave them marked
            // Check for enemies that exited
            foreach (var enemy in executableEnemiesInRange)
            {
                if (!currentFrameEnemies.Contains(enemy))
                {
                    Debug.Log("Enemy EXITED: " + enemy.name);
                    UnMarkEnemyForExecution(enemy);
                }
            }
            */
        } else if (!hider.IsHidden && executableEnemiesInRange.Count > 0)
        {
            /* this isn't working so let's leave them marked
            // Remove effect from enemies that exited
            foreach (var enemy in executableEnemiesInRange)
            {
                if (!currentFrameEnemies.Contains(enemy))
                {
                    UnMarkEnemyForExecution(enemy);
                }
            }
            */
            // Update tracking set
            executableEnemiesInRange = currentFrameEnemies;
        }

    }

    // Update is called once per frame
    void Update()
    {
        nextAttackCountdown -= Time.deltaTime;

        if (canMove)
        {

            if (dashRechargeCounter > 0)
            {
                dashRechargeCounter -= Time.deltaTime;
            }
            else
            {
                if (Input.GetButtonDown("Fire2") && standing.activeSelf && abilities.canDash)
                {
                    dashCounter = dashTime;
                    ShowAfterImage();
                }
            }


            if (dashCounter > 0)
            {
                dashCounter = dashCounter - Time.deltaTime;
                rb.linearVelocity = new Vector2(dashSpeed * transform.localScale.x, rb.linearVelocity.y);
                afterImageCounter -= Time.deltaTime;

                if (afterImageCounter <= 0)
                {
                    ShowAfterImage();
                }

                dashRechargeCounter = waitAfterDashing;
            }
            else
            {

                xInput = Input.GetAxisRaw("Horizontal");

                switch (currentState)
                {
                    case PlayerState.Regular: currentMoveSpeed = baseMoveSpeed; break;
                    case PlayerState.Disguise:
                        currentMoveSpeed = (disguiseMode == DisguiseMode.Ultra) ? 0f : disguiseSpeed;
                        break;
                    case PlayerState.Rage: currentMoveSpeed = rageSpeed; break;
                }

                rb.linearVelocity = new Vector2(xInput * currentMoveSpeed, rb.linearVelocity.y);

                //handle direction change
                if (rb.linearVelocity.x < 0)
                {

                    transform.localScale = new Vector3(-1f, 1f, 1f);
                    isFacingLeft = true;
                }
                else if (rb.linearVelocity.x > 0)
                {
                    transform.localScale = Vector3.one;
                    isFacingLeft = false;
                }
            }

            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckSize, groundLayer);


            if (Input.GetButtonDown("Jump") && (isGrounded || (canDoubleJump && abilities.canDoubleJump)))
            {
                if (isGrounded)
                {
                    canDoubleJump = true;
                }
                else
                {
                    canDoubleJump = false;
                    anim.SetTrigger("doubleJump");
                }

                float jump = (currentState == PlayerState.Rage) ? rageJumpForce : jumpForce;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            //shooting
            //if (Input.GetButtonDown("Fire1"))
            //{
            //    if (standing.activeSelf)
            //    {
            //        Instantiate(shotToFire, shotOrigin.position, shotOrigin.rotation).moveDir = new Vector2(transform.localScale.x, 0);
            //        anim.SetTrigger("shotFired");
            //    }
            //    else if (ball.activeSelf && abilities.canDropBomb)
            //    {
            //        Instantiate(bomb, bombPoint.position, bombPoint.rotation);
            //    }
            //}

            if (Input.GetButtonDown("Fire1") && nextAttackCountdown <= 0)
            {

                if (fire1Sound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(fire1Sound);
                }

                //add attack damage delay so we can't just spam attack
                if (standing.activeSelf)
                {
                    Debug.Log("Player attacks");
                    Vector2 direction = Vector2.right * Mathf.Sign(transform.localScale.x);
                    attackSensorPos = (Vector2)transform.position + direction + new Vector2(0, attackSensorOffset.y);
                    //Instantiate(shotToFire, shotOrigin.position, shotOrigin.rotation).moveDir = new Vector2(transform.localScale.x, 0);
                    Collider2D hit = Physics2D.OverlapBox(attackSensorPos, attackSensorSize, 0f, LayerMask.GetMask("Enemy"));
                    if (hit != null)
                    {
                        Debug.Log("Player attacks an enemy");
                        EnemyPatroller enemy = hit.GetComponent<EnemyPatroller>();
                        EnemyHealthController enemyHealth = enemy.GetComponent<EnemyHealthController>();
                        if (enemy.isReadyForExecute)
                        {
                            enemyHealth.ExecuteEnemy();
                            Debug.Log("player executes enemy in silence");
                            
                            GameManager.Instance.harvestScore.AddExecution(ExecutionScore.Type.silence);
                        } else
                        {
                            enemy.GetComponent<EnemyHealthController>().DamageEnemy(attackDamage);
                        }
                            
                    }

                    Collider2D hitBoss = Physics2D.OverlapBox(attackSensorPos, attackSensorSize, 0f, LayerMask.GetMask("Boss"));
                    if (hitBoss != null)
                    {
                        Debug.Log("Player attacks a boss");
                        BossHealthController enemy = hitBoss.GetComponentInParent<BossHealthController>();
                        enemy.TakeDamage(attackDamage);
                        /*
                        if (enemy.isReadyForExecute)
                        {
                            enemyHealth.ExecuteEnemy();
                            Debug.Log("player executes enemy in silence");

                            GameManager.Instance.harvestScore.AddExecution(ExecutionScore.Type.silence);
                        }
                        else
                        {
                            enemy.GetComponent<EnemyHealthController>().DamageEnemy(attackDamage);
                        }
                        */
                    }

                    nextAttackCountdown = attackTimer;
                }
            }

            // Toggle disguise with Q
            if (Input.GetKeyDown(KeyCode.Q) && !hider.IsHidden && currentState != PlayerState.Rage)
            {
                if (currentState == PlayerState.Regular) EnterDisguise();
                else if (currentState == PlayerState.Disguise) ExitDisguise();
            }

            // Rage fill in Regular mode by pressing E near enemy
            if (currentState == PlayerState.Regular && Input.GetKeyDown(KeyCode.R) && !hider.IsHidden)
            {
                Debug.Log("player: try to build rage");
                if (IsNearEnemy())
                {
                    rageBar = Mathf.Min(rageBar + 1, maxRage);
                    Debug.Log("Rage Bar: " + rageBar + "/" + maxRage);
                }
            }
            /*
            // Toggle Ultra Disguise inside disguise
            if (currentState == PlayerState.Disguise && Input.GetKeyDown(KeyCode.E))
            {
                if (disguiseMode == DisguiseMode.Normal) EnterUltraDisguise();
                else if (disguiseMode == DisguiseMode.Ultra) ExitUltraDisguise();
            }
            */
            // Activate rage when bar is full
            if (currentState == PlayerState.Regular && Input.GetKeyDown(KeyCode.T) && !hider.IsHidden)
            {
                if (rageBar >= maxRage) EnterRage();
            }
            //ball mode
            if (!ball.activeSelf)
            {
                if (Input.GetAxisRaw("Vertical") < -ballThreshold && abilities.canBecomeBall)
                {
                    ballCounter -= Time.deltaTime;
                    if (ballCounter <= 0)
                    {
                        ball.SetActive(true);
                        standing.SetActive(false);
                    }
                }
                else
                {
                    ballCounter = waitToBall;
                }
            }
            else
            {
                if (Input.GetAxisRaw("Vertical") > ballThreshold)
                {
                    ballCounter -= Time.deltaTime;
                    if (ballCounter <= 0)
                    {
                        ball.SetActive(false);
                        standing.SetActive(true);
                    }
                }
                else
                {
                    ballCounter = waitToBall;
                }
            }
        } else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (standing.activeSelf)
        {
            //anim.SetBool("isGrounded", isGrounded);
            //anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if(ball.activeSelf)
        {
            ballAnim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
        }

        HandleStates();
    }

    public void ShowAfterImage()
    {
        SpriteRenderer image = Instantiate(afterImageSR, transform.position, transform.rotation);
        image.sprite = sr.sprite;
        image.transform.localScale = transform.localScale;
        image.color = afterImageColor;

        Destroy(image.gameObject, afterImageLifetime);
        afterImageCounter = timeBetweenAfterImages;
    }

    void HandleStates()
    {
        if (currentState == PlayerState.Regular)
        {
            currentMoveSpeed = baseMoveSpeed;
        }
        if (currentState == PlayerState.Rage)
        {
            currentMoveSpeed = rageSpeed;

            rageTimer -= Time.deltaTime;
            if (rageTimer <= 0) ExitRage();
        }

        if (currentState == PlayerState.Disguise && disguiseMode == DisguiseMode.Normal)
        {
            currentMoveSpeed = (disguiseMode == DisguiseMode.Ultra) ? 0f : disguiseSpeed;            
            disguiseTimer -= Time.deltaTime;
            if (disguiseTimer <= 0) ExitDisguise();
        }
    }

    bool IsNearEnemy()
    {
        Debug.Log("player: check is near enemy");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, enemyCheckRadius);
        foreach (Collider2D hit in hits)
        {
            
            if (hit.CompareTag(enemyTag)) return true;
            Debug.Log("player: is near enemy");
        }
        return false;
    }

    // --- Disguise Logic ---
    void EnterDisguise()
    {
        currentState = PlayerState.Disguise;
        disguiseMode = DisguiseMode.Normal;
        disguiseTimer = disguiseDuration;
        normalCharacter.SetActive(false);
        disguisedCharacter.SetActive(true);
        GameObject smoke = Instantiate(smokeBomb, transform.position, Quaternion.identity);

        Destroy(smoke, 1f); // destroy smoke after 1 second
    }

    void ExitDisguise()
    {
        currentState = PlayerState.Regular;
        disguiseMode = DisguiseMode.Normal;
        //spriteRenderer.color = Color.white;

        normalCharacter.SetActive(true);
        disguisedCharacter.SetActive(false);
        GameObject smoke = Instantiate(smokeBomb, transform.position, Quaternion.identity);

        Destroy(smoke, 1f); // destroy smoke after 1 second
    }

    void EnterUltraDisguise()
    {
        disguiseMode = DisguiseMode.Ultra;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.25f);
        disguiseTimer = 0f; // disable normal disguise timer
    }

    void ExitUltraDisguise()
    {
        disguiseMode = DisguiseMode.Normal;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        disguiseTimer = disguiseDuration; // resume countdown if needed
    }

    // --- Rage Logic ---
    void EnterRage()
    {
        currentState = PlayerState.Rage;
        rageTimer = rageDuration;
        spriteRenderer.color = Color.red;
        rageBar = 0;
    }

    void ExitRage()
    {
        currentState = PlayerState.Regular;
        spriteRenderer.color = Color.white;
    }

    public PlayerState GetPlayerState()
    {
        return currentState;
    }

    public bool IsMovingLeft()
    {
        return rb.linearVelocity.x < -0.1f;
    }

    public bool IsMovingRight()
    {
        return rb.linearVelocity.x > 0.1f;
    }

    public bool IsHidden()
    {
        return hider.IsHidden;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(hider.IsHidden && other.gameObject.CompareTag("Enemy"))
        {            

            EnemyPatroller enemyController = other.gameObject.GetComponent<EnemyPatroller>();
            if (enemyController.isReadyForHarvest)
            {
                //TODO: make this a state on the enemy so we're not changing all this from the player
                MarkEnemyForExecution(enemyController);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (hider.IsHidden && other.gameObject.CompareTag("Enemy"))
        {
            EnemyPatroller enemyController = other.gameObject.GetComponent<EnemyPatroller>();
            if(!enemyController.isReadyForExecute)
            {
                MarkEnemyForExecution(enemyController);
            }            
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (hider.IsHidden)
        {
            EnemyPatroller enemyController = other.gameObject.GetComponent<EnemyPatroller>();
            UnMarkEnemyForExecution(enemyController);
        }
    }

    private void MarkEnemyForExecution(EnemyPatroller enemyController)
    {
        enemyController.ActivateExecuteHalo();
        enemyController.isReadyForExecute = true;
    }

    private void UnMarkEnemyForExecution(EnemyPatroller enemyController)
    {
        //enemyController.DeactivateExecuteHalo();
        //enemyController.isReadyForExecute = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(sensorPos, sensorSize);


    }

    private void OnDrawGizmos()
    {
        //if (!Application.isPlaying) return;
        float direction = Mathf.Sign(transform.localScale.x);        
        Vector2 center2D = (Vector2)transform.position + new Vector2(attackSensorOffset.x * direction, attackSensorOffset.y);
        Vector3 center = new Vector3(center2D.x, center2D.y, 0f);
        Vector3 size = new Vector3(attackSensorSize.x, attackSensorSize.y, 0.1f);

        // Convert size to Vector3, give Z a small value
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackSensorPos, attackSensorSize);
    }

}
