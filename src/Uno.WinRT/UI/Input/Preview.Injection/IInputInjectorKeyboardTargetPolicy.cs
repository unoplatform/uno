#nullable enable

namespace Windows.UI.Input.Preview.Injection;

// Optional host policy for compositors that do not report native activation.
internal interface IInputInjectorKeyboardTargetPolicy
{
	// Synthetic lifecycle activation need not imply native keyboard focus.
	bool IsActive(IInputInjectorTarget target);

	IInputInjectorTarget? GetFallbackTarget();
}
