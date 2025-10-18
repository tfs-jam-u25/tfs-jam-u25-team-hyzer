using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class HideableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private SpriteRenderer objectSprite;
    [SerializeField] private float hiddenAlpha = 0.6f;
    [SerializeField] private float fadeSpeed = 1f;
    [SerializeField] private GameObject smokeEffectPrefab;

    private PlayerHider currentPlayer;
    private float originalAlpha;
    private bool isPlayerHidden = false;
    private MultiInteractable multiInteractable;

    public SpriteRenderer ObjectSprite => objectSprite;

    void Awake()
    {
        if (objectSprite == null)
            objectSprite = GetComponentInChildren<SpriteRenderer>();

        if (objectSprite != null)
            originalAlpha = objectSprite.color.a;

        // Check if we're part of a MultiInteractable
        multiInteractable = GetComponent<MultiInteractable>();
    }

    public List<InteractionOption> GetOptions()
    {
        return new List<InteractionOption>
        {
            new InteractionOption
            {
                key = KeyCode.F,
                description = isPlayerHidden ? "Unhide" : "Hide"
            }
        };
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            currentPlayer = other.GetComponent<PlayerHider>() ??
                            other.GetComponentInParent<PlayerHider>() ??
                            other.GetComponentInChildren<PlayerHider>();

            Debug.Log($"[HideableObject] Player entered. PlayerHider found: {currentPlayer != null}");

            // Don't manually refresh prompt - let PlayerInteractionController handle it
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[HideableObject] Player exited");

            if (currentPlayer != null && isPlayerHidden)
            {
                currentPlayer.ForceUnhideIfLeftArea(this);
                isPlayerHidden = false;

                if (objectSprite != null)
                {
                    StopAllCoroutines();
                    StartCoroutine(FadeAlpha(originalAlpha));
                }
            }

            currentPlayer = null;
        }
    }

    public void Interact(KeyCode key, GameObject player)
    {
        if (key != KeyCode.F) return;

        if (currentPlayer == null)
        {
            currentPlayer = player.GetComponent<PlayerHider>() ??
                            player.GetComponentInParent<PlayerHider>() ??
                            player.GetComponentInChildren<PlayerHider>();
        }

        if (currentPlayer == null)
        {
            Debug.LogError("[HideableObject] Cannot interact - no PlayerHider component found!");
            return;
        }

        if (isPlayerHidden)
            Unhide();
        else
            Hide();

        // Force prompt update after state change
        RefreshInteractionPrompt();
    }

    private void Hide()
    {
        Debug.Log("[HideableObject] Hiding player");
        isPlayerHidden = true;

        int sortingOrder = objectSprite != null ? objectSprite.sortingOrder : 0;
        string sortingLayer = objectSprite != null ? objectSprite.sortingLayerName : "Hideables";

        currentPlayer.Hide(this, sortingOrder, sortingLayer);

        if (smokeEffectPrefab != null)
            Instantiate(smokeEffectPrefab, currentPlayer.transform.position, Quaternion.identity);

        if (objectSprite != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeAlpha(hiddenAlpha));
        }
    }

    private void Unhide()
    {
        Debug.Log("[HideableObject] Unhiding player");
        isPlayerHidden = false;
        currentPlayer.Unhide();

        if (smokeEffectPrefab != null)
            Instantiate(smokeEffectPrefab, currentPlayer.transform.position, Quaternion.identity);

        if (objectSprite != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeAlpha(originalAlpha));
        }
    }

    private void RefreshInteractionPrompt()
    {
        if (InteractionPrompt.Instance != null && currentPlayer != null)
        {
            // If we're part of a MultiInteractable, get options from that instead
            if (multiInteractable != null)
            {
                InteractionPrompt.Instance.Show(multiInteractable.GetOptions());
            }
            else
            {
                InteractionPrompt.Instance.Show(GetOptions());
            }
        }
    }

    private IEnumerator FadeAlpha(float targetAlpha)
    {
        Debug.Log("player hiding - try to fade");
        Color c = objectSprite.color;
        while (!Mathf.Approximately(c.a, targetAlpha))
        {
            c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
            objectSprite.color = c;
            yield return null;
        }
        Debug.Log($"[HideableObject] Fade complete. Final alpha: {objectSprite.color.a}");
    }
}