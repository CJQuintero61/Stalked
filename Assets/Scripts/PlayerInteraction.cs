using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Important for TextMeshPro

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 5f;
    public LayerMask interactLayer = ~0;
    
    [Header("UI Settings")]
    public GameObject interactPanel; // The object that turns on/off
    public TextMeshProUGUI promptText; // The actual text component to change words

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        bool hitSomething = Physics.Raycast(ray, out hit, interactDistance, interactLayer);

        if (hitSomething)
        {
            // Check for both types
            DoorInteractable door = hit.collider.GetComponentInParent<DoorInteractable>();
            CellarInteractable cellar = hit.collider.GetComponentInParent<CellarInteractable>();

            if (door != null)
            {
                UpdateUI(true, "Press [E] to use Door");
                if (Keyboard.current.eKey.wasPressedThisFrame) door.ToggleDoor();
            }
            else if (cellar != null)
            {
                UpdateUI(true, "Press [E] to enter cellar");
                if (Keyboard.current.eKey.wasPressedThisFrame) cellar.OpenCellar();
            }
            else
            {
                UpdateUI(false, "");
            }
        }
        else
        {
            UpdateUI(false, "");
        }
    }

    // Helper method to keep code clean
    void UpdateUI(bool state, string message)
    {
        if (interactPanel != null) interactPanel.SetActive(state);
        if (promptText != null) promptText.text = message;
    }
}