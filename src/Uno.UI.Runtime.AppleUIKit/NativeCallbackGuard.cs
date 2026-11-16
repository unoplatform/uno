#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.AppleUIKit;

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
	/// <returns>Whether <paramref name="action"/> completed without throwing.</returns>
	internal static bool Run(Action action, [CallerMemberName] string callback = "")
	{
		try
		{
			action();
			return true;
		}
		catch (Exception ex)
		{
			// Logged as well, since an app that doesn't handle UnhandledException would otherwise see nothing.
			var log = typeof(NativeCallbackGuard).Log();

			if (log.IsEnabled(LogLevel.Error))
			{
				log.Error($"The {callback} callback failed.", ex);
			}

			Application.Current?.RaiseRecoverableUnhandledException(ex);
			return false;
		}
	}
}
