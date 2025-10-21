using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Interaction Sensor")]
    [SerializeField] private Vector2 sensorSize = new(1f, 1f);
    [SerializeField] private Vector2 sensorOffset = Vector2.zero;
    [SerializeField] private LayerMask interactableMask;

    private IInteractable currentInteractable;
    private Collider2D currentCollider;

    void FixedUpdate()
    {
        DetectInteractable();
    }

    void Update()
    {
        if (currentInteractable == null)
            return;

        var options = currentInteractable.GetInteractions();
        foreach (var opt in options)
        {
            if (Input.GetKeyDown(opt.key))
            {
                currentInteractable.TryTriggerInteraction(opt.key);

                // Refresh prompt in case interactions changed after trigger
                RefreshPrompt();
                break;
            }
        }
    }

    void OnEnable()
    {
        // Re-scan for interactables when we come back online
        DetectInteractable();

        // If we're already overlapping something, refresh the prompt manually
        if (currentInteractable != null)
        {
            RefreshPrompt();
        }
    }

    private void DetectInteractable()
    {
        Vector2 sensorPos = (Vector2)transform.position + sensorOffset;
        Collider2D hit = Physics2D.OverlapBox(sensorPos, sensorSize, 0f, interactableMask);

        if (hit != null)
        {
            // Only update if we detected a new collider
            if (currentCollider != hit)
            {
                // Look for ANY IInteractable component
                IInteractable interactable = hit.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    SetCurrentInteractable(interactable, hit);
                }
            }
        }
        else
        {
            ClearCurrent();
        }
    }

    private void SetCurrentInteractable(IInteractable interactable, Collider2D collider)
    {
        currentInteractable = interactable;
        currentCollider = collider;
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (currentInteractable != null && currentCollider != null)
        {
            List<InteractionOption> options = currentInteractable.GetInteractions();

            // Check if the interactable has a custom prompt position
            Transform promptTarget = currentCollider.transform;

            // Try to get custom prompt position via interface
            IPromptPositionable positionable = currentCollider.GetComponent<IPromptPositionable>();
            if (positionable != null)
            {
                promptTarget = positionable.GetPromptPosition();
            }

            InteractionPrompt.Instance?.Show(options, promptTarget);
        }
    }

    void ClearCurrent()
    {
        if (currentInteractable != null)
        {
            // Notify the interactable that player is leaving range
            NotifyInteractableExit(currentCollider);

            currentInteractable = null;
            currentCollider = null;
            InteractionPrompt.Instance?.Hide();
        }
    }

    private void NotifyInteractableExit(Collider2D collider)
    {
        // Check for any components that need exit notification
        var exitNotifiables = collider.GetComponents<MonoBehaviour>();
        foreach (var component in exitNotifiables)
        {
            // Check if component has OnPlayerExitRange method
            if (component is HideableObject hideableObject)
            {
                hideableObject.OnPlayerExitRange();
            }
            // Add more types here as needed
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 sensorPos = (Vector2)transform.position + sensorOffset;
        Gizmos.DrawWireCube(sensorPos, sensorSize);
    }
}