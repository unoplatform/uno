using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;

namespace Uno.Extensions
{
	public static class ApplicationExtensions
	{
		internal static void RaiseRecoverableUnhandledExceptionOrLog(this Application application, Exception e, object sender)
		{
			if (application != null)
			{
				application.RaiseRecoverableUnhandledException(e);
			}
			else
			{
				sender.GetType().Log().LogError("Unhandled exception", e);
			}
		}

		/// <summary>
		/// Calls <see cref="Application.RaiseUnhandledException"/> when an <see cref="Application"/>
		/// is available; otherwise logs the exception and re-throws it preserving the original
		/// stack trace. The exception is always re-thrown unless an
		/// <see cref="Application.UnhandledException"/> handler marks it as handled.
		/// </summary>
		internal static void RaiseUnhandledExceptionOrThrow(this Application application, Exception e, object sender)
		{
			if (application != null)
			{
				application.RaiseUnhandledException(e);
			}
			else
			{
				sender.GetType().Log().LogError("Unhandled exception (no Application available)", e);
				ExceptionDispatchInfo.Capture(e).Throw();
			}
		}
	}
}
