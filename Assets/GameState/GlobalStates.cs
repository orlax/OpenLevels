// Somewhere in your initialization code
public static class GlobalStates
{
    public static GameState<string> message = new GameState<string>();
    public static GameState<IInteractable> interactable = new GameState<IInteractable>();
}