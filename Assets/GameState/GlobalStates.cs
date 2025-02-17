// Somewhere in your initialization code
public static class GlobalStates
{
    public static GameState<string> message = new GameState<string>();
    
    public static GameState<IInteractable> interactable = new GameState<IInteractable>();
    /// <summary>
    /// Sets the current interactable only if the new one has a priority equal or higher than the current one.
    /// Passing null will clear the current interactable.
    /// </summary>
    public static void SetInteractable(IInteractable newInteractable)
    {
        // If we're clearing the interactable, just do it.
        if (newInteractable == null)
        {
            interactable.Value = null;
            return;
        }
        
        // If there's no current interactable, assign the new one.
        if (interactable.Value == null)
        {
            interactable.Value = newInteractable;
            return;
        }
        
        // Compare priorities; if the new one is equal or higher, set it.
        if (newInteractable.priority >= interactable.Value.priority)
        {
            interactable.Value = newInteractable;
        }
    }
}