using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private AudioSource audioSource;
    private AudioClip defaultClip;

    [Range(0.5f, 5f)] public float fadeDuration = 1.5f;

    private Coroutine fadeCoroutine;
    private bool isInTriggerZone = false;

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        defaultClip = audioSource.clip;
    }

    void Update()
    {
        // If player dies, return to default music immediately
        if (PlayerHealthController.instance != null)
        {
            if (PlayerHealthController.instance.currentHealth <= 0 && audioSource.clip != defaultClip)
            {
                ReturnToDefault();
                isInTriggerZone = false; // reset trigger state
            }
        }
    }

    public void PlayMusic(AudioClip newClip)
    {
        if (newClip == null || audioSource.clip == newClip) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewClip(newClip));
    }

    public void ReturnToDefault()
    {
        if (audioSource.clip == defaultClip) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewClip(defaultClip));
    }

    private IEnumerator FadeToNewClip(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        // Fade out
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0;
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0, startVolume, t / fadeDuration);
            yield return null;
        }

        audioSource.volume = startVolume;
    }

    // Public helper for MusicTrigger
    public void SetTriggerZone(bool state)
    {
        isInTriggerZone = state;
    }

    public bool IsInTriggerZone() => isInTriggerZone;
}
