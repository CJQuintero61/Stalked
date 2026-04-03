using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    public bool isOpen = false;
    public float openRotation = 90f;
    public float smooth = 2f;

    private Quaternion targetRotation;
    private Quaternion defaultRotation;

    void Start()
    {
        defaultRotation = transform.rotation;
        targetRotation = defaultRotation;
    }

    void Update()
    {
        // Smoothly rotate toward the target
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smooth);
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        targetRotation = isOpen ? defaultRotation * Quaternion.Euler(0, openRotation, 0) : defaultRotation;
    }
}