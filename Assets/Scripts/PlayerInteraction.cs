using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 5f;
    public LayerMask interactLayer;
    
    [Header("UI Settings")]
    public GameObject interactPanel; // The "Press E" UI container
    public TextMeshProUGUI promptText; // The text component inside the container

    [Header("Booleans")]
    public bool hasCollar = false;
    public bool hasFlashlight = false;

    void Update()
    {
        // Shoot ray from center of screen (First Person)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            // 1. Check for standard interactables
            DoorInteractable door = hit.collider.GetComponentInParent<DoorInteractable>();
            CellarInteractable cellar = hit.collider.GetComponentInParent<CellarInteractable>();
            
            // 2. Check for the items you want to "Pick Up"
            // We use a simple tag or script name to identify the item
            ItemObject item = hit.collider.GetComponentInParent<ItemObject>();

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
            else if (item != null)
            {
                UpdateUI(true, "Press [E] to pick up " + item.itemName);
                
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    HandlePickUp(item.itemName);
                    Destroy(item.gameObject); // Deletes the item from the scene
                    UpdateUI(false, "");
                }
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

    // This is where your booleans live
void HandlePickUp(string itemName)
{
    if (itemName == "Collar")
    {
        hasCollar = true;
    }
    else if (itemName == "Flashlight")
    {
        hasFlashlight = true;

        // NEW: Tell the FlashlightController script that we found it!
        FlashlightController fc = GetComponent<FlashlightController>();
    if (fc != null)
    {
        // This flips the bool AND shows the UI text at the same time
        fc.EnableFlashlight(); 
    }
    }
}
    

    void UpdateUI(bool state, string message)
    {
        if (interactPanel != null) interactPanel.SetActive(state);
        if (promptText != null) promptText.text = message;
    }
}