#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls.Extensions;

internal readonly record struct ImeSessionActivation(
	FocusState FocusState,
	bool IsSoftwareKeyboardSuppressed);

[Flags]
internal enum ImeSessionUpdate
{
	None = 0,
	CandidateWindowAlignment = 1,
	InputScope = 2,
	TextPrediction = 4,
	TextAndSelection = 8,
	AcceptsReturn = 16,
	SpellCheck = 32,
}

internal sealed class ImeCandidateWindowBoundsChangedEventArgs : EventArgs
{
	internal ImeCandidateWindowBoundsChangedEventArgs(Windows.Foundation.Rect bounds) => Bounds = bounds;

	internal Windows.Foundation.Rect Bounds { get; }
}
