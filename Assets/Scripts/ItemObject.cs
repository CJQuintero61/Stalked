using UnityEngine;

public class ItemObject : MonoBehaviour
{
    public string itemName;
    
    // NEW: We now look for the Audio Source attached to this specific item
    public AudioSource itemAudioSource; 
}
