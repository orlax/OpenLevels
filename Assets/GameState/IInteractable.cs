using UnityEngine;

public interface IInteractable
{
    public string label { get; }
    public int priority { get; }
    public void Interact();    
}
