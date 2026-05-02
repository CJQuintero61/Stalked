using UnityEngine;

[RequireComponent(typeof(BoxCollider))] // This ensures the script has a collider to work with
public class PersistentTrigger : MonoBehaviour
{
    [Tooltip("Every checkpoint needs a unique ID! (e.g., 'HallwayJumpScare', 'CellarObjectiveUpdate')")]
    public string uniqueID;

    private void Start()
    {
        // 1. Check the GameManager's checklist as soon as the room loads
        if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            bool wasTriggered = GameManager.Instance.GetObjectState(uniqueID, false);

            if (wasTriggered == true)
            {
                // 2. If this already happened, completely destroy this trigger object
                // so the player can safely walk here without it firing again.
                Destroy(gameObject); 
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing walking into the box is actually the player
        if (other.CompareTag("Player"))
        {
            // ==========================================
            // YOUR CUSTOM CHECKPOINT LOGIC GOES HERE!
            // e.g., ObjectiveManager.Instance.UpdateObjective("...");
            // e.g., audioSource.Play();
            // ==========================================

            // 3. Tell the GameManager to remember that this checkpoint was crossed
            if (GameManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
            {
                GameManager.Instance.SaveObjectState(uniqueID, true);
            }

            // 4. Destroy this object so it doesn't trigger a second time 
            // while the player is still standing in the current scene
            Destroy(gameObject);
        }
    }
}
