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

	private static readonly object _gate = new();
	private static Type? _skiaBackendType;
	private static bool _typeResolved;
	private static bool _fontAttempted;
	private static bool _imageDecoderAttempted;
	private static bool _geometryAttempted;
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
			if (Invoke<IImageDecoder>("CreateImageDecoder") is { } decoder)
			{
				ImageDecoder.RegisterDefault(decoder);
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

				// The raw-Skia SKCanvasElement factory (Composition-internal render hook) — still a void reflective
				// call; it self-registers via ApiExtensibility inside the backend.
				Invoke("RegisterSKCanvasElementFactory");
			}
		}
	}

	/// <summary>Reflectively calls a parameterless static factory on the Skia backend and returns its result cast to
	/// the neutral seam interface <typeparamref name="T"/> — so the framework registers the instance through its own
	/// internal registrar, and this backend reaches no framework internal (no InternalsVisibleTo from Drawing). Null
	/// if the backend assembly isn't present (a SkiaSharp-free head) or the call fails.</summary>
	private static T? Invoke<T>(string methodName) where T : class => Invoke(methodName) as T;

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
