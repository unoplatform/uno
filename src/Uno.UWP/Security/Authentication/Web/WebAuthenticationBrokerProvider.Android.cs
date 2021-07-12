#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.System;
using Android.App;
using Android.OS;
using Uno.UI;

namespace Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		private static string[]? _schemes;

		protected virtual IEnumerable<string> GetApplicationCustomSchemes()
		{
			if (_schemes == null)
			{
				var appType = ContextHelper.Current.GetType();
				var applicationTypes = appType.Assembly.GetTypes();

				static IEnumerable<string> ExtractSchemes(IntentFilterAttribute a)
				{
					if (a.DataSchemes != null)
					{
						// If DataSchemes is defined, it will be used
						// instead of DataScheme (the singular version)
						foreach (var ds in a.DataSchemes)
						{
							yield return ds;
						}
					}
					else if (!string.IsNullOrWhiteSpace(a.DataScheme))
					{
						// When DataScheme (singular version) is used.
						yield return a.DataScheme!;
					}
				}

				_schemes = applicationTypes
					// Check all types deriving from Activity
					.Where(t => t.BaseType != null && (t.BaseType == typeof(WebAuthenticationBrokerActivityBase) || t.BaseType.IsSubclassOf(typeof(WebAuthenticationBrokerActivityBase))))
					// Extract "IntentFilter" attributes
					.SelectMany(t => t.GetCustomAttributes(typeof(IntentFilterAttribute), true))
					.OfType<IntentFilterAttribute>()
					// Extract Data Schemes
					.SelectMany(ExtractSchemes)
					// Add ":" to create a scheme prefix
					.Select(s => s + ":")
					// Remove duplicates
					.Distinct()
					// Materialize the results
					.ToArray();
			}

			return _schemes;
		}

		internal static void SetReturnData(Uri data)
		{
			if (_waitingForCallbackUri == null || _completionSource == null)
			{
				return; // Not waiting for this
			}

			if (data.OriginalString.StartsWith(_waitingForCallbackUri))
			{
				_completionSource?.TrySetResult(
					new WebAuthenticationResult(data.OriginalString, 0, WebAuthenticationStatus.Success));
			}
		}

		internal static void OnMainActivityResumed()
		{
			// This is called when the application activity is resumed.
			// If it occurs before the TCS is competed, it means the user canceled the operation.

			Interlocked.Exchange(ref _completionSource, null)
				?.TrySetResult(new WebAuthenticationResult(null, 0, WebAuthenticationStatus.UserCancel));
		}

		private static string? _waitingForCallbackUri;

		private static TaskCompletionSource<WebAuthenticationResult>? _completionSource;

		protected virtual async Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			var tcs = new TaskCompletionSource<WebAuthenticationResult>();
			Interlocked.Exchange(ref _completionSource, tcs)?.TrySetCanceled();

			_waitingForCallbackUri = callbackUri.OriginalString;

			await LaunchBrowserCore(options, requestUri, callbackUri, ct);

			var result = await tcs!.Task;

			if(Interlocked.CompareExchange(ref _completionSource, null!, tcs) == tcs)
			{
				_waitingForCallbackUri = null;
			}

			return result;
		}

		protected virtual async Task LaunchBrowserCore(WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			await Launcher.LaunchUriAsync(requestUri);
		}
	}
}
