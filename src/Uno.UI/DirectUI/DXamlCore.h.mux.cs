#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

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

	private readonly Dictionary<IntPtr, Window> _handleToDesktopWindowMap = new();

	private State _state = State.Deinitialized;

	private JupiterControl? _control;

	private Window? _uwpWindowNoRef;
}
