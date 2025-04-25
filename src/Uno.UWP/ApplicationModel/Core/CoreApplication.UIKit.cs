#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Core;

partial class CoreApplication
{
	private static Action<object?>? _invalidateRender;
	private static Action<object?, bool>? _setContinuousRender;

	internal static void SetInvalidateRender(Action<object?> invalidateRender, Action<object?, bool>? setContinuousRender)
	{
		Debug.Assert(_invalidateRender is null);
		Debug.Assert(_setContinuousRender is null);

		_invalidateRender ??= invalidateRender;
		_setContinuousRender ??= setContinuousRender;
	}

	internal static void SetContinuousRender(object? visual, bool enabled)
		=> _setContinuousRender?.Invoke(visual, enabled);

	internal static void QueueInvalidateRender(object? visual)
		=> _invalidateRender?.Invoke(visual);
}
