using System;
using System.Collections.Generic;
using System.Text;
using Uno.Foundation.Logging;
using Windows.UI.Xaml;

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

	}
}
