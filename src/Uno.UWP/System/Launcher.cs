#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Microsoft.Extensions.Logging;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static async Task<bool> LaunchUriAsync(Uri uri)
		{
			try
			{
#if __MOBILE__
				if ( await HandleSpecialUriAsync(uri))
				{
					return;
				}
#endif
#if __IOS__
				return UIKit.UIApplication.SharedApplication.OpenUrl(new global::Foundation.NSUrl(uri.OriginalString));
#elif __ANDROID__
				var androidUri = global::Android.Net.Uri.Parse(uri.OriginalString);
				var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView, androidUri);

				((Android.App.Activity)Uno.UI.ContextHelper.Current).StartActivity(intent);

				return true;
#elif __WASM__
				var command = $"Uno.UI.WindowManager.current.open(\"{uri.OriginalString}\");";
				var result = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
				return result == "True";
#else
				throw new NotImplementedException();
#endif
			}
			catch (Exception exception)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().Error($"Failed to {nameof(LaunchUriAsync)}.", exception);
				}

				return false;
			}
		}
	}
}
