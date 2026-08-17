#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using Uno.Foundation.Logging;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Register-if-absent fallback that lights up the SkiaSharp backend by default <em>when its assembly is present</em>
/// in the app (i.e. SkiaSharp is referenced), located and invoked purely by reflection so the neutral framework keeps
/// no compile-time dependency on SkiaSharp or the Skia backend. This lets an app that upgrades keep rendering without
/// changing its startup, while a deliberately SkiaSharp-free app — which doesn't ship the backend assembly — is
/// unaffected (the lookup finds nothing and this is a no-op).
///
/// The fallback is <strong>per-seam</strong>: each seam (font provider, image decoder, graphics backend) lazily lights
/// up ONLY its own Skia implementation, the first time that specific seam is needed and found empty. So a head that
/// declared a WebGPU renderer (via <see cref="GraphicsRegistry"/>) but no font/image implementors still gets the Skia
/// font + image DEFAULTS, yet never the Skia renderer — and conversely a head that registered a font provider but no
/// renderer gets the Skia renderer without a Skia font provider. An explicit registration always wins.
/// </summary>
internal static class DrawingBackendFallback
{
	private const string SkiaBackendTypeName = "Uno.UI.Composition.Skia.SkiaBackend, Uno.UI.Composition.Skia";

	// SVG has no impl in the core Skia backend: the Svg.Skia renderer ships as the optional Uno.UI.Svg add-in (its
	// heavy dependency tree stays opt-in), and the SkiaSharp-free managed engine is the built-in fallback. Both are
	// reached reflectively by assembly-qualified name so this seam assembly keeps no compile-time dependency on either.
	private const string SvgAddInBackendTypeName = "Uno.UI.Svg.SvgBackend, Uno.UI.Svg";
	private const string ManagedSvgRendererTypeName = "Uno.UI.Composition.Drawing.ManagedSvgRenderer, Uno.UI.Composition.Managed";

	// Wire the downward codec-resolve trigger so Uno.UWP's BitmapEncoder (which sits below this assembly and can't
	// reach here) can lazily light up a codec on first encode instead of failing. Runs on assembly load.
	[System.Runtime.CompilerServices.ModuleInitializer]
	internal static void WireDownwardHooks()
		=> Windows.Graphics.Imaging.BitmapEncoder.EnsureCodec = EnsureImageDecoder;

	private static readonly object _gate = new();
	private static Type? _skiaBackendType;
	private static bool _typeResolved;
	private static bool _fontAttempted;
	private static bool _imageDecoderAttempted;
	private static bool _geometryAttempted;
	private static bool _svgAttempted;
	private static bool _graphicsAttempted;

	/// <summary>Lights up the Skia font provider if that seam is empty (a render-independent content seam).</summary>
	public static void EnsureFontProvider()
	{
		if (Volatile.Read(ref _fontAttempted))
		{
			return;
		}

		lock (_gate)
		{
			if (_fontAttempted)
			{
				return;
			}

			_fontAttempted = true;
			if (Invoke<IFontProvider>("CreateFontProvider") is { } fontProvider)
			{
				FontProvider.RegisterDefault(fontProvider);
			}
		}
	}

	/// <summary>Lights up the Skia image decoder if that seam is empty (a render-independent content seam).</summary>
	public static void EnsureImageDecoder()
	{
		if (Volatile.Read(ref _imageDecoderAttempted))
		{
			return;
		}

		lock (_gate)
		{
			if (_imageDecoderAttempted)
			{
				return;
			}

			_imageDecoderAttempted = true;
			if (Invoke<IImageEncoderDecoder>("CreateImageDecoder") is { } decoder)
			{
				ImageEncoderDecoder.RegisterDefault(decoder);
			}
		}
	}

	/// <summary>Lights up the Skia geometry engine if that seam is empty (a render-independent content seam).</summary>
	public static void EnsureGeometryFactory()
	{
		if (Volatile.Read(ref _geometryAttempted))
		{
			return;
		}

		lock (_gate)
		{
			if (_geometryAttempted)
			{
				return;
			}

			_geometryAttempted = true;
			if (Invoke<IGeometryFactory>("CreateGeometryFactory") is { } geometryFactory)
			{
				GeometryFactory.RegisterDefault(geometryFactory);
			}
		}
	}

	/// <summary>
	/// Lights up the default SVG renderer if that seam is empty: the optional Svg.Skia add-in when referenced,
	/// otherwise the built-in managed engine. An explicit host-builder registration already wins (this only runs when
	/// <see cref="SvgRenderer.Current"/> is still null).
	/// </summary>
	public static void EnsureSvgRenderer()
	{
		if (Volatile.Read(ref _svgAttempted))
		{
			return;
		}

		lock (_gate)
		{
			if (_svgAttempted)
			{
				return;
			}

			_svgAttempted = true;
			var renderer = InvokeStatic<ISvgRenderer>(SvgAddInBackendTypeName, "CreateSvgRenderer")
				?? CreateInstance<ISvgRenderer>(ManagedSvgRendererTypeName);
			if (renderer is { })
			{
				SvgRenderer.RegisterDefault(renderer);
			}
		}
	}

	/// <summary>
	/// Lights up the Skia graphics backend (the matched drawing-factory + renderer pair) if that seam is empty AND no
	/// backend was declared. A head that declared its own backend via <see cref="GraphicsRegistry.Register"/> owns
	/// this seam — even while that backend is still initializing asynchronously (WASM/WebGPU device import) — so the
	/// implicit Skia renderer/factory must never fill the pre-init window and clobber the declared choice.
	/// </summary>
	public static void EnsureGraphicsBackend()
	{
		if (Volatile.Read(ref _graphicsAttempted))
		{
			return;
		}

		lock (_gate)
		{
			if (_graphicsAttempted)
			{
				return;
			}

			_graphicsAttempted = true;

			if (GraphicsRegistry.HasRegisteredBackends)
			{
				return;
			}

			if (Invoke<IGraphicsProvider>("CreateGraphicsProvider") is { } provider)
			{
				GraphicsRegistry.RegisterDefault(new[] { provider });

				if (Invoke<IDrawingFactory>("CreateDefaultRenderer") is { } renderer)
				{
					DrawingRegistration.RegisterDefaultRenderer(renderer);
				}
			}
		}
	}

	/// <summary>Reflectively calls a parameterless static factory on the Skia backend and returns its result cast to
	/// the neutral seam interface <typeparamref name="T"/> — so the framework registers the instance through its own
	/// internal registrar, and this backend reaches no framework internal (no InternalsVisibleTo from Drawing). Null
	/// if the backend assembly isn't present (a SkiaSharp-free head) or the call fails.</summary>
	private static T? Invoke<T>(string methodName) where T : class => Invoke(methodName) as T;

	/// <summary>Reflectively calls a parameterless static factory on an arbitrary assembly-qualified type (for seams
	/// served by an add-in rather than the core Skia backend). Null if the type/assembly isn't present or the call fails.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	private static T? InvokeStatic<T>(string typeName, string methodName) where T : class
	{
		try
		{
			return Type.GetType(typeName, throwOnError: false)
				?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, Type.EmptyTypes)
				?.Invoke(null, null) as T;
		}
		catch (Exception e)
		{
			if (typeof(DrawingBackendFallback).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(DrawingBackendFallback).Log().Debug($"Fallback factory '{typeName}.{methodName}' failed (register this seam explicitly): {e}");
			}

			return null;
		}
	}

	/// <summary>Reflectively constructs an assembly-qualified type via its public parameterless constructor, cast to
	/// the neutral seam interface <typeparamref name="T"/>. Null if the type/assembly isn't present or the call fails.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Best-effort fallback; a trimmed/AOT app registers this seam explicitly.")]
	private static T? CreateInstance<T>(string typeName) where T : class
	{
		try
		{
			var type = Type.GetType(typeName, throwOnError: false);
			return type is null ? null : Activator.CreateInstance(type) as T;
		}
		catch (Exception e)
		{
			if (typeof(DrawingBackendFallback).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(DrawingBackendFallback).Log().Debug($"Fallback type '{typeName}' could not be created (register this seam explicitly): {e}");
			}

			return null;
		}
	}

	/// <summary>Reflectively invokes a parameterless static method on the Skia backend; returns its result (or null).
	/// No-op if the backend assembly isn't present. Caller holds <see cref="_gate"/> and sets the once-flag first.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort fallback; a trimmed/AOT app registers its backend explicitly.")]
	private static object? Invoke(string methodName)
	{
		try
		{
			if (!_typeResolved)
			{
				_skiaBackendType = Type.GetType(SkiaBackendTypeName, throwOnError: false);
				_typeResolved = true;
			}

			// NonPublic: the factories on SkiaBackend are internal (apps register through the host builder, not this
			// backend directly); reflection reaches them across assemblies without a compile-time dependency.
			return _skiaBackendType
				?.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, Type.EmptyTypes)
				?.Invoke(null, null);
		}
		catch (Exception e)
		{
			if (typeof(DrawingBackendFallback).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(DrawingBackendFallback).Log().Debug($"Skia backend fallback '{methodName}' failed (the app should register this seam explicitly): {e}");
			}

			return null;
		}
	}
}
