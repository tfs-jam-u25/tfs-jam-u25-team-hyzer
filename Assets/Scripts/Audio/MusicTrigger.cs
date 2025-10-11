using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MusicTrigger : MonoBehaviour
{
    public AudioClip newMusic;

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && newMusic != null)
        {
            MusicManager.Instance.PlayMusic(newMusic);
            MusicManager.Instance.SetTriggerZone(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            MusicManager.Instance.ReturnToDefault();
            MusicManager.Instance.SetTriggerZone(false);
        }
    }
}
