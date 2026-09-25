#nullable enable

using System;
using System.Collections.Generic;
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
	// each default is lit up by reflection if its assembly is present; a SkiaSharp-free build registers each seam
	// explicitly. Resolved by assembly-qualified name so no assembly reference is required. Every backend and add-in
	// assembly has one public static *Backend entry class, and each default comes from the same Create* factory an
	// app registers explicitly, so the two paths can't diverge.
	private const string SkiaBackendTypeName = "Uno.UI.Composition.Skia.SkiaBackend, Uno.UI.Composition.Skia";
	private const string WebGpuBackendTypeName = "Uno.UI.Composition.WebGpu.WebGpuBackend, Uno.UI.Composition.WebGpu";
	private const string ManagedBackendTypeName = "Uno.UI.Composition.Managed.ManagedBackend, Uno.UI.Composition.Managed";

	// Lottie defaults to Skottie, which comes with the Skia backend, else to the SkiaSharp-free managed engine.
	// An app that wants the managed engine calls IUnoPlatformHostBuilder.LottieRenderer, which this light-up leaves alone.
	//
	// SVG has no core Skia impl: the Svg.Skia renderer ships as the optional Uno.UI.Svg add-in, with the managed
	// engine as the built-in fallback. The add-in extends SkiaBackend with its factory (a C# 14 static extension,
	// emitted as a plain static method on the add-in's extension class, which is what is looked up here).
	private const string SvgAddInBackendTypeName = "Uno.UI.Composition.Skia.SkiaBackendSvgExtensions, Uno.UI.Svg";

	// Each factory lookup keeps Type.GetType and GetMethod, both with literal arguments, in one expression:
	// that lets the trimmer (and NativeAOT) keep exactly the factory invoked, and nothing else on the type.
	// NonPublic: SkiaBackend.CreateDefaultRenderer is internal.
	private const BindingFlags FactoryFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

	// Downward codec-resolve trigger: Uno.UWP's BitmapEncoder sits below Uno.UI and can't reach the codec registry,
	// so it invokes this to lazily light up the Skia codec on first encode when Build() was never called.
#pragma warning disable CA2255 // intentional library module initializer
	[ModuleInitializer]
	internal static void WireDownwardHooks()
		=> Windows.Graphics.Imaging.BitmapEncoder.EnsureCodec = TryLightUpImageDecoder;
#pragma warning restore CA2255

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

		// Skia is the default renderer wherever it is referenced: naming the WebGpu feature makes the backend
		// available, but a head opts into it by registering it on the host builder (handled above).
		if (InvokeFactory<Drawing.IGraphicsProvider>(static () => Type.GetType(SkiaBackendTypeName, throwOnError: false)
			?.GetMethod("CreateGraphicsProvider", FactoryFlags, Type.EmptyTypes)) is { } provider)
		{
			Drawing.GraphicsRegistry.RegisterDefault(new[] { provider });
			return;
		}

		// No Skia: a SkiaSharp-free app, where WebGPU is the only renderer there is. Geometry goes to the managed
		// engine, which WebGPU flattens.
		if (InvokeFactory<Drawing.IGraphicsProvider>(static () => Type.GetType(WebGpuBackendTypeName, throwOnError: false)
			?.GetMethod("CreateGraphicsProvider", FactoryFlags, Type.EmptyTypes)) is { } webGpuProvider)
		{
			Drawing.GraphicsRegistry.RegisterDefault(new[] { webGpuProvider });
			if (!Drawing.GeometryFactory.IsRegistered
				&& InvokeFactory<Drawing.IGeometryFactory>(static () => Type.GetType(ManagedBackendTypeName, throwOnError: false)
					?.GetMethod("CreateGeometryFactory", FactoryFlags, Type.EmptyTypes)) is { } managedGeometry)
			{
				Drawing.GeometryFactory.RegisterDefault(managedGeometry);
			}
		}
	}

	private static void TryLightUpFontProvider()
	{
		if (Drawing.FontProvider.IsRegistered)
		{
			return;
		}

		// The managed engine is the fallback for a SkiaSharp-free head. It reads the system fonts, so it needs a
		// bundled default passed in where those cannot be enumerated (iOS, WASM) - such a head registers its own.
		var fontProvider = InvokeFactory<Drawing.IFontProvider>(static () => Type.GetType(SkiaBackendTypeName, throwOnError: false)
				?.GetMethod("CreateFontProvider", FactoryFlags, Type.EmptyTypes))
			?? InvokeFactory<Drawing.IFontProvider>(static () => Type.GetType(ManagedBackendTypeName, throwOnError: false)
				?.GetMethod("CreateFontProvider", FactoryFlags, Type.EmptyTypes));
		if (fontProvider is not null)
		{
			Drawing.FontProvider.RegisterDefault(fontProvider);
		}
	}

	private static void TryLightUpImageDecoder()
	{
		if (Drawing.ImageEncoderDecoder.IsRegistered)
		{
			return;
		}

		var decoder = InvokeFactory<Drawing.IImageEncoderDecoder>(static () => Type.GetType(SkiaBackendTypeName, throwOnError: false)
				?.GetMethod("CreateImageDecoder", FactoryFlags, Type.EmptyTypes))
			?? InvokeFactory<Drawing.IImageEncoderDecoder>(static () => Type.GetType(ManagedBackendTypeName, throwOnError: false)
				?.GetMethod("CreateImageDecoder", FactoryFlags, Type.EmptyTypes));
		if (decoder is not null)
		{
			Drawing.ImageEncoderDecoder.RegisterDefault(decoder);
		}
	}

	private static void TryLightUpGeometryFactory()
	{
		if (Drawing.GeometryFactory.IsRegistered)
		{
			return;
		}

		var geometryFactory = InvokeFactory<Drawing.IGeometryFactory>(static () => Type.GetType(SkiaBackendTypeName, throwOnError: false)
				?.GetMethod("CreateGeometryFactory", FactoryFlags, Type.EmptyTypes))
			?? InvokeFactory<Drawing.IGeometryFactory>(static () => Type.GetType(ManagedBackendTypeName, throwOnError: false)
				?.GetMethod("CreateGeometryFactory", FactoryFlags, Type.EmptyTypes));
		if (geometryFactory is not null)
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

		var renderer = InvokeFactory<Drawing.ISvgRenderer>(static () => Type.GetType(SvgAddInBackendTypeName, throwOnError: false)
				?.GetMethod("CreateSvgRenderer", FactoryFlags, Type.EmptyTypes))
			?? InvokeFactory<Drawing.ISvgRenderer>(static () => Type.GetType(ManagedBackendTypeName, throwOnError: false)
				?.GetMethod("CreateSvgRenderer", FactoryFlags, Type.EmptyTypes));
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

		var renderer = InvokeFactory<Drawing.ILottieRenderer>(static () => Type.GetType(SkiaBackendTypeName, throwOnError: false)
				?.GetMethod("CreateLottieRenderer", FactoryFlags, Type.EmptyTypes))
			?? InvokeFactory<Drawing.ILottieRenderer>(static () => Type.GetType(ManagedBackendTypeName, throwOnError: false)
				?.GetMethod("CreateLottieRenderer", FactoryFlags, Type.EmptyTypes));
		if (renderer is not null)
		{
			Drawing.LottieRenderer.RegisterDefault(renderer);
		}
	}

	/// <summary>Invokes a parameterless static factory or constructor, cast to the neutral seam <typeparamref name="T"/>.
	/// Null if the type/assembly isn't present or the call fails.</summary>
	private static T? InvokeFactory<T>(Func<MethodBase?> resolveFactory) where T : class
	{
		MethodBase? factory = null;
		try
		{
			factory = resolveFactory();
			return factory switch
			{
				ConstructorInfo constructor => constructor.Invoke(null) as T,
				MethodInfo method => method.Invoke(null, null) as T,
				_ => null,
			};
		}
		catch (Exception e)
		{
			LogFallbackFailure(factory is null ? $"{typeof(T).Name} lookup" : $"{factory.DeclaringType?.AssemblyQualifiedName}.{factory.Name}", e);
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
