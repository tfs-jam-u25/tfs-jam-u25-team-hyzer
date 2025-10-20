using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HideableObject : MonoBehaviour, IInteractable
{
    [Header("Hiding Settings")]
    [SerializeField] private KeyCode hideKey = KeyCode.F;
    [SerializeField] private string hiddenPrompt = "Hide";
    [SerializeField] private string unhidePrompt = "Unhide";

    [Header("Visual Settings")]
    [SerializeField] private float hiddenAlpha = 0.3f;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private SpriteRenderer spriteRenderer;
    private float originalAlpha;
    private Coroutine fadeCoroutine;

    // Reference to player's hider component
    private PlayerHider playerHider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalAlpha = spriteRenderer.color.a;
    }

    public List<InteractionOption> GetInteractions()
    {
        var options = new List<InteractionOption>();

        // Check if player is currently hidden in THIS object
        bool isPlayerHiddenHere = playerHider != null &&
                                   playerHider.IsHidden &&
                                   playerHider.CurrentHideSpot == this;

        var option = new InteractionOption
        {
            promptText = isPlayerHiddenHere ? unhidePrompt : hiddenPrompt,
            key = hideKey,
            onInteract = new UnityEngine.Events.UnityEvent()
        };

        option.onInteract.AddListener(ToggleHide);
        options.Add(option);

        return options;
    }

    public bool TryTriggerInteraction(KeyCode key)
    {
        if (key == hideKey)
        {
            ToggleHide();
            return true;
        }
        return false;
    }

    private void ToggleHide()
    {
        // Find player hider if not cached
        if (playerHider == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHider = player.GetComponent<PlayerHider>();
                if (playerHider == null)
                {
                    Debug.LogError("[HideableObject] Player doesn't have PlayerHider component!");
                    return;
                }
            }
            else
            {
                Debug.LogError("[HideableObject] No GameObject with 'Player' tag found!");
                return;
            }
        }

        // Toggle based on current state
        if (playerHider.IsHidden && playerHider.CurrentHideSpot == this)
        {
            Unhide();
        }
        else
        {
            Hide();
        }
    }

    private void Hide()
    {
        if (playerHider == null)
            return;

        // Tell the player to hide using this object's sorting layer/order
        playerHider.Hide(this, spriteRenderer.sortingLayerName, spriteRenderer.sortingOrder);

        // Fade this object to semi-transparent
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToAlpha(hiddenAlpha));

        if (showDebugLogs)
        {
            Debug.Log($"[HideableObject] Player hiding in {gameObject.name}");
        }
    }

    public void Unhide()
    {
        if (playerHider == null)
            return;

        // Tell the player to unhide
        playerHider.Unhide();

        // Restore this object's alpha
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeToAlpha(originalAlpha));

        if (showDebugLogs)
        {
            Debug.Log($"[HideableObject] Player unhidden from {gameObject.name}");
        }
    }

    // Called by PlayerInteractionController when player leaves range
    public void OnPlayerExitRange()
    {
        if (playerHider != null)
        {
            playerHider.ForceUnhideIfLeftArea(this);

            // Restore alpha if player was hiding here
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeToAlpha(originalAlpha));
        }
    }

    private IEnumerator FadeToAlpha(float targetAlpha)
    {
        Color startColor = spriteRenderer.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        spriteRenderer.color = targetColor;
    }
}