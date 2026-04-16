using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 5f;
    public LayerMask interactLayer;
    
    [Header("UI Settings")]
    public GameObject interactPanel; 
    public TextMeshProUGUI promptText; 

    [Header("Booleans")]
    public bool hasCollar = false;
    public bool hasFlashlight = false;
    public bool hasCellarKey = false;
    public bool hasCliffard = false;
    public bool isInTriggerZone = false;

    private bool isShowingMessage = false;

void Update()
    {
        if (isShowingMessage == true) return; 

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            DoorInteractable door = hit.collider.GetComponentInParent<DoorInteractable>();
            CellarInteractable cellar = hit.collider.GetComponentInParent<CellarInteractable>();
            ItemObject item = hit.collider.GetComponentInParent<ItemObject>();

            if (door != null)
            {
                UpdateUI(true, "Press [E] to use Door");
                if (Keyboard.current.eKey.wasPressedThisFrame) door.ToggleDoor();
            }
            else if (cellar != null)
            {
                if (cellar.isLocked == true)
                {
                    UpdateUI(true, "Press [E] to open cellar");
                }
                else
                {
                    UpdateUI(true, "Press [E] to enter cellar");
                }
                
                if (Keyboard.current.eKey.wasPressedThisFrame) 
                {
                    bool success = cellar.TryOpenCellar(hasCellarKey);
                    if (success == false)
                    {
                        StartCoroutine(ShowTemporaryMessage("It's locked. Find a key.", 2f));
                        ObjectiveManager.Instance.UpdateObjective("Find Key to Cellar");
                    }
                }
            }
            else if (item != null)
            {
                bool isEarlyCellarKey = item.itemName == "Cellar Key" && 
                                       (ObjectiveManager.Instance == null || 
                                        ObjectiveManager.Instance.objectiveText.text != "Find Key to Cellar");

                if (isEarlyCellarKey == true)
                {
                    // FIXED: Only hide the UI if we are NOT in the exit zone
                    if (isInTriggerZone == false)
                    {
                        UpdateUI(false, "");
                    }
                }
                else
                {
                    UpdateUI(true, "Press [E] to pick up " + item.itemName);
                    
                    if (Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        HandlePickUp(item.itemName);

                        if (item.itemAudioSource != null)
                        {
                            item.itemAudioSource.Play();

                            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                            foreach (Renderer r in renderers) r.enabled = false;

                            Collider[] colliders = item.GetComponentsInChildren<Collider>();
                            foreach (Collider c in colliders) c.enabled = false;

                            Destroy(item.gameObject, item.itemAudioSource.clip.length);
                        }
                        else
                        {
                            Destroy(item.gameObject);
                        }

                        // FIXED: Only hide the UI if we are NOT in the exit zone
                        if (isInTriggerZone == false)
                        {
                            UpdateUI(false, "");
                        }
                    }
                }
            }
            else
            {
                // FIXED: Only hide the UI if we are NOT in the exit zone
                if (isInTriggerZone == false)
                {
                    UpdateUI(false, "");
                }
            }
        }
        else
        {
            // FIXED: Only hide the UI if we are NOT in the exit zone
            if (isInTriggerZone == false)
            {
                UpdateUI(false, "");
            }
        }
    }

    IEnumerator ShowTemporaryMessage(string message, float duration)
    {
        isShowingMessage = true; 
        UpdateUI(true, message); 
        yield return new WaitForSeconds(duration); 
        isShowingMessage = false; 
    }

    void HandlePickUp(string itemName)
    {
        if (itemName == "Collar")
        {
            hasCollar = true;
        }
        else if(itemName == "Cellar Key")
        {
            hasCellarKey = true;
            ObjectiveManager.Instance.UpdateObjective("Head Back to the Cellar");
        }
        else if(itemName == "Cliffard")
        {
            hasCliffard = true;
            ObjectiveManager.Instance.UpdateObjective("Leave the Cellar");
        }
        else if (itemName == "Flashlight")
        {
            hasFlashlight = true;
            ObjectiveManager.Instance.UpdateObjective("Find Cliffard");

            FlashlightController fc = GetComponent<FlashlightController>();
            if (fc != null)
            {
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