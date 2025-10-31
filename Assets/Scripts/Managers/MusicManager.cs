using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource audioSource;

    [Header("Music Settings")]
    [Range(0.5f, 5f)] public float fadeDuration = 1.5f;
    [SerializeField] private AudioClip defaultTrack; //TODO: remove duplicate default clip vs default track - which one is in use?

    [Header("Songs")]
    [SerializeField] private AudioClip defaultClip;
    [SerializeField] private AudioClip ambientClip;
    [SerializeField] private AudioClip ambientClip2;
    [SerializeField] private AudioClip bellsClip;

    //TODO: #music #soundtrack boss music is managed separately for now but should be managed through here later    

    private AudioClip currentTrack;

    private string currentSceneName;

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

        //audioSource = GetComponent<AudioSource>();
        //defaultClip = audioSource.clip;
        // Setup sources


        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        currentSceneName = SceneManager.GetActiveScene().name;
        currentTrack = defaultTrack;

        SceneManager.sceneLoaded += OnSceneLoaded;
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newScene = scene.name;

        // If you want to play a different track based on the scene:
        AudioClip newTrack = GetTrackForScene(newScene);

        if (newTrack == null)
        {
            // Continue looping current track
            return;
        }

        if (newTrack != currentTrack)
        {
            PlayMusic(newTrack);
        }

        currentSceneName = newScene;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private AudioClip GetTrackForScene(string sceneName)
    {
        // 🎶 Add your scene-music mapping logic here
        // Returning null = continue playing the current song
        switch (sceneName)
        {   //TODO: don't use hard coded string scene names here. Rushing a release out right now :(
            case "DanaForestOpener1":
                return bellsClip;
            case "DanaBoss1":
                return ambientClip2;
            default:
                return null; // no change
        }
    }
}
