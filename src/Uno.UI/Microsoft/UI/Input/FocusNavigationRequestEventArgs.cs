using System;

namespace Microsoft.UI.Input;

/// <summary>
/// Contains event data for the InputFocusController.NavigateFocusRequested and InputFocusNavigationHost.DepartFocusRequested events.
/// </summary>
public partial class FocusNavigationRequestEventArgs
{
	internal FocusNavigationRequestEventArgs(FocusNavigationRequest request)
	{
		Request = request ?? throw new ArgumentNullException(nameof(request));
	}

	/// <summary>
	/// Gets the details for focus navigation event.
	/// </summary>
	public FocusNavigationRequest Request { get; }

	/// <summary>
	/// Gets or sets the result of a focus navigation event.
	/// </summary>
	public FocusNavigationResult Result { get; set; }
}
