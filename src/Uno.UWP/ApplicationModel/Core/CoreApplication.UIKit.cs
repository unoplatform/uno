#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Core;

partial class CoreApplication
{
	private static Action<object?> _invalidateRender;

	internal static void SetInvalidateRender(Action<object?> invalidateRender)
		// Currently we don't support multi-windowing, so we invalidate all XamlRoots
		=> _invalidateRender ??= invalidateRender;

	internal static void QueueInvalidateRender(object? visual)
		=> _invalidateRender?.Invoke(visual);
}
