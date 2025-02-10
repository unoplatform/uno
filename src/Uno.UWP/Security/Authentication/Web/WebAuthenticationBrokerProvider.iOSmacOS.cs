#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using AuthenticationServices;
using Foundation;
using Windows.UI.Core;
using Uno.Extensions;
using UIKit;
using SafariServices;

namespace Uno.AuthenticationBroker
{
	partial class WebAuthenticationBrokerProvider
	{
		private const string asWebAuthenticationSessionErrorDomain = "com.apple.AuthenticationServices.WebAuthenticationSession";
		private const int asWebAuthenticationSessionErrorCodeCanceledLogin = 1;
#if __IOS__
		private const string sfAuthenticationErrorDomain = "com.apple.SafariServices.Authentication";
		private const int sfAuthenticationErrorCanceledLogin = 1;
#endif
		protected virtual async Task<WebAuthenticationResult> AuthenticateAsyncCore(
			WebAuthenticationOptions options,
			Uri requestUri,
			Uri callbackUri,
			CancellationToken ct)
		{
			var tcs = new TaskCompletionSource<WebAuthenticationResult>();

			void AuthSessionCallback(NSUrl? callbackUrl, NSError? error)
			{
				if (error != null)
				{
					if ((error.Domain == asWebAuthenticationSessionErrorDomain &&
						error.Code == asWebAuthenticationSessionErrorCodeCanceledLogin)
#if __IOS__
						|| (error.Domain == sfAuthenticationErrorDomain &&
							error.Code == sfAuthenticationErrorCanceledLogin)
#endif
						)
					{
						tcs.SetResult(new WebAuthenticationResult(
							null,
							0,
							WebAuthenticationStatus.UserCancel));
					}
					tcs.SetResult(new WebAuthenticationResult(
						error.ToString(),
						0,
						WebAuthenticationStatus.ErrorHttp));
				}
				else
				{
					tcs.SetResult(new WebAuthenticationResult(
						callbackUrl?.AbsoluteString,
						0,
						WebAuthenticationStatus.Success));
				}
			}

			IDisposable? was = default;

			void Cancel()
			{
				var w = was;
				if (w is ASWebAuthenticationSession aswas)
				{
					aswas.Cancel();
				}
#if __IOS__
				else if (w is SFAuthenticationSession sfwas)
				{
					sfwas.Cancel();
				}
#endif

				w?.Dispose();
				was = null;
			}

			await using var x = ct.Register(Cancel);

			var startUrl = new NSUrl(requestUri.OriginalString);
			var callbackUrl = callbackUri.OriginalString;

			var schemes = GetApplicationCustomSchemes().ToArray();
			if (!schemes.Any(s => callbackUrl.StartsWith(s, StringComparison.Ordinal)))
			{
				var message = schemes.Length == 0
					? "No schemes defined in info.plist. You must define a custom scheme (CFBundleURLSchemes) for your callback url."
					: $"No schemes defined for callback url {callbackUrl}. Defined schemes are: {schemes.JoinBy(" ")}";
				throw new InvalidOperationException(message);
			}

			// Both ASWebAuthenticationSession and SFAuthenticationSession accept a scheme
			// as the second parameter, not a full url. This will cause issues if the callbackuri
			// is a fully defined url
			var scheme = callbackUri.Scheme;

#if __IOS__
			if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
#else
			var osVersion = NSProcessInfo.ProcessInfo.OperatingSystemVersion;
			if (new Version((int)osVersion.Major, (int)osVersion.Minor) >= new Version(10, 15))
#endif
			{
				var aswas = new ASWebAuthenticationSession(startUrl, callbackUrlScheme: scheme, AuthSessionCallback);
				was = aswas;

#if __IOS__
				if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
#endif
				{
					aswas.PresentationContextProvider = new PresentationContextProviderToSharedKeyWindow();
					aswas.PrefersEphemeralWebBrowserSession =
						WinRTFeatureConfiguration.WebAuthenticationBroker.PrefersEphemeralWebBrowserSession;
				}

				var dispatcher = CoreDispatcher.Main;
				if (dispatcher.HasThreadAccess)
				{
					Start();
				}
				else
				{
					var t = dispatcher.RunAsync(CoreDispatcherPriority.Normal, Start);
				}

				void Start()
				{
					if (!aswas.Start())
					{
						tcs.TrySetException(new InvalidOperationException("Unable to start authentication"));
					}
				}
			}
#if __IOS__
			else
			{
				if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
				{
					var sfwas = new SFAuthenticationSession(startUrl, callbackUrlScheme: scheme, AuthSessionCallback);
					was = sfwas;

					if (!sfwas.Start())
					{
						tcs.TrySetException(new InvalidOperationException("Unable to start authentication"));
					}
				}

				else
				{
					throw new InvalidOperationException("iOS v11+ is required for this implementation of WebAuthenticationBroker.");
				}
			}
#endif

			try
			{
				return await tcs.Task;
			}
			finally
			{
				Cancel();
			}
		}

		private class PresentationContextProviderToSharedKeyWindow : NSObject, IASWebAuthenticationPresentationContextProviding
		{
#if __IOS__
			public UIWindow GetPresentationAnchor(ASWebAuthenticationSession session) => UIApplication.SharedApplication.KeyWindow!;
#else
			public NSWindow GetPresentationAnchor(ASWebAuthenticationSession session) => NSApplication.SharedApplication.KeyWindow!;
#endif
		}

		protected virtual IEnumerable<string> GetApplicationCustomSchemes()
		{
			if (NSBundle.MainBundle.InfoDictionary.TryGetValue((NSString)"CFBundleURLTypes", out var o))
			{
				if (o is NSArray a)
				{
					for (nuint i = 0; i < a.Count; i++)
					{
						var urlTypes = a.GetItem<NSDictionary>(i);
						if (urlTypes != null && urlTypes.Any())
						{
							if (urlTypes.TryGetValue((NSString)"CFBundleURLSchemes", out var sc))
							{
								if (sc is NSMutableArray schemes)
								{
									for (nuint j = 0; j < schemes.Count; j++)
									{
										yield return schemes.GetItem<NSString>(j) + ':';
									}

								}
							}
						}
					}
				}
			}
		}
	}
}
