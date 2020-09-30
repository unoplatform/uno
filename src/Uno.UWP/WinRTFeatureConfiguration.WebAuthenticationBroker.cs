#nullable enable
using System;

namespace Uno
{
	partial class WinRTFeatureConfiguration
	{
		public static class WebAuthenticationBroker
		{
			public static string DefaultCallbackPath { get; set; } = "/authentication-callback";

			/// <summary>
			/// Set this to end the authentication process after this timeout.
			/// </summary>
			public static TimeSpan AuthenticationTimeout { get; set; } = TimeSpan.FromMinutes(5);

			/// <summary>
			/// Set the default return Uri. If not defined (null), the default provider
			/// will try to determine it automatically.
			/// </summary>
			public static Uri? DefaultReturnUri { get; set; } = null;

#if __WASM__

			/// <summary>
			/// This is the initial name of the opened window, when this mode is used.
			/// </summary>
			public static string? WindowTitle { get; set; } = "Sign In";

			/// <summary>
			/// Width of the opened window
			/// </summary>
			public static ushort WindowWidth { get; set; } = 483;

			/// <summary>
			/// Height of the opened window
			/// </summary>
			public static ushort WindowHeight { get; set; } = 600;

			/// <summary>
			/// Set this property if you want to use an &lt;iframe&gt; for
			/// the wasm login. READ THE DOCUMENTATION FOR LIMITATIONS.
			/// </summary>
			/// <remarks>
			/// Using an iframe works only if you control the server and set
			/// appropriate http headers.
			/// </remarks>
			public static string? IFrameHtmlId { get; set; }

#elif __IOS__ || __MACOS__
			/// <summary>
			/// (iOS 13+, MacOS 10.15+) If Ephemeral Web Browser Session should be used.
			/// That means no cookies is preserved between sessions.
			/// </summary>
			public static bool PrefersEphemeralWebBrowserSession { get; set; } = false;
#endif
		}
	}
}
