using UnityEngine;

public class PlayerTeleporter : MonoBehaviour
{
    void Start()
    {
        // Check if the Maze Manager requested a specific teleport
        if (MazeGameManager.ShouldTeleport)
        {
            // Move the player to the coordinates specified in the previous scene
            transform.position = MazeGameManager.GlobalSpawnPoint;
            
            // Reset the flag so we don't teleport again by accident later
            MazeGameManager.ShouldTeleport = false;
            
            Debug.Log("Player teleported to: " + transform.position);
        }
    }
}