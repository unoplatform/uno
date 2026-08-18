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

		// Apply the app-declared drawing registrations before any host runs, so backend + content seams are in
		// place before the host negotiates graphics and records the first frame.
		foreach (var apply in _drawingRegistrations)
		{
			apply();
		}

		// Fill unregistered seams from the SkiaSharp backend (if present), then fail fast if a required seam is empty.
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

	// The framework holds no compile-time reference to any concrete backend. When the app declares no backend/seam,
	// the SkiaSharp backend is lit up by reflection if its assembly is present; a SkiaSharp-free build registers each
	// seam explicitly. Resolved by assembly-qualified name so no assembly reference is required.
	private const string SkiaBackendTypeName = "Uno.UI.Composition.Skia.SkiaBackend, Uno.UI.Composition.Skia";

	// SVG has no core Skia impl: the Svg.Skia renderer ships as the optional Uno.UI.Svg add-in, with the managed
	// engine as the built-in fallback.
	private const string SvgAddInBackendTypeName = "Uno.UI.Svg.SvgBackend, Uno.UI.Svg";
	private const string ManagedSvgRendererTypeName = "Uno.UI.Composition.Drawing.ManagedSvgRenderer, Uno.UI.Composition.Managed";

	// Lottie ships as the optional Uno.UI.Lottie add-in; when referenced it becomes the default, otherwise playback
	// is unavailable.
	private const string SkottieLottieRendererTypeName = "Uno.UI.Lottie.SkottieLottieRenderer, Uno.UI.Lottie";

	private static readonly object _fallbackGate = new();
	private static Type? _skiaBackendType;
	private static bool _skiaBackendTypeResolved;

	// Downward codec-resolve trigger: Uno.UWP's BitmapEncoder sits below Uno.UI and can't reach the codec registry,
	// so it invokes this to lazily light up the Skia codec on first encode when Build() was never called.
	[ModuleInitializer]
	internal static void WireDownwardHooks()
		=> Windows.Graphics.Imaging.BitmapEncoder.EnsureCodec = TryLightUpImageDecoder;

	/// <summary>
	/// Resolves default backend + content seams for any seam the app left unregistered, then throws if a required
	/// seam still has no implementation. Called once from <see cref="Build"/>.
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

	/// <summary>Reflectively calls a parameterless static factory on the Skia backend, cast to the neutral seam
	/// <typeparamref name="T"/>. Null if the backend assembly isn't present or the call fails.</summary>
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

			// NonPublic: the SkiaBackend factories are internal; reflection reaches them without a compile-time dependency.
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
