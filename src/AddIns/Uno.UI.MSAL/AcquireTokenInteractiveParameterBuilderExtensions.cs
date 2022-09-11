using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace Uno.UI.MSAL
{
	public static class AcquireTokenInteractiveParameterBuilderExtensions
	{
		/// <summary>
		/// Add required helpers for the current Uno platform.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static AcquireTokenInteractiveParameterBuilder WithUnoHelpers(this AcquireTokenInteractiveParameterBuilder builder)
		{
#if __WASM__
			builder.WithCustomWebUi(WasmWebUi.Instance);
#elif __MACOS__
#if NET6_0_OR_GREATER
			// WithUnoHelpers is not yet supported for macOS on .NET 6
			// builder.WithParentActivityOrWindow(Windows.UI.Xaml.Window.Current.Content.Window);
#else
			builder.WithParentActivityOrWindow(Windows.UI.Xaml.Window.Current.Content.Window);
#endif
#endif
			return builder;
		}
	}
}
