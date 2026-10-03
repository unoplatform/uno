#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Runs work invoked from a UIKit callback, reporting rather than propagating any exception.
/// </summary>
/// <remarks>
/// A managed exception unwinding into Objective-C terminates the process, so every UIKit entry
/// point has to absorb one. That matters most where the callback invokes app code, such as an
/// <see cref="Microsoft.Windows.AppLifecycle.AppInstance.Activated"/> handler.
/// </remarks>
internal static class NativeCallbackGuard
{
	internal static void Run(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Application.Current?.RaiseRecoverableUnhandledException(ex);
		}
	}
}
