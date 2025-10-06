using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    public AudioSource audioSource;   // Drag an AudioSource here
    public AudioClip startingMusic;   // Optional default music

    private AudioClip currentClip;
    private float savedTime;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If no AudioSource is assigned, add one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
        }

        // Play starting music automatically (optional)
        if (startingMusic != null)
        {
            PlayMusic(startingMusic);
        }
    }

    public void PlayMusic(AudioClip clip, bool resume = false)
    {
        if (clip == null) return;

        if (clip == currentClip && resume)
        {
            audioSource.time = savedTime;
            audioSource.Play();
            return;
        }

        currentClip = clip;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            savedTime = audioSource.time;
            audioSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (currentClip != null)
        {
            PlayMusic(currentClip, true);
        }
    }

    public AudioClip GetCurrentClip()
    {
        return currentClip;
    }
}
