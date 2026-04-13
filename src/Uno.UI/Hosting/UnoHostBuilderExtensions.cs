#nullable enable

using System;

namespace Uno.UI.Hosting;

public static class UnoPlatformHostBuilderExtensions
{
	/// <summary>
	/// Provides an <see cref="Microsoft.UI.Xaml.Application"/> instance to use when starting the app.
	/// </summary>
	/// <remarks>
	/// The parameter is non-generic <c>Func&lt;Application&gt;</c> on purpose. With the previous
	/// generic <c>App&lt;TApplication&gt;(Func&lt;TApplication&gt;)</c> signature, a call like
	/// <c>.App(() =&gt; new App())</c> caused the C# compiler to instantiate <c>Func&lt;App&gt;</c>.
	/// When the inner app runs inside a collectible
	/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, CoreCLR's shared-generic policy
	/// places that instantiation in the default ALC's <c>LoaderAllocator</c>. The resulting
	/// generic-dictionary entry holds a native cross-LA reference into the inner ALC's
	/// <c>App.MethodTable</c>, which keeps the inner LoaderAllocator's reference count above zero
	/// indefinitely and blocks ALC collection.
	///
	/// Using a closed <c>Func&lt;Application&gt;</c> parameter lets the user's lambda bind
	/// covariantly without instantiating any per-app-type generic, so no host-LA pin is created.
	/// The concrete app type is recovered from <c>appBuilder.Method.ReturnType</c>.
	/// </remarks>
	public static IUnoPlatformHostBuilder App(this IUnoPlatformHostBuilder builder, Func<Microsoft.UI.Xaml.Application> appBuilder)
	{
		builder.AppBuilder = appBuilder;
		builder.SetAppType(appBuilder.Method.ReturnType);
		return builder;
	}

	/// <summary>
	/// Provides an action to be executed after the UnoPlatformHost has been initialized, and before the run loop starts.
	/// </summary>
	public static IUnoPlatformHostBuilder AfterInit(this IUnoPlatformHostBuilder builder, Action action)
	{
		builder.AfterInitAction = action;
		return builder;
	}
}
