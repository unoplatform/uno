#nullable enable

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

	internal bool IsInitializing => _state == State.Initializing;
	internal bool IsInitialized => _state == State.Initialized;
	public State GetState() => _state;

	private State _state = State.Deinitialized;

	private JupiterControl? _control;
}
