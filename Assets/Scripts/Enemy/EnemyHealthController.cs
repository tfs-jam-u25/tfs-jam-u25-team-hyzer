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

    public float hitDelay = 0.25f;
    private float hitCounter = 0f;

    public bool isHittable = true;

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
        } else
        {
            hitCounter = 0f;
            isHittable = true;
        }
    }

    public void DamageEnemy(int damageAmount)
    {
        
        if(!isHittable)
        {
            Debug.Log("Enemy currently hit, dead or wounded. Cannot be hit.");
            return;
        }

        isHittable = false;
        totalHealth -= damageAmount;
        hitCounter = hitDelay;
        
        if(totalHealth <= 0 )
        {
            healthStateMachine.ChangeState(HealthState.Dead);
            StartCoroutine(PlayDeadAndWait(deathSound));
            //audioSource.PlayOneShot(deathSound);                        
        } else if(totalHealth < maxHealth && totalHealth > 0) {
            healthStateMachine.ChangeState(HealthState.Wounded);
        } else if(totalHealth == 1)
        {
            healthStateMachine.ChangeState(HealthState.GravelyWounded);
        }
    }

    public void ExecuteEnemy()
    {
        if(healthStateMachine.CurrentState == HealthState.InExecute)
        {
            Debug.Log("Enemy currently hit, dead or wounded. Cannot be executed.");
            return;
        }
        Debug.Log("Update enemy state to InExecute");
        healthStateMachine.ChangeState(HealthState.InExecute);
        totalHealth = 0;
        GameManager.Instance.harvestScore.AddExecution(ExecutionScore.Type.silence);

        StartCoroutine(PlayDeadAndWait(executeSound));        

    }

    public int GetCurrentHealth()
    {
        return totalHealth;
    }

    private void Death()
    {        
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
        audioSource.PlayOneShot(sample);
        yield return new WaitForSeconds(sample.length);

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
                break;

            case HealthState.InExecute:                
                break;

            case HealthState.Dead:
                break;
        }
    }
}
