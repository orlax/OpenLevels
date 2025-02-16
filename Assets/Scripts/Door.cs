using UnityEngine;

// In your Door component:
public class Door : MonoBehaviour, IInteractable
{
    public bool IsOpen = false;

    public string actionLabel = "(E) Open Door";
    public string label => actionLabel;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // Set interactable state (could be another state system)
            // For simplicity, assume we're just updating UI via a global state.
            GlobalStates.interactable.Value = this;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player") && GlobalStates.interactable.Value == this)
        {
            // Set interactable state (could be another state system)
            // For simplicity, assume we're just updating UI via a global state.
            GlobalStates.interactable.Value = null;
        }
    }

    public void Interact()
    {
        if (IsOpen)
        {
            GlobalStates.message.Value = "Aun no se ha implementado";
        }
        else
        {
            GlobalStates.message.Value = "La Puerta esta Cerrada";
        }
    }
}