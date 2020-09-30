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
#if __ANDROID__ && !NET6_0_OR_GREATER
			(builder as PublicClientApplicationBuilder)?.WithParentActivityOrWindow(() => ContextHelper.Current as Android.App.Activity);
#elif __IOS__ && !NET6_0_OR_GREATER
			(builder as PublicClientApplicationBuilder)?.WithParentActivityOrWindow(() => Windows.UI.Xaml.Window.Current.Content.Window.RootViewController);
#elif __WASM__
			builder.WithHttpClientFactory(WasmHttpFactory.Instance);
#endif
			return builder;
		}
	}
}
