using UnityEngine;

public class LeaveCornfield : MonoBehaviour
{
    public void EscapeMaze(bool rescuedCliffard)
    {
        // Add your exact win/escape logic here (like loading different scenes)
        if (rescuedCliffard)
        {
            Debug.Log("You successfully escaped the corn maze WITH Cliffard! (Good Ending)");
            // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("GoodEnding");
        }
        else
        {
            Debug.Log("You escaped the corn maze... but left Cliffard behind. (Bad Ending)");
            // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("BadEnding");
        }
    }
}
