using System;

public class StateMachine<T> where T : Enum
{
    private T currentState;

    public event Action<T, T> OnStateChanged;

    public T CurrentState
    {
        get => currentState;
        private set
        {
            if (Equals(currentState, value))
                return;

            T oldState = currentState;
            currentState = value;

            OnStateChanged?.Invoke(oldState, currentState);
        }
    }

    public StateMachine(T initialState)
    {
        currentState = initialState;
    }

    public void ChangeState(T newState)
    {
        CurrentState = newState;
    }

    public bool IsState(T state) => Equals(currentState, state);

}
