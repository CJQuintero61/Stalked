using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    void Start()
    {
        // 1. Check if we actually have a saved spawn point to go to
        if (SceneMemory.targetSpawnName != "")
        {
            // 2. Search the new scene for an object with that exact name
            GameObject spawnPoint = GameObject.Find(SceneMemory.targetSpawnName);

            if (spawnPoint != null)
            {
                // 3. Temporarily pause physics so the Rigidbody doesn't fight the teleport
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                // 4. Teleport the player to match the position and rotation!
                transform.position = spawnPoint.transform.position;
                transform.rotation = spawnPoint.transform.rotation;

                // 5. Turn physics back on
                if (rb != null) rb.isKinematic = false;
                
                Debug.Log("Successfully spawned at: " + SceneMemory.targetSpawnName);
            }
            else
            {
                Debug.LogWarning("Could not find a spawn point named: " + SceneMemory.targetSpawnName);
            }
            
            // Clear the memory so it doesn't accidentally trigger again if we reload the same level
            SceneMemory.targetSpawnName = "";
        }
    }
}