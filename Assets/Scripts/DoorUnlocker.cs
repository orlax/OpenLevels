using UnityEngine;

public class DoorUnlocker : MonoBehaviour, IInteractable
{
    public string actionLabel = "(E) Unlock Door";
    public string label => actionLabel;

    public int priority => 2;

    // Reference to the door that this object unlocks.
    public Door doorToUnlock;


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // Assuming your global interaction system selects the interactable
            // with the highest priority.
            GlobalStates.SetInteractable(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player") && GlobalStates.interactable.Value == this)
        {
            GlobalStates.interactable.Value = null;
        }
    }

    public void Interact()
    {
        if (doorToUnlock == null)
        {
            Debug.LogWarning("UnlockDoor: No door assigned to unlock!");
            return;
        }
        
        // Unlock the door by setting its IsOpen property.
        doorToUnlock.IsOpen = true;
        GlobalStates.message.Value = "Door Unlocked!";
        
        GlobalStates.SetInteractable(null); // Clear the interactable
        GlobalStates.SetInteractable(doorToUnlock);// Set the door as the new interactable.

        Destroy(gameObject); // Remove the key from the scene.
    }
}
