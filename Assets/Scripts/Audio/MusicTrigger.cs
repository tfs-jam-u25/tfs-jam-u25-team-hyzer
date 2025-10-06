using UnityEngine;
using System.Collections;

public class MusicTrigger : MonoBehaviour
{
    [Header("Boss Music")]
    public AudioClip bossMusic;
    public float fadeTime = 1.5f; // seconds

    private AudioSource bossAudioSource;

    private void Start()
    {
        // Create a new AudioSource for this trigger
        bossAudioSource = gameObject.AddComponent<AudioSource>();
        bossAudioSource.loop = true;
        bossAudioSource.playOnAwake = false;
        bossAudioSource.volume = 0f; // start silent
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Stop background music with fade
            StartCoroutine(FadeOut(MusicManager.Instance.audioSource, fadeTime));

            // Play boss music with fade in
            bossAudioSource.clip = bossMusic;
            bossAudioSource.Play();
            StartCoroutine(FadeIn(bossAudioSource, fadeTime));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Fade out boss music
            StartCoroutine(FadeOut(bossAudioSource, fadeTime));

            // Resume background music with fade in
            StartCoroutine(FadeIn(MusicManager.Instance.audioSource, fadeTime));
        }
    }

    // 🔹 Fade helpers
    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        if (source == null || !source.isPlaying) yield break;

        float startVolume = source.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            yield return null;
        }

        source.volume = 0f;
        source.Pause(); // stop but remember time
    }

    private IEnumerator FadeIn(AudioSource source, float duration)
    {
        if (source == null) yield break;

        float time = 0f;
        source.Play();

        while (time < duration)
        {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }

        source.volume = 1f;
    }
}
