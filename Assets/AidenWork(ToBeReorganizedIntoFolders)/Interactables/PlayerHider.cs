using UnityEngine;

public class PlayerHider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    [Header("Physics Layer Settings")]
    [SerializeField] private string hiddenLayerName = "HiddenPlayer";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // State tracking
    private bool isHidden;
    private HideableObject currentHideSpot;

    // Original values for restoration
    private string originalSpriteSortingLayerName;
    private int originalSpriteSortingOrder;
    private int originalPlayerPhysicsLayer;
    private int originalSpritePhysicsLayer;

    public bool IsHidden => isHidden;
    public HideableObject CurrentHideSpot => currentHideSpot;

    void Awake()
    {
        // Auto-find sprite renderer if not set
        if (playerSpriteRenderer == null)
        {
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (playerSpriteRenderer == null)
            {
                Debug.LogError("[PlayerHider] No SpriteRenderer found on player or children!");
                enabled = false;
                return;
            }
        }

        // Cache original values
        originalSpriteSortingLayerName = playerSpriteRenderer.sortingLayerName;
        originalSpriteSortingOrder = playerSpriteRenderer.sortingOrder;
        originalPlayerPhysicsLayer = gameObject.layer;
        originalSpritePhysicsLayer = playerSpriteRenderer.gameObject.layer;

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerHider] Initialized. Original sorting: {originalSpriteSortingLayerName}:{originalSpriteSortingOrder}");
            Debug.Log($"[PlayerHider] Original layers - Player: {originalPlayerPhysicsLayer}, Sprite: {originalSpritePhysicsLayer}");
        }
    }

    public void Hide(HideableObject hideableObject, string hideableSortingLayer, int hideableSortingOrder)
    {
        if (isHidden)
        {
            if (showDebugLogs)
                Debug.LogWarning("[PlayerHider] Already hidden!");
            return;
        }

        isHidden = true;
        currentHideSpot = hideableObject;

        // Update visual sorting - move player sprite behind the hideable object
        playerSpriteRenderer.sortingLayerName = hideableSortingLayer;
        playerSpriteRenderer.sortingOrder = hideableSortingOrder - 1;

        // Change physics layer to disable enemy collision
        int hiddenLayer = LayerMask.NameToLayer(hiddenLayerName);
        if (hiddenLayer == -1)
        {
            Debug.LogWarning($"[PlayerHider] Layer '{hiddenLayerName}' doesn't exist! Create it in Project Settings.");
        }
        else
        {
            gameObject.layer = hiddenLayer;
            playerSpriteRenderer.gameObject.layer = hiddenLayer;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerHider] Hidden behind {hideableObject.name}");
            Debug.Log($"[PlayerHider] New sorting: {hideableSortingLayer}:{hideableSortingOrder - 1}");
        }
    }

    public void Unhide()
    {
        if (!isHidden)
            return;

        isHidden = false;
        currentHideSpot = null;

        // Restore visual sorting
        playerSpriteRenderer.sortingLayerName = originalSpriteSortingLayerName;
        playerSpriteRenderer.sortingOrder = originalSpriteSortingOrder;

        // Restore physics layers
        gameObject.layer = originalPlayerPhysicsLayer;
        playerSpriteRenderer.gameObject.layer = originalSpritePhysicsLayer;

        if (showDebugLogs)
        {
            Debug.Log("[PlayerHider] Unhidden - restored original sorting and layers");
        }
    }

    public void ForceUnhideIfLeftArea(HideableObject hideableObject)
    {
        if (isHidden && currentHideSpot == hideableObject)
        {
            if (showDebugLogs)
                Debug.Log("[PlayerHider] Force unhiding - left the hide area");
            Unhide();
        }
    }
}