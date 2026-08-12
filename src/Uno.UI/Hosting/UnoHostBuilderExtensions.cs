#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Hosting;

public static class UnoPlatformHostBuilderExtensions
{
	/// <summary>
	/// Registers the app's graphics backend — the render backend and its base drawing/geometry factory, which are
	/// one unit (a renderer without its drawing factory, or vice-versa, is meaningless). The render backend
	/// announces the context kinds it supports (<see cref="IGraphicsProvider.PreferredContexts"/>); the framework
	/// creates a matching context internally (synchronously or asynchronously) and hands it to the backend — the app
	/// never wires a context factory. The drawing factory produces the neutral geometry the render backend consumes.
	/// </summary>
	public static IUnoPlatformHostBuilder GraphicsBackend(this IUnoPlatformHostBuilder builder, IGraphicsProvider renderBackend, IDrawingFactory drawingBackend)
	{
		ArgumentNullException.ThrowIfNull(renderBackend);
		ArgumentNullException.ThrowIfNull(drawingBackend);
		builder.AddDrawingRegistration(() =>
		{
			// Register the base drawing factory first so it exists before the render backend's context is negotiated
			// (the backend decorates it — e.g. WebGPU wraps it for GPU-resident images while delegating geometry).
			DrawingFactory.Register(drawingBackend);
			GraphicsRegistry.Register(new[] { renderBackend });
		});
		return builder;
	}

	/// <summary>
	/// Registers a self-contained graphics backend whose render backend produces its own drawing factory (e.g. the
	/// Skia backend), so no separate base factory is supplied. See <see cref="GraphicsBackend(IUnoPlatformHostBuilder, IGraphicsProvider, IDrawingFactory)"/>.
	/// </summary>
	public static IUnoPlatformHostBuilder GraphicsBackend(this IUnoPlatformHostBuilder builder, IGraphicsProvider renderBackend)
	{
		ArgumentNullException.ThrowIfNull(renderBackend);
		builder.AddDrawingRegistration(() => GraphicsRegistry.Register(new[] { renderBackend }));
		return builder;
	}

	/// <summary>
	/// Registers the font resolver — a render-independent content seam (family/style/bytes/codepoint → glyph
	/// outlines). Independent of the graphics backend, like <see cref="ImageDecoder"/>.
	/// </summary>
	public static IUnoPlatformHostBuilder FontProvider(this IUnoPlatformHostBuilder builder, IFontProvider provider)
	{
		ArgumentNullException.ThrowIfNull(provider);
		builder.AddDrawingRegistration(() => Uno.UI.Composition.Drawing.FontProvider.Current = provider);
		return builder;
	}

	/// <summary>
	/// Registers the image decoder — a render-independent content seam (encoded bytes → neutral pixels). Independent
	/// of the graphics backend, like <see cref="FontProvider"/>.
	/// </summary>
	public static IUnoPlatformHostBuilder ImageDecoder(this IUnoPlatformHostBuilder builder, IImageDecoder decoder)
	{
		ArgumentNullException.ThrowIfNull(decoder);
		builder.AddDrawingRegistration(() => Uno.UI.Composition.Drawing.ImageDecoder.Current = decoder);
		return builder;
	}

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
	///
	/// The concrete app type is deliberately *not* recovered from <c>appBuilder.Method.ReturnType</c>:
	/// for a lambda that yields the delegate's declared return type rather than the concrete one, and
	/// retrieving a <c>MethodInfo</c> for a compiler-generated delegate is not supported under NativeAOT.
	/// No platform host builder consumes the app type.
	/// </remarks>
	public static IUnoPlatformHostBuilder App(this IUnoPlatformHostBuilder builder, Func<Microsoft.UI.Xaml.Application> appBuilder)
	{
		builder.AppBuilder = appBuilder;
		builder.SetAppType(typeof(Microsoft.UI.Xaml.Application));
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
