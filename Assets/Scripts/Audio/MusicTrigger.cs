using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    public AudioClip bossMusic;
    private AudioClip previousClip;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Save what was playing before
            previousClip = MusicManager.Instance.GetCurrentClip();

            // Switch to boss music
            MusicManager.Instance.PlayMusic(bossMusic);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Switch back to old music (resume)
            MusicManager.Instance.PlayMusic(previousClip, true);
        }
    }
}
