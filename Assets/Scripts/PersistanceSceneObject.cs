using UnityEngine;

public class PersistentSceneObject : MonoBehaviour
{
    [Tooltip("CRITICAL: Every item in the scene must have a completely unique ID (e.g., 'CellarKey_MainRoom', 'Note_Desk').")]
    public string uniqueID;

    private void Start()
    {
        // When the game scene reloads, ask the GameManager if this specific item was already picked up
        if (GameManager.Instance != null)
        {
            bool wasPickedUp = GameManager.Instance.GetObjectState(uniqueID, false);

            if (wasPickedUp)
            {
                // If we already grabbed it before going to the cellar, destroy it so it doesn't duplicate
                Destroy(gameObject); 
            }
        }
    }

    // Call this method right before you Destroy() the item during gameplay when the player collects it
    public void RegisterPickup()
    {
        if (GameManager.Instance != null)
        {
            // Tell the GameManager to remember this ID as 'true' (picked up)
            GameManager.Instance.SaveObjectState(uniqueID, true);
        }
    }
}
