using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Uno.UI.MSAL.Extensibility;
using Microsoft.Identity.Client.Extensibility;

#if !WINDOWS
using Uno.Foundation.Extensibility;
#endif

#if UNO_MSAL_RUNTIME_SKIA
[assembly: ApiExtension(typeof(IMsalExtension), typeof(DefaultMsalExtension))]
#endif

namespace Uno.UI.MSAL.Extensibility;

[EditorBrowsable(EditorBrowsableState.Never)]
public partial class DefaultMsalExtension() : IMsalExtension
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DefaultMsalExtension"/> class.
	/// </summary>
	/// <param name="_">An unused parameter reserved for compatibility. This parameter has no effect on the behavior of the
	/// constructor.</param>
	public DefaultMsalExtension(object _) : this() { }

	public static DefaultMsalExtension Instance =>
#if WINDOWS
		new DefaultMsalExtension();
#else
	ApiExtensibility.CreateInstance<DefaultMsalExtension>(typeof(DefaultMsalExtension), out var instance) ? instance : new DefaultMsalExtension();
#endif

	public T InitializeAbstractApplicationBuilder<T>(T builder) where T : AbstractApplicationBuilder<T>
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

	public AcquireTokenInteractiveParameterBuilder InitializeAcquireTokenInteractiveParameterBuilder(AcquireTokenInteractiveParameterBuilder builder)
	{
#if __WASM__
		builder.WithCustomWebUi(WasmWebUi.Instance);
#endif

		return builder;
	}
}
