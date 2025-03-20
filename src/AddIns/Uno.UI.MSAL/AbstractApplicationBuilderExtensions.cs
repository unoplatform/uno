using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;

namespace Uno.UI.MSAL
{
	public static class AbstractApplicationBuilderExtensions
	{
		/// <summary>
		/// Add required helpers for the current Uno platform.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T WithUnoHelpers<T>(this T builder)
			where T : AbstractApplicationBuilder<T>
		{
#if __ANDROID__
			(builder as PublicClientApplicationBuilder)?.WithParentActivityOrWindow(() => ContextHelper.Current as Android.App.Activity);
#elif __APPLE_UIKIT__
#pragma warning disable CA1422 // Validate platform compatibility
			(builder as PublicClientApplicationBuilder)?.WithParentActivityOrWindow(() => UIKit.UIApplication.SharedApplication?.KeyWindow?.RootViewController);
#pragma warning restore CA1422 // Validate platform compatibility
#elif __WASM__
			builder.WithHttpClientFactory(WasmHttpFactory.Instance);
#endif
			return builder;
		}
	}
}
