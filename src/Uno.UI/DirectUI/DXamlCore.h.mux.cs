namespace DirectUI;

partial class DXamlCore
{
	public enum State
	{
		Initializing,
		InitializationFailed,
		Initialized,
		Deinitializing,
		Deinitialized,
		Idle,
	}

	public State GetState() => _state;

	private State _state = State.Deinitialized;
}
