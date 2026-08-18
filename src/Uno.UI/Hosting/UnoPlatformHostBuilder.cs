#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Drawing = Uno.UI.Composition.Drawing;

namespace Uno.UI.Hosting;

public class UnoPlatformHostBuilder : IUnoPlatformHostBuilder
{
	private List<Func<IPlatformHostBuilder>> _hostBuilders = new();
	private readonly List<Action> _drawingRegistrations = new();
	private Func<Application>? _appBuilder;
	private Action? _afterInitAction;
	private Type? _appType;

	internal UnoPlatformHostBuilder() { }

	Func<Application>? IUnoPlatformHostBuilder.AppBuilder
	{
		get => _appBuilder;
		set => _appBuilder = value;
	}

	Action? IUnoPlatformHostBuilder.AfterInitAction
	{
		get => _afterInitAction;
		set => _afterInitAction = value;
	}

	void IUnoPlatformHostBuilder.SetAppType(Type appType)
		=> _appType = appType;

	public static UnoPlatformHostBuilder Create()
		=> new();

	void IUnoPlatformHostBuilder.AddDrawingRegistration(Action apply)
		=> _drawingRegistrations.Add(apply);

	public UnoPlatformHost Build()
	{
		if (_appBuilder is null || _appType is null)
		{
			throw new InvalidOperationException($"No app builder delegate was provided via the .App extension method.");
		}

		// Apply the app-declared drawing-backend registrations before any host runs, so the render/drawing backend
		// + content seams are in place before the host negotiates graphics (GraphicsRegistry.Initialize) and the
		// first frame is recorded.
		foreach (var apply in _drawingRegistrations)
		{
			apply();
		}

		// Fill any seam the app left unregistered from the SkiaSharp backend (if present), then fail fast if a required
		// seam is still empty — instead of NRE-ing deep in the first frame.
		EnsureDrawingRegistrationsOrThrow();

		foreach (var hostBuilderFunc in _hostBuilders)
		{
			var hostBuilder = hostBuilderFunc();

			if (hostBuilder.IsSupported)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Using host builder {hostBuilder.GetType()}");
				}

				var host = hostBuilder.Create(_appBuilder, _appType);

				host.AfterInitAction = _afterInitAction;

				return host;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Host builder {hostBuilder.GetType()} is not supported");
				}
			}
		}

		throw new InvalidOperationException($"No platform host could be selected");
	}

	void IUnoPlatformHostBuilder.AddHostBuilder(Func<IPlatformHostBuilder> platformHostBuilder)
		=> _hostBuilders.Add(platformHostBuilder);

	#region Default drawing-backend resolution (composition root)

	// The framework is backend-agnostic and holds no compile-time reference to SkiaSharp or any concrete backend. When
	// the app declares no backend/seam explicitly, the host builder lights up the SkiaSharp backend (renderer + its
	// matched drawing factory, plus the render-independent font/image/geometry content seams) by reflection IF its
	// assembly is present — so an app that just references SkiaSharp keeps rendering with no startup change, while a
	// deliberately SkiaSharp-free app (whose build ships no Skia backend assembly) registers each seam explicitly. This
	// runs once, eagerly, in Build(), and throws right there if a required seam is left with no implementation and no
	// Skia to supply one.
	private const string SkiaBackendTypeName = "Uno.UI.Composition.Skia.SkiaBackend, Uno.UI.Composition.Skia";

	// SVG has no impl in the core Skia backend: the Svg.Skia renderer ships as the optional Uno.UI.Svg add-in, and the
	// SkiaSharp-free managed engine is the built-in fallback. Both are reached by assembly-qualified name.
	private const string SvgAddInBackendTypeName = "Uno.UI.Svg.SvgBackend, Uno.UI.Svg";
	private const string ManagedSvgRendererTypeName = "Uno.UI.Composition.Drawing.ManagedSvgRenderer, Uno.UI.Composition.Managed";

	// Lottie ships as the optional Uno.UI.Lottie add-in (its Skottie dependency stays opt-in). When referenced it
	// becomes the default; without it Lottie playback is simply unavailable (the player shows fallback content).
	private const string SkottieLottieRendererTypeName = "Uno.UI.Lottie.SkottieLottieRenderer, Uno.UI.Lottie";

	private static readonly object _fallbackGate = new();
	private static Type? _skiaBackendType;
	private static bool _skiaBackendTypeResolved;

	// Downward codec-resolve trigger: Uno.UWP's BitmapEncoder sits below Uno.UI and can't reach the codec registry, so
	// it invokes this to lazily light up the Skia codec on first encode when the app never went through Build() (e.g. a
	// standalone encode). The normal path registers the decoder eagerly in Build(). No-op on a SkiaSharp-free head.
	[ModuleInitializer]
	internal static void WireDownwardHooks()
		=> Windows.Graphics.Imaging.BitmapEncoder.EnsureCodec = TryLightUpImageDecoder;

	/// <summary>
	/// Resolves the default drawing backend + content seams for any seam the app left unregistered, then throws if a
	/// required seam still has no implementation. Called once from <see cref="Build"/> after the app's explicit
	/// registrations are applied and before any host negotiates graphics.
	/// </summary>
	private static void EnsureDrawingRegistrationsOrThrow()
	{
		TryLightUpGraphicsBackend();
		TryLightUpFontProvider();
		TryLightUpImageDecoder();
		TryLightUpGeometryFactory();
		TryLightUpSvgRenderer(); // best-effort: the managed engine is the built-in default; SVG is optional.
		TryLightUpLottieRenderer(); // best-effort: the Skottie add-in when referenced; Lottie is optional.

		List<string>? missing = null;
		void Require(bool satisfied, string seam, string register)
		{
			if (!satisfied)
			{
				(missing ??= new()).Add($"  • {seam} — register via {register} on the host builder.");
			}
		}

		Require(Drawing.GraphicsRegistry.HasRegisteredBackends, "graphics backend (renderer)", ".GraphicsBackend(...)");
		Require(Drawing.FontProvider.IsRegistered, "font provider", ".FontProvider(...)");
		Require(Drawing.ImageEncoderDecoder.IsRegistered, "image decoder", ".ImageEncoderDecoder(...)");
		Require(Drawing.GeometryFactory.IsRegistered, "geometry engine", ".GeometryFactory(...)");

		if (missing is { Count: > 0 })
		{
			throw new InvalidOperationException(
				"No drawing backend could be resolved. The SkiaSharp backend (Uno.UI.Composition.Skia) is not present to "
				+ "supply defaults, and the following required drawing seam(s) were not registered on the host builder:\n"
				+ string.Join("\n", missing)
				+ "\nReference SkiaSharp (and Uno's Skia backend) for the built-in defaults, or register each seam explicitly.");
		}
	}

	private static void TryLightUpGraphicsBackend()
	{
		// A head that declared its own backend (e.g. WebGPU) owns this seam — even while it initializes asynchronously —
		// so the implicit Skia renderer/factory must never fill the pre-init window and clobber the declared choice.
		if (Drawing.GraphicsRegistry.HasRegisteredBackends)
		{
			return;
		}

		if (InvokeSkiaFactory<Drawing.IGraphicsProvider>("CreateGraphicsProvider") is { } provider)
		{
			Drawing.GraphicsRegistry.RegisterDefault(new[] { provider });

			if (InvokeSkiaFactory<Drawing.IDrawingFactory>("CreateDefaultRenderer") is { } renderer)
			{
				Drawing.DrawingRegistration.RegisterDefaultRenderer(renderer);
			}
		}
	}

	private static void TryLightUpFontProvider()
	{
		if (!Drawing.FontProvider.IsRegistered && InvokeSkiaFactory<Drawing.IFontProvider>("CreateFontProvider") is { } fontProvider)
		{
			Drawing.FontProvider.RegisterDefault(fontProvider);
		}
	}

	private static void TryLightUpImageDecoder()
	{
		if (!Drawing.ImageEncoderDecoder.IsRegistered && InvokeSkiaFactory<Drawing.IImageEncoderDecoder>("CreateImageDecoder") is { } decoder)
		{
			Drawing.ImageEncoderDecoder.RegisterDefault(decoder);
		}
	}

	private static void TryLightUpGeometryFactory()
	{
		if (!Drawing.GeometryFactory.IsRegistered && InvokeSkiaFactory<Drawing.IGeometryFactory>("CreateGeometryFactory") is { } geometryFactory)
		{
			Drawing.GeometryFactory.RegisterDefault(geometryFactory);
		}
	}

	private static void TryLightUpSvgRenderer()
	{
		if (Drawing.SvgRenderer.Current is not null)
		{
			return;
		}

		var renderer = InvokeStaticFactory<Drawing.ISvgRenderer>(SvgAddInBackendTypeName, "CreateSvgRenderer")
			?? CreateInstanceOf<Drawing.ISvgRenderer>(ManagedSvgRendererTypeName);
		if (renderer is not null)
		{
			Drawing.SvgRenderer.RegisterDefault(renderer);
		}
	}

	private static void TryLightUpLottieRenderer()
	{
		if (Drawing.LottieRenderer.Current is not null)
		{
			return;
		}

		if (InvokeStaticFactory<Drawing.ILottieRenderer>(SkottieLottieRendererTypeName, "CreateLottieRenderer") is { } renderer)
		{
			Drawing.LottieRenderer.RegisterDefault(renderer);
		}
	}

	/// <summary>Reflectively calls a parameterless static factory on the Skia backend and returns its result cast to
	/// the neutral seam interface <typeparamref name="T"/> — so the framework registers the instance through its own
	/// internal registrar, and the backend reaches no framework internal. Null if the backend assembly isn't present (a
	/// SkiaSharp-free head) or the call fails.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort fallback; a trimmed/AOT app registers its backend explicitly.")]
	private static T? InvokeSkiaFactory<T>(string methodName) where T : class
	{
		try
		{
			if (!_skiaBackendTypeResolved)
			{
				lock (_fallbackGate)
				{
					if (!_skiaBackendTypeResolved)
					{
						_skiaBackendType = Type.GetType(SkiaBackendTypeName, throwOnError: false);
						_skiaBackendTypeResolved = true;
					}
				}
			}

			// NonPublic: the factories on SkiaBackend are internal (apps register through the host builder, not this
			// backend directly); reflection reaches them across assemblies without a compile-time dependency.
			return _skiaBackendType
				?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, Type.EmptyTypes)
				?.Invoke(null, null) as T;
		}
		catch (Exception e)
		{
			LogFallbackFailure($"{SkiaBackendTypeName}.{methodName}", e);
			return null;
		}
	}

	/// <summary>Reflectively calls a parameterless static factory on an arbitrary assembly-qualified type (for seams
	/// served by an add-in rather than the core Skia backend). Null if the type/assembly isn't present or the call fails.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	private static T? InvokeStaticFactory<T>(string typeName, string methodName) where T : class
	{
		try
		{
			return Type.GetType(typeName, throwOnError: false)
				?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, Type.EmptyTypes)
				?.Invoke(null, null) as T;
		}
		catch (Exception e)
		{
			LogFallbackFailure($"{typeName}.{methodName}", e);
			return null;
		}
	}

	/// <summary>Reflectively constructs an assembly-qualified type via its public parameterless constructor, cast to
	/// the neutral seam interface <typeparamref name="T"/>. Null if the type/assembly isn't present or the call fails.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	private static T? CreateInstanceOf<T>(string typeName) where T : class
	{
		try
		{
			var type = Type.GetType(typeName, throwOnError: false);
			return type is null ? null : Activator.CreateInstance(type) as T;
		}
		catch (Exception e)
		{
			LogFallbackFailure(typeName, e);
			return null;
		}
	}

	private static void LogFallbackFailure(string what, Exception e)
	{
		if (typeof(UnoPlatformHostBuilder).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(UnoPlatformHostBuilder).Log().Debug($"Default drawing-seam fallback '{what}' failed (register this seam explicitly): {e}");
		}
	}

	#endregion
}
