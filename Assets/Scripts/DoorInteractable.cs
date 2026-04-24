using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    [Tooltip("Every door needs a unique ID! (e.g., 'KitchenDoor', 'CellarDoor')")]
    public string uniqueID;

    public bool isOpen = false;
    public float openRotation = 90f;
    public float smooth = 2f;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip doorClip;
    public float openPitch = 1.0f;
    public float closePitch = 0.8f;

    private Quaternion targetRotation;
    private Quaternion defaultRotation;

    void Start()
    {
        defaultRotation = transform.rotation;
        targetRotation = defaultRotation;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Assign the clip to the source once at the start
        if (audioSource != null && doorClip != null)
            audioSource.clip = doorClip;

        // --- PERSISTENCE LOGIC ADDED HERE ---
        // 1. When the scene loads, check if the GameManager remembers this specific door
        if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            isOpen = GameManager.Instance.GetObjectState(uniqueID, false);

            if (isOpen == true)
            {
                // Calculate exactly where the door should be if it's open
                targetRotation = defaultRotation * Quaternion.Euler(0, openRotation, 0);
                
                // SNAP the door's actual physical rotation instantly so the player doesn't see it moving
                transform.rotation = targetRotation;
            }
        }
    }

    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smooth);
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        targetRotation = isOpen ? defaultRotation * Quaternion.Euler(0, openRotation, 0) : defaultRotation;

        PlayDoorSound();

        // --- PERSISTENCE LOGIC ADDED HERE ---
        // 2. Whenever the player opens or closes the door, tell the GameManager to remember the new state
        if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            GameManager.Instance.SaveObjectState(uniqueID, isOpen);
        }
    }

    private void PlayDoorSound()
    {
        if (audioSource == null || doorClip == null) return;

        // 1. Stop the current sound immediately
        audioSource.Stop();

        // 2. Adjust the pitch for the new state
        audioSource.pitch = isOpen ? openPitch : closePitch;

        // 3. Play from the beginning
        audioSource.Play();
    }
}