#nullable enable

using System;
using System.Collections.Generic;
using Uno.Helpers.Theming;

namespace Windows.ApplicationModel.Core;

/// <summary>
/// Enables apps to handle state changes, manage windows, and integrate with a variety of UI frameworks.
/// </summary>
public static partial class CoreApplication
{
	internal static void SetContinuousRender(object? visual, bool enabled)
		=> throw new NotSupportedException();

	internal static void QueueInvalidateRender(object? visual)
		=> throw new NotSupportedException();

	internal static void SetInvalidateRender(Action<object?> invalidateRender, Action<object?, bool>? setContinuousRender)
		=> throw new NotSupportedException();
}
