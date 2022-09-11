using System;

namespace Windows.UI.Popups;

/// <summary>
/// Specifies less frequently used options for a MessageDialog.
/// </summary>
[Flags]
public enum MessageDialogOptions : uint
{
	/// <summary> 
	/// No options are specified and default behavior is used.
	/// </summary>
	None = 0U,

	/// <summary>
	/// Ignore user input for a short period. This enables browsers to defend against clickjacking.
	/// </summary>
	AcceptUserInputAfterDelay = 1U
}
