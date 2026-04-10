using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Don't forget this!

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    public GameObject flashlightLight;
    public AudioSource audioSource;
    public TextMeshProUGUI promptText; 

    [Header("Settings")]
    public bool hasFlashlight = false;
    public AudioClip soundOn;
    public AudioClip soundOff;

    private bool isOn = false;

    void Start()
    {
        // Make sure it's hidden when the game starts
        if(promptText != null) promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Only allow the toggle if we have the flashlight
        if (hasFlashlight && Keyboard.current.fKey.wasPressedThisFrame)
        {
            ToggleFlashlight();
        }
    }

    // Call this from your Pickup Script!
    public void EnableFlashlight()
    {
        hasFlashlight = true;
        if(promptText != null) promptText.gameObject.SetActive(true);
    }

    void ToggleFlashlight()
    {
        // Hide the prompt as soon as they use the flashlight once
        if(promptText != null) promptText.gameObject.SetActive(false);
        isOn = !isOn;
        if (flashlightLight != null) flashlightLight.SetActive(isOn);

        if (audioSource != null)
        {
            AudioClip clipToPlay = isOn ? soundOn : soundOff;
            if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
        }
    }
}