#nullable enable

using System;
using System.Linq;

namespace Uno.UI.Xaml.Core;

internal enum PointerCaptureResult
{
	/// <summary>
	/// The capture has been added for the given element.
	/// </summary>
	Added,

	/// <summary>
	/// The pointer has already been captured with the same kind by the given element.
	/// </summary>
	AlreadyCaptured,

	/// <summary>
	/// The pointer has already been captured by another element,
	/// or it cannot be captured at this time (pointer not pressed).
	/// </summary>
	Failed,
}
