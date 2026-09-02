#nullable enable
using Windows.UI.Core;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	#region IInputInjectorTarget
	void IInputInjectorTarget.InjectKeyDown(KeyEventArgs args) => InjectKeyDown(args);
	partial void InjectKeyDown(KeyEventArgs args);

	void IInputInjectorTarget.InjectKeyUp(KeyEventArgs args) => InjectKeyUp(args);
	partial void InjectKeyUp(KeyEventArgs args);

	/// <summary>
	/// Keyboard injection targets the active window, mirroring the foreground-window
	/// behavior of the Windows implementation.
	/// </summary>
	bool IInputInjectorTarget.IsActive
		=> ContentRoot.GetOwnerWindow()?.NativeWrapper?.ActivationState
			is not (null or CoreWindowActivationState.Deactivated);
	#endregion
}
