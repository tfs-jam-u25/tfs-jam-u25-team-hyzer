using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyHealthController : MonoBehaviour
{
    public int totalHealth = 0;
    public int maxHealth = 3;
    public GameObject deathEffect;

    [SerializeField] private AudioSource audioSource;
    public AudioClip deathSound;
    public AudioClip executeSound;
    public AudioClip hitSound; // Optional hit sound

    public float hitDelay = 0.25f;
    private float hitCounter = 0f;

    public bool isHittable = true;

    // Reference to patroller for visual/physics effects
    private EnemyPatroller patroller;

    public enum HealthState
    {
        Full,
        Wounded,
        GravelyWounded,
        InExecute,
        Dead
    }

    private StateMachine<HealthState> healthStateMachine;
    public StateMachine<HealthState> StateMachine => healthStateMachine;

    private void Awake()
    {
        // Get patroller component
        patroller = GetComponent<EnemyPatroller>();

        // Initialize with starting state
        healthStateMachine = new StateMachine<HealthState>(HealthState.Full); //may be an issue if we ever want to start an enemy at less than full health

        // Subscribe to state change event
        healthStateMachine.OnStateChanged += HandleHealthStateChanged;
        totalHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        if (hitCounter > 0f)
        {
            hitCounter -= Time.fixedDeltaTime;
        }
        else
        {
            hitCounter = 0f;
            isHittable = true;
        }
    }

    // Main damage function with knockback
    public void DamageEnemy(int damageAmount, Vector2 knockbackForce, ForceMode2D forceType = ForceMode2D.Force)
    {

        if (!isHittable)
        {
            Debug.Log("Enemy currently hit, dead or wounded. Cannot be hit.");
            return;
        }

        isHittable = false;
        totalHealth -= damageAmount;
        hitCounter = hitDelay;

        // Trigger patroller's hit reaction (blood, knockback, animation)
        if (patroller != null)
        {
            patroller.TakeHit(knockbackForce, forceType);
        }

        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (totalHealth <= 0)
        {
            healthStateMachine.ChangeState(HealthState.Dead);
            StartCoroutine(PlayDeadAndWait(deathSound));
            //audioSource.PlayOneShot(deathSound);                        
        }
        else if (totalHealth < maxHealth && totalHealth > 0)
        {
            healthStateMachine.ChangeState(HealthState.Wounded);
        }
        else if (totalHealth == 1)
        {
            healthStateMachine.ChangeState(HealthState.GravelyWounded);
        }
    }

    // Overload for simple damage without knockback
    public void DamageEnemy(int damageAmount)
    {
        DamageEnemy(damageAmount, Vector2.zero, ForceMode2D.Force);
    }

    public void ExecuteEnemy()
    {
        if (healthStateMachine.CurrentState == HealthState.InExecute)
        {
            Debug.Log("Enemy currently hit, dead or wounded. Cannot be executed.");
            return;
        }
        Debug.Log("Update enemy state to InExecute");
        healthStateMachine.ChangeState(HealthState.InExecute);
        totalHealth = 0;
        GameManager.Instance.harvestScore.AddExecution(ExecutionScore.Type.silence);

        // Trigger patroller's death visuals
        if (patroller != null)
        {
            patroller.Die();
        }

        StartCoroutine(PlayDeadAndWait(executeSound));
    }

    public int GetCurrentHealth()
    {
        return totalHealth;
    }

    public bool IsDead()
    {
        return healthStateMachine.CurrentState == HealthState.Dead ||
               healthStateMachine.CurrentState == HealthState.InExecute;
    }

    private void Death()
    {
        // Trigger patroller's death method
        if (patroller != null)
        {
            patroller.Die();
        }

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }

    IEnumerator PlayDeadAndWait(AudioClip sample)
    {
        //should add an enemy visual effect here, maybe toggle the sprite renderer at least
        //currentState = HealhState.Dead;

        // Trigger death visuals immediately
        if (patroller != null)
        {
            patroller.Die();
        }

        float waitTime = 0.5f; // Default wait time

        if (sample != null && audioSource != null)
        {
            audioSource.PlayOneShot(sample);
            waitTime = sample.length;
        }

        // Wait for sound AND give animation time to play
        // Use the longer of: sound length or 1 second (for animation)
        yield return new WaitForSeconds(Mathf.Max(waitTime, 1f));

        Debug.Log("Sound finished playing!");
        // Trigger your "complete" logic here
        Death();
    }

    private void OnDestroy()
    {
        healthStateMachine.OnStateChanged -= HandleHealthStateChanged;
    }

    private void HandleHealthStateChanged(HealthState oldState, HealthState newState)
    {
        Debug.Log($"Enemy changed state: {oldState} -> {newState}");

        switch (newState)
        {
            case HealthState.Full:

                break;

            case HealthState.Wounded:
                break;

            case HealthState.GravelyWounded:
                // Activate harvest halo if not already active
                if (patroller != null && !patroller.isReadyForHarvest)
                {
                    patroller.isReadyForHarvest = true;
                    patroller.ActivateHarvestHalo();
                }
                break;

            case HealthState.InExecute:
                break;

            case HealthState.Dead:
                break;
        }
    }
}