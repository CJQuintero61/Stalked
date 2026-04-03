using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 5f; // Increased distance for testing
    public LayerMask interactLayer = ~0; // This sets it to "Everything" by default
    public GameObject interactUI;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Visual helper: Red if hitting nothing, Green if hitting the door
        bool hitSomething = Physics.Raycast(ray, out hit, interactDistance, interactLayer);
        Debug.DrawRay(transform.position, transform.forward * interactDistance, hitSomething ? Color.green : Color.red);

        if (hitSomething)
        {
            // This looks for the script on the object HIT, or any of its PARENTS
            DoorInteractable door = hit.collider.GetComponentInParent<DoorInteractable>();

            if (door != null)
            {
                if (interactUI != null) interactUI.SetActive(true);

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    door.ToggleDoor();
                }
            }
            else
            {
                if (interactUI != null) interactUI.SetActive(false);
            }
        }
        else
        {
            if (interactUI != null) interactUI.SetActive(false);
        }
    }
}