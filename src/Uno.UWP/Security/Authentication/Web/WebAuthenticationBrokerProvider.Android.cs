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
using Android.Content;
using Windows.Foundation.Metadata;
using AndroidX.Browser.CustomTabs;
using Android.Content.PM;

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

		private Activity? CurrentActivity => ContextHelper.Current as Activity;

		TaskCompletionSource<WebAuthenticationResult>? tcsResponse = null;
		Uri? currentRedirectUri = null;

		public bool OnResumeCallback(Intent? intent = null)
		{
			// If we aren't waiting on a task, don't handle the url
			if (tcsResponse?.Task?.IsCompleted ?? true)
				return false;

			var intentData = intent?.Data?.ToString();
			if (intentData is null ||
				currentRedirectUri is null)
			{
				tcsResponse.TrySetCanceled();
				return false;
			}

			try
			{
				var intentUri = new Uri(intentData);

				// Only handle schemes we expect
				if (!CanHandleCallback(currentRedirectUri, intentUri))
				{
					tcsResponse.TrySetException(new InvalidOperationException($"Invalid Redirect URI, detected `{intentUri}` but expected a URI in the format of `{currentRedirectUri}`"));
					return false;
				}

				tcsResponse?.TrySetResult(new WebAuthenticationResult(intentUri.OriginalString, 0, WebAuthenticationStatus.Success));
				return true;
			}
			catch (Exception ex)
			{
				tcsResponse.TrySetException(ex);
				return false;
			}
		}

		protected virtual async Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			var url = requestUri;
			var callbackUrl = callbackUri;
			var packageName = Application.Context.PackageName;

			// Create an intent to see if the app developer wired up the callback activity correctly
			var intent = new Intent(Intent.ActionView);
			intent.AddCategory(Intent.CategoryBrowsable);
			intent.AddCategory(Intent.CategoryDefault);
			intent.SetPackage(packageName);
			intent.SetData(global::Android.Net.Uri.Parse(callbackUrl.OriginalString));

			// Try to find the activity for the callback intent
			if (!IsIntentSupported(intent, packageName))
				throw new InvalidOperationException($"You must subclass the `{nameof(WebAuthenticationBrokerActivityBase)}` and create an IntentFilter for it which matches your `{nameof(callbackUrl)}`.");

			// Cancel any previous task that's still pending
			if (tcsResponse?.Task != null && !tcsResponse.Task.IsCompleted)
				tcsResponse.TrySetCanceled();

			tcsResponse = new TaskCompletionSource<WebAuthenticationResult>();
			currentRedirectUri = callbackUrl;

			if (!(await StartCustomTabsActivity(url)))
			{
				// Fall back to opening the system-registered browser if necessary
				var urlOriginalString = url.OriginalString;
				var browserIntent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(urlOriginalString));
				WebAuthenticationBrokerRedirectActivity.StartActivity(CurrentActivity!, browserIntent);
			}

			return await tcsResponse.Task;
		}

		private async Task<bool> StartCustomTabsActivity(Uri url)
		{
			// Is only set to true if BindServiceAsync succeeds and no exceptions are thrown
			var success = false;
			var parentActivity = CurrentActivity;

			var customTabsActivityManager = CustomTabsActivityManager.From(parentActivity);
			try
			{
				if (await BindServiceAsync(customTabsActivityManager))
				{
					var customTabsIntent = new CustomTabsIntent.Builder(customTabsActivityManager.Session)
						.SetShowTitle(true)
						.Build();

					customTabsIntent.Intent.SetData(global::Android.Net.Uri.Parse(url.OriginalString));

					if (parentActivity?.PackageManager is { } packageManager &&
						customTabsIntent.Intent.ResolveActivity(packageManager) != null)
					{
						WebAuthenticationBrokerRedirectActivity.StartActivity(parentActivity, customTabsIntent.Intent);
						success = true;
					}
				}
			}
			finally
			{
				try
				{
					customTabsActivityManager.Client?.Dispose();
				}
				finally
				{
				}
			}

			return success;
		}

		static Task<bool> BindServiceAsync(CustomTabsActivityManager manager)
		{
			var tcs = new TaskCompletionSource<bool>();

			manager.CustomTabsServiceConnected += OnCustomTabsServiceConnected;

			if (!manager.BindService())
			{
				manager.CustomTabsServiceConnected -= OnCustomTabsServiceConnected;
				tcs.TrySetResult(false);
			}

			return tcs.Task;

			void OnCustomTabsServiceConnected(ComponentName name, CustomTabsClient client)
			{
				manager.CustomTabsServiceConnected -= OnCustomTabsServiceConnected;
				tcs.TrySetResult(true);
			}
		}

		internal static bool IsIntentSupported(Intent intent, string? expectedPackageName)
		{
			if (Application.Context is not Context ctx || ctx.PackageManager is not PackageManager pm)
				return false;

			return intent.ResolveActivity(pm) is ComponentName c && c.PackageName == expectedPackageName;
		}

		internal static bool CanHandleCallback(Uri expectedUrl, Uri callbackUrl)
		{
			if (!callbackUrl.Scheme.Equals(expectedUrl.Scheme, StringComparison.OrdinalIgnoreCase))
				return false;

			if (!string.IsNullOrEmpty(expectedUrl.Host))
			{
				if (!callbackUrl.Host.Equals(expectedUrl.Host, StringComparison.OrdinalIgnoreCase))
					return false;
			}

			return true;
		}
	}
}
