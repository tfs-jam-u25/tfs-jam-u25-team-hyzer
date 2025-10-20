using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class InteractionOption
{
    public string promptText;
    public KeyCode key;
    public UnityEvent onInteract;
}

public interface IInteractable
{
    List<InteractionOption> GetInteractions();
    bool TryTriggerInteraction(KeyCode key);
}

public interface IPromptPositionable
{
    Transform GetPromptPosition();
}

public class Interactable : MonoBehaviour, IInteractable, IPromptPositionable
{
    [Header("Manual Interactions (optional)")]
    public List<InteractionOption> manualInteractions = new List<InteractionOption>();

    [Header("Prompt Position")]
    [SerializeField] private Transform promptPositionOverride;
    [Tooltip("Optional: Set a custom transform for where the prompt appears. If null, uses this object's center.")]

    [Header("Auto-detect other interactables on this object")]
    public bool includeChildInteractables = false;

    private List<IInteractable> _childInteractables = new();

    private void Awake()
    {
        // Collect any IInteractable components on this object or its children
        var interactables = includeChildInteractables
            ? GetComponentsInChildren<IInteractable>()
            : GetComponents<IInteractable>();

        foreach (var i in interactables)
        {
            if (i as Object != (Object)this)
                _childInteractables.Add(i);
        }
    }

    public Transform GetPromptPosition()
    {
        return promptPositionOverride != null ? promptPositionOverride : transform;
    }

    public List<InteractionOption> GetInteractions()
    {
        List<InteractionOption> all = new();
        all.AddRange(manualInteractions);

        foreach (var child in _childInteractables)
            all.AddRange(child.GetInteractions());

        return all;
    }

    public bool TryTriggerInteraction(KeyCode key)
    {
        // Try manual interactions first
        foreach (var interaction in manualInteractions)
        {
            if (interaction.key == key)
            {
                interaction.onInteract?.Invoke();
                return true;
            }
        }

        // Try child interactables
        foreach (var child in _childInteractables)
        {
            if (child.TryTriggerInteraction(key))
                return true;
        }

        return false;
    }
}