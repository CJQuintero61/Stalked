using UnityEngine;

public class MazePlayerRelay : MonoBehaviour
{
    private MazeGameManager manager;

    void Start()
    {
        manager = FindFirstObjectByType<MazeGameManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        MazeTrigger trigger = other.GetComponent<MazeTrigger>();
        if (trigger != null && manager != null)
        {
            manager.OnPlayerEnterTrigger(trigger.isStart);
        }
    }

    void OnTriggerExit(Collider other)
    {
        MazeTrigger trigger = other.GetComponent<MazeTrigger>();
        if (trigger != null && manager != null)
        {
            manager.OnPlayerExitTrigger(trigger.isStart);
        }
    }
}