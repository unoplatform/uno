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

	internal static void SetInvalidateRender(Action<object?> invalidateRender)
	{
		Debug.Assert(_invalidateRender is null);
		_invalidateRender ??= invalidateRender;
	}

	internal static void QueueInvalidateRender(object? visual)
		=> _invalidateRender?.Invoke(visual);
}
