#nullable enable

using System;
using System.Diagnostics;
using Uno.Foundation.Logging;
using Uno.ApplicationModel.Core;
using Uno.Foundation.Extensibility;

namespace Windows.ApplicationModel.Core;

partial class CoreApplication
{
	private static Action<object?>? _invalidateRender;
	private static Action<object?, bool>? _setContinuousRender;
	private static ICoreApplicationExtension? _coreApplicationExtension;

	static partial void InitializePlatform()
	{
		ApiExtensibility.CreateInstance(typeof(CoreApplication), out _coreApplicationExtension);
	}

	private static void ExitPlatform()
	{
		if (_coreApplicationExtension != null && _coreApplicationExtension.CanExit)
		{
			_coreApplicationExtension.Exit();
		}
		else
		{
			if (typeof(CoreApplication).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(CoreApplication).Log().LogWarning("This platform does not support application exit.");
			}
		}
	}

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
