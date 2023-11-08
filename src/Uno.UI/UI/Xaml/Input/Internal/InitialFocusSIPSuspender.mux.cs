using System;
using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input;

public class InitialFocusSIPSuspender : IDisposable
{
	private readonly FocusManager _focusManager;
	
	public InitialFocusSIPSuspender(FocusManager focusManager)
	{
		_focusManager = focusManager;
		_focusManager.InitialFocus = true;
	}

	public void Dispose() => _focusManager.InitialFocus = false;
}
