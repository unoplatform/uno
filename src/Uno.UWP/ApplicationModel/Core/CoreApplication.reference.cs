#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding

using System;
using System.Collections.Generic;
using Uno.Helpers.Theming;

namespace Windows.ApplicationModel.Core;

/// <summary>
/// Enables apps to handle state changes, manage windows, and integrate with a variety of UI frameworks.
/// </summary>
public static partial class CoreApplication
{
	internal static void QueueInvalidateRender()
		=> throw new NotSupportedException();

	internal static void SetInvalidateRender(Action invalidateRender)
		=> throw new NotSupportedException();
}
