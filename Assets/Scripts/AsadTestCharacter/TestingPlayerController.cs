using System.Collections;
using UnityEngine;

public class TestingPlayerController : MonoBehaviour
{
    public enum PlayerState { Regular, Disguise, Rage }
    public enum DisguiseMode { Normal, Ultra }

    [Header("Debug")]
    public PlayerState currentState = PlayerState.Regular;
    public DisguiseMode disguiseMode = DisguiseMode.Normal;

    [Header("Movement Settings")]
    public float regularSpeed = 5f;
    public float disguiseSpeed = 2.5f;
    public float rageSpeed = 8f;

    [Header("Jump Settings")]
    public float jumpForce = 7f;
    public float rageJumpForce = 11f;

    [Header("Rage Settings")]
    public float rageDuration = 5f;
    private float rageTimer;
    public int maxRage = 3;
    public int rageBar = 0;

    [Header("Disguise Settings")]
    public float disguiseDuration = 10f;
    private float disguiseTimer;

    [Header("References")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Enemy Detection")]
    public float enemyCheckRadius = 1.5f;
    public string enemyTag = "Enemy";

    private bool isGrounded;

    void Update()
    {
        HandleInput();
        HandleStates();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleInput()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            float jump = (currentState == PlayerState.Rage) ? rageJumpForce : jumpForce;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jump);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (currentState == PlayerState.Regular) EnterDisguise();
            else if (currentState == PlayerState.Disguise) ExitDisguise();
        }

        // Hit enemy → build rage
        if (currentState == PlayerState.Regular && Input.GetKeyDown(KeyCode.E))
        {
            if (IsNearEnemy()) AddRage(1);
        }

        if (currentState == PlayerState.Disguise && Input.GetKeyDown(KeyCode.E))
        {
            if (disguiseMode == DisguiseMode.Normal) EnterUltraDisguise();
            else if (disguiseMode == DisguiseMode.Ultra) ExitUltraDisguise();
        }

        // Activate rage
        if (currentState == PlayerState.Regular && Input.GetKeyDown(KeyCode.T))
        {
            if (rageBar >= maxRage) EnterRage();
        }
    }

    void HandleMovement()
    {
        float move = Input.GetAxis("Horizontal");
        float speed = 0f;

        switch (currentState)
        {
            case PlayerState.Regular: speed = regularSpeed; break;
            case PlayerState.Disguise:
                speed = (disguiseMode == DisguiseMode.Ultra) ? 0f : disguiseSpeed;
                break;
            case PlayerState.Rage: speed = rageSpeed; break;
        }

        rb.linearVelocity = new Vector2(move * speed, rb.linearVelocity.y);

        if (move != 0) spriteRenderer.flipX = move < 0;
    }

    void HandleStates()
    {
        if (currentState == PlayerState.Rage)
        {
            rageTimer -= Time.deltaTime;
            if (rageTimer <= 0) ExitRage();
        }

        if (currentState == PlayerState.Disguise && disguiseMode == DisguiseMode.Normal)
        {
            disguiseTimer -= Time.deltaTime;
            if (disguiseTimer <= 0) ExitDisguise();
        }
    }

    // Call this from any attack that hits an enemy
    public void AddRage(int amount)
    {
        if (currentState == PlayerState.Rage) return;

        rageBar = Mathf.Min(rageBar + amount, maxRage);
        Debug.Log(rageBar >= maxRage ? "RAGE READY — press T" : $"Rage Bar: {rageBar}/{maxRage}");
    }

    bool IsNearEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, enemyCheckRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(enemyTag)) return true;
        }
        return false;
    }

    void EnterDisguise()
    {
        currentState = PlayerState.Disguise;
        disguiseMode = DisguiseMode.Normal;
        disguiseTimer = disguiseDuration;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
    }

    void ExitDisguise()
    {
        currentState = PlayerState.Regular;
        disguiseMode = DisguiseMode.Normal;
        spriteRenderer.color = Color.white;
    }

    void EnterUltraDisguise()
    {
        disguiseMode = DisguiseMode.Ultra;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.25f);
        disguiseTimer = 0f;
    }

    void ExitUltraDisguise()
    {
        disguiseMode = DisguiseMode.Normal;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        disguiseTimer = disguiseDuration;
    }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyCheckRadius);
    }

    // -------------------
    // PLAYER MOVEMENT HELPERS
    // -------------------
    public bool IsMovingLeft()
    {
        return rb.linearVelocity.x < -0.1f;
    }

    public bool IsMovingRight()
    {
        return rb.linearVelocity.x > 0.1f;
    }

    // -------------------
    // ACCESS HELPERS FOR OTHER SCRIPTS
    // -------------------
    public int GetRageBar()
    {
        return rageBar;
    }

    public int GetMaxRage()
    {
        return maxRage;
    }

    public bool IsHidden()
    {
        return currentState == PlayerState.Disguise;
    }
}