using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify which keyboard shortcuts for formatting are disabled in a RichEditBox.
/// </summary>
[Flags]
public enum DisabledFormattingAccelerators : uint
{
	/// <summary>
	/// No keyboard shortcuts are disabled.
	/// </summary>
	None = 0,

	/// <summary>
	/// The keyboard shortcut for bold (Ctrl+B) is disabled.
	/// </summary>
	Bold = 1,

	/// <summary>
	/// The keyboard shortcut for italic (Ctrl+I) is disabled.
	/// </summary>
	Italic = 2,

	/// <summary>
	/// The keyboard shortcut for underline (Ctrl+U) is disabled.
	/// </summary>
	Underline = 4,

	/// <summary>
	/// All keyboard shortcuts are disabled.
	/// </summary>
	All = 4294967295,
}
