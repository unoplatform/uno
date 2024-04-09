#nullable enable

using System;

namespace Windows.ApplicationModel.Core;

partial class CoreApplication
{
	private static Action<object?> _invalidateRender;

	private static void ExitPlatform() => Android.OS.Process.KillProcess(Android.OS.Process.MyPid());


	internal static void SetInvalidateRender(Action<object?> invalidateRender)
		// Currently we don't support multi-windowing, so we invalidate all XamlRoots
		=> _invalidateRender ??= invalidateRender;

	internal static void QueueInvalidateRender(object? visual)
		=> _invalidateRender?.Invoke(visual);

}
