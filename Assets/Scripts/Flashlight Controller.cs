using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    public GameObject flashlightLight;
    public AudioSource audioSource;
    public TextMeshProUGUI promptText; 

    // REMOVED: public bool hasFlashlight = false;
    // We no longer need this because the GameManager tracks it permanently!

    [Header("Settings")]
    public AudioClip soundOn;
    public AudioClip soundOff;

    private bool isOn = false;

    void Start()
    {
        if(promptText != null) promptText.gameObject.SetActive(false);
        
        // Ensure the AudioSource is assigned
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // Ensure the flashlight starts turned off when entering a new scene
        if (flashlightLight != null) flashlightLight.SetActive(false);
    }

    void Update()
    {
        // UPDATED: Check the GameManager directly. 
        // Now, if you press 'F', it asks the persistent memory if you actually own the flashlight.
        if (GameManager.Instance != null && GameManager.Instance.hasFlashlight == true)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                ToggleFlashlight();
            }
        }
    }

    public void EnableFlashlight()
    {
        // This is called by your PlayerInteraction script the very first time you pick it up
        if(promptText != null) promptText.gameObject.SetActive(true);
    }

    void ToggleFlashlight()
    {
        if(promptText != null) promptText.gameObject.SetActive(false);
        
        isOn = !isOn;
        if (flashlightLight != null) flashlightLight.SetActive(isOn);

        PlayToggleSound();
    }

    private void PlayToggleSound()
    {
        if (audioSource != null)
        {
            // 1. Kill any sound currently playing (the "Anti-Spam" fix)
            audioSource.Stop();

            // 2. Choose the correct clip
            AudioClip clipToPlay = isOn ? soundOn : soundOff;

            if (clipToPlay != null)
            {
                // 3. Set the clip and play it fresh
                audioSource.clip = clipToPlay;
                
                // Optional: Add a tiny pitch variation to make spamming less repetitive
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                
                audioSource.Play();
            }
        }
    }
}