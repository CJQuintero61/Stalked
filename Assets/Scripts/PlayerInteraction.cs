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

    // REMOVED the local booleans from here. 
    // They now live in GameManager.Instance.
    [Header("State")]
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
                    // CHANGED: Check the GameManager for the key, not the local variable
                    bool success = cellar.TryOpenCellar(GameManager.Instance.hasCellarKey);
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
                        HandlePickUp(item.itemName, item);

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

                        if (isInTriggerZone == false)
                        {
                            UpdateUI(false, "");
                        }
                    }
                }
            }
            else
            {
                if (isInTriggerZone == false)
                {
                    UpdateUI(false, "");
                }
            }
        }
        else
        {
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

    // CHANGED: Added the ItemObject reference so we can register the pickup with the PersistentManager
    void HandlePickUp(string itemName, ItemObject itemRef)
    {
        // 1. Update the boolean in the GameManager
        if (itemName == "Collar")
        {
            GameManager.Instance.hasCollar = true;
        }
        else if(itemName == "Cellar Key")
        {
            GameManager.Instance.hasCellarKey = true;
            ObjectiveManager.Instance.UpdateObjective("Head Back to the Cellar");
        }
        else if(itemName == "Cliffard")
        {
            GameManager.Instance.hasCliffard = true;
            ObjectiveManager.Instance.UpdateObjective("Leave the Cellar");
        }
        else if (itemName == "Flashlight")
        {
            GameManager.Instance.hasFlashlight = true;
            ObjectiveManager.Instance.UpdateObjective("Find Cliffard");

            FlashlightController fc = GetComponent<FlashlightController>();
            if (fc != null)
            {
                fc.EnableFlashlight(); 
            }
        }

        // 2. Tell the specific scene object to save its destroyed state
        PersistentSceneObject pso = itemRef.GetComponent<PersistentSceneObject>();
        if (pso != null)
        {
            pso.RegisterPickup();
        }
        else
        {
            Debug.LogWarning($"Item {itemName} is missing a PersistentSceneObject script!");
        }
    }
    
    void UpdateUI(bool state, string message)
    {
        if (interactPanel != null) interactPanel.SetActive(state);
        if (promptText != null) promptText.text = message;
    }
}