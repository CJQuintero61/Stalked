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
                    }
                }
            }
            else if (item != null)
            {
                UpdateUI(true, "Press [E] to pick up " + item.itemName);
                
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    HandlePickUp(item.itemName);

                    // NEW LOGIC: Play the item's Audio Source
                    if (item.itemAudioSource != null)
                    {
                        item.itemAudioSource.Play();

                        // 1. Turn off all 3D meshes so it looks like it disappeared
                        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
                        foreach (Renderer r in renderers) r.enabled = false;

                        // 2. Turn off all colliders so we can't click it again
                        Collider[] colliders = item.GetComponentsInChildren<Collider>();
                        foreach (Collider c in colliders) c.enabled = false;

                        // 3. Destroy the object ONLY after the audio clip finishes playing
                        Destroy(item.gameObject, item.itemAudioSource.clip.length);
                    }
                    else
                    {
                        // If there is no audio source, just destroy it instantly like normal
                        Destroy(item.gameObject);
                    }

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
            ObjectiveManager.Instance.UpdateObjective("Find the Cellar");
        }
        else if(itemName == "Cliffard")
        {
            hasCliffard = true;
        }
        else if (itemName == "Flashlight")
        {
            hasFlashlight = true;

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