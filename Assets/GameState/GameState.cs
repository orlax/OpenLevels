using System;
using System.Collections.Generic;

public class GameState<T>
{
    public event Action<T> OnStateChanged;
    private T _value;

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                OnStateChanged?.Invoke(_value);
            }
        }
    }
}