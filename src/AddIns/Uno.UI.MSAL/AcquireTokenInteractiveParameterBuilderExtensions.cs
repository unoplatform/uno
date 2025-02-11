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
#endif
			return builder;
		}
	}
}
