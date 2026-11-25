using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FearedState : MonoBehaviour
{
    public enum FearModifier { Stoic, Weak, Emboldened }
    public enum FearBehaviour { Run, Survivor, Freeze }

    [System.Serializable]
    public class StatusVisual
    {
        public Sprite icon;
        public AudioClip sound;
        public Color tint = Color.white;
        [Range(0f, 1f)] public float volume = 1f;
    }

    // ---------------- FEAR SETTINGS ----------------

    [Header("Current Fear (read-only, for debugging)")]
    public bool isActive = false;
    public FearModifier currentModifier;
    public FearBehaviour currentBehaviour;
    public int currentDamage = 0;

    [Header("Allowed Modifiers (untick to remove from the random pool)")]
    public bool allowStoic = true;
    public bool allowWeak = true;
    public bool allowEmboldened = true;

    [Header("Allowed Behaviours (untick to remove from the random pool)")]
    public bool allowRun = true;
    public bool allowSurvivor = true;
    public bool allowFreeze = true;

    [Header("Stats")]
    public int damageModifier = 1;                  // how much Weak/Emboldened changes damage
    public float runSpeed = 3f;                     // flee speed for Run
    public float survivorAttackSpeedMultiplier = 1.5f;

    // ---------------- ICON + SOUND SETTINGS ----------------

    [Header("Modifier Visuals (left icon)")]
    public StatusVisual weak = new StatusVisual();
    public StatusVisual emboldened = new StatusVisual();
    public StatusVisual stoic = new StatusVisual();

    [Header("Behaviour Visuals (right icon)")]
    public StatusVisual run = new StatusVisual();
    public StatusVisual survivor = new StatusVisual();
    public StatusVisual freeze = new StatusVisual();

    [Header("What To Show")]
    public bool showModifier = true;
    public bool showBehaviour = true;

    [Header("Icon Layout (live-editable in Play mode)")]
    public Vector2 iconOffset = new Vector2(0f, 1.2f); // position above the enemy
    public float iconSpacing = 0.45f;                   // gap between the two icons
    public float iconScale = 0.5f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;                      // high so icons draw on top

    [Header("Pop-in Animation")]
    public bool popIn = true;
    public float popDuration = 0.2f;

    [Header("Sound")]
    public float delayBetweenSounds = 0.15f; // modifier sound, then behaviour sound

    // ---------------- PRIVATE ----------------

    private Transform player;
    private Rigidbody2D rb;

    private Transform iconContainer;
    private SpriteRenderer modifierRenderer;
    private SpriteRenderer behaviourRenderer;
    private AudioSource audioSource;
    private float popTimer;
    private Coroutine soundRoutine;

    public FearBehaviour CurrentBehaviour => currentBehaviour;

    // EnemyAI multiplies its attack speed by this
    public float AttackSpeedMultiplier =>
        (isActive && currentBehaviour == FearBehaviour.Survivor) ? survivorAttackSpeedMultiplier : 1f;

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();

        BuildIcons();

        // own audio source so it doesn't interfere with other enemy sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    // ================= FEAR ON / OFF =================

    // Called by EnemyAI when fear starts / ends
    public void SetActive(bool active)
    {
        isActive = active;

        if (active)
        {
            RollFear();
            ApplyFearModifiers();
            ShowIcons();
            Debug.Log($"<color=yellow>{name} is feared: {currentModifier} + {currentBehaviour}</color>");
        }
        else
        {
            ResetModdifiers();
            HideIcons();
        }
    }

    void RollFear()
    {
        // build the pool of allowed modifiers, then pick one
        List<FearModifier> mods = new List<FearModifier>();
        if (allowStoic) mods.Add(FearModifier.Stoic);
        if (allowWeak) mods.Add(FearModifier.Weak);
        if (allowEmboldened) mods.Add(FearModifier.Emboldened);
        currentModifier = mods.Count > 0 ? mods[Random.Range(0, mods.Count)] : FearModifier.Stoic;

        // same for behaviours
        List<FearBehaviour> behaviours = new List<FearBehaviour>();
        if (allowRun) behaviours.Add(FearBehaviour.Run);
        if (allowSurvivor) behaviours.Add(FearBehaviour.Survivor);
        if (allowFreeze) behaviours.Add(FearBehaviour.Freeze);
        currentBehaviour = behaviours.Count > 0 ? behaviours[Random.Range(0, behaviours.Count)] : FearBehaviour.Run;
    }

    public void ApplyFearModifiers()
    {
        switch (currentModifier)
        {
            case FearModifier.Weak: currentDamage = -damageModifier; break; // hits softer
            case FearModifier.Emboldened: currentDamage = damageModifier; break; // hits harder
            default: currentDamage = 0; break; // Stoic
        }
    }

    public void ResetModdifiers()
    {
        currentDamage = 0;
    }

    public int GetCurrentDamage()
    {
        return isActive ? currentDamage : 0;
    }

    // ================= BEHAVIOURS =================

    void Update()
    {
        if (!isActive || rb == null) return;

        switch (currentBehaviour)
        {
            case FearBehaviour.Run:
                if (player != null)
                {
                    // flee horizontally away from the player, keep vertical velocity (gravity)
                    float dir = Mathf.Sign(transform.position.x - player.position.x);
                    rb.linearVelocity = new Vector2(dir * runSpeed, rb.linearVelocity.y);
                }
                break;

            case FearBehaviour.Freeze:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case FearBehaviour.Survivor:
                // handled in EnemyAI (it chases + attacks faster)
                break;
        }
    }

    // ================= ICONS =================

    void BuildIcons()
    {
        // holder object for both icons, parented to the enemy
        iconContainer = new GameObject("FearStatusIcons").transform;
        iconContainer.SetParent(transform, false);

        modifierRenderer = CreateIconRenderer("ModifierIcon");
        behaviourRenderer = CreateIconRenderer("BehaviourIcon");
    }

    SpriteRenderer CreateIconRenderer(string objectName)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(iconContainer, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.enabled = false;
        return sr;
    }

    void ShowIcons()
    {
        StatusVisual mod = GetModifierVisual(currentModifier);
        StatusVisual beh = GetBehaviourVisual(currentBehaviour);

        ApplyVisual(modifierRenderer, mod, showModifier);
        ApplyVisual(behaviourRenderer, beh, showBehaviour);

        popTimer = 0f;

        if (soundRoutine != null) StopCoroutine(soundRoutine);
        soundRoutine = StartCoroutine(PlaySounds(
            showModifier ? mod : null,
            showBehaviour ? beh : null));
    }

    void HideIcons()
    {
        if (modifierRenderer != null) modifierRenderer.enabled = false;
        if (behaviourRenderer != null) behaviourRenderer.enabled = false;
    }

    void ApplyVisual(SpriteRenderer sr, StatusVisual visual, bool allowed)
    {
        sr.sprite = visual.icon;
        sr.color = visual.tint;
        sr.enabled = allowed && visual.icon != null;
    }

    // LateUpdate runs after EnemyAI flips the enemy, so the flip-cancel is always correct
    void LateUpdate()
    {
        if (!isActive) return;

        // cancel out the enemy's left/right flip so icons never appear mirrored
        float flip = Mathf.Sign(transform.lossyScale.x);
        if (flip == 0f) flip = 1f;

        // pop-in scale
        float pop = 1f;
        if (popIn && popDuration > 0f)
        {
            popTimer += Time.deltaTime;
            pop = EaseOutBack(Mathf.Clamp01(popTimer / popDuration));
        }

        iconContainer.localPosition = new Vector3(iconOffset.x * flip, iconOffset.y, 0f);
        iconContainer.localScale = new Vector3(flip, 1f, 1f) * iconScale * pop;

        // one icon → centred, two icons → side by side
        bool bothVisible = modifierRenderer.enabled && behaviourRenderer.enabled;
        float half = bothVisible ? iconSpacing / (2f * Mathf.Max(iconScale, 0.0001f)) : 0f;
        modifierRenderer.transform.localPosition = new Vector3(-half, 0f, 0f);
        behaviourRenderer.transform.localPosition = new Vector3(half, 0f, 0f);

        // keep sorting live-editable
        modifierRenderer.sortingLayerName = sortingLayerName;
        behaviourRenderer.sortingLayerName = sortingLayerName;
        modifierRenderer.sortingOrder = sortingOrder;
        behaviourRenderer.sortingOrder = sortingOrder;
    }

    IEnumerator PlaySounds(StatusVisual first, StatusVisual second)
    {
        if (first != null && first.sound != null)
        {
            audioSource.PlayOneShot(first.sound, first.volume);
            yield return new WaitForSeconds(delayBetweenSounds);
        }

        if (second != null && second.sound != null)
            audioSource.PlayOneShot(second.sound, second.volume);

        soundRoutine = null;
    }

    StatusVisual GetModifierVisual(FearModifier m)
    {
        switch (m)
        {
            case FearModifier.Weak: return weak;
            case FearModifier.Emboldened: return emboldened;
            default: return stoic;
        }
    }

    StatusVisual GetBehaviourVisual(FearBehaviour b)
    {
        switch (b)
        {
            case FearBehaviour.Run: return run;
            case FearBehaviour.Survivor: return survivor;
            default: return freeze;
        }
    }

    // slight overshoot then settle — gives the "pop"
    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}