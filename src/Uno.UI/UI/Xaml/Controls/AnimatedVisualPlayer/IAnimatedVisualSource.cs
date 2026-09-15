using System;

namespace Microsoft.UI.Xaml.Controls
{
	// Declared here (with no members) so the sync generator sees the interface as defined by Uno; its
	// only member, WinUI's TryCreateAnimatedVisual (AnimatedVisualPlayer.idl:22-29), stays in the
	// generated partial.
	/// <summary>
	/// An animated Visual that can be used by other objects, such as an AnimatedVisualPlayer.
	/// </summary>
	public partial interface IAnimatedVisualSource
	{
	}

	internal partial interface IAnimatedVisualSourceWithUri
	{
		Uri UriSource { get; set; }
	}
}
