using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public AudioClip clickSfx;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayClick()
    {
        audioSource.PlayOneShot(clickSfx);
    }
}
