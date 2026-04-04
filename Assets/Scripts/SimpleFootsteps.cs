using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class SimpleFootsteps : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] footstepSounds;
    
    [Header("Walking Settings")]
    public float walkStepDistance = 2.0f; 
    public float walkPitch = 1.0f;

    [Header("Sprinting Settings")]
    public float sprintStepDistance = 1.3f; // Smaller distance = faster sound
    public float sprintPitch = 1.3f;        // Higher pitch makes it sound "lighter/faster"
    public float sprintVolume = 0.8f;

    private float distanceTraveled;
    private Vector3 lastPosition;
    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    void Update()
    {
        // 1. Calculate movement this frame
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        
        // 2. Check if we are currently holding Shift
        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;

        // 3. Only count if moving and on the ground
        if (controller.isGrounded && controller.velocity.magnitude > 0.5f)
        {
            distanceTraveled += distanceMoved;

            // Use the sprint distance if holding shift, otherwise use walk distance
            float currentThreshold = isSprinting ? sprintStepDistance : walkStepDistance;

            if (distanceTraveled >= currentThreshold)
            {
                PlayFootstep(isSprinting);
                distanceTraveled = 0f;
            }
        }

        lastPosition = transform.position;
    }

    void PlayFootstep(bool sprinting)
    {
        if (footstepSounds.Length > 0)
        {
            int index = Random.Range(0, footstepSounds.Length);
            
            // Adjust pitch and volume based on whether we are sprinting
            audioSource.pitch = (sprinting ? sprintPitch : walkPitch) + Random.Range(-0.1f, 0.1f);
            float currentVolume = sprinting ? sprintVolume : 0.5f;

            audioSource.PlayOneShot(footstepSounds[index], currentVolume);
        }
    }
}