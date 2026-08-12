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
/// changing its startup, while a deliberately SkiaSharp-free app (managed / WebGPU) — which doesn't ship the backend
/// assembly — is unaffected (the lookup finds nothing and this is a no-op).
///
/// It fires lazily the first time the framework needs a backend and none was registered explicitly, so an app that
/// calls <c>SkiaBackend.Register()</c> / <c>ManagedBackend.Register()</c> (or a WebGPU head that installs its own
/// renderer) always wins — the fallback never clobbers an explicit registration.
/// </summary>
internal static class DrawingBackendFallback
{
	private const string SkiaBackendTypeName = "Uno.UI.Composition.Skia.SkiaBackend, Uno.UI.Composition.Skia";

	private static readonly object _gate = new();
	private static bool _attempted;

	/// <summary>Attempts the reflection-based Skia backend registration exactly once; no-op if already tried.</summary>
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Best-effort fallback; a trimmed/AOT app registers its backend explicitly.")]
	public static void EnsureRegistered()
	{
		if (Volatile.Read(ref _attempted))
		{
			return;
		}

		lock (_gate)
		{
			if (_attempted)
			{
				return;
			}

			// Set before invoking so a re-entrant access from within Register() (which touches DrawingFactory.Current)
			// doesn't recurse.
			_attempted = true;

			try
			{
				// SkiaBackend.Register() installs the whole Skia backend (drawing factory, SkiaFontProvider, image
				// decoder, encoder, default renderer, graphics provider). Reflection-only: if the backend assembly
				// isn't in the app (a SkiaSharp-free head), Type.GetType returns null and this does nothing.
				var type = Type.GetType(SkiaBackendTypeName, throwOnError: false);
				var register = type?.GetMethod("Register", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
				register?.Invoke(null, null);
			}
			catch (Exception e)
			{
				if (typeof(DrawingBackendFallback).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(DrawingBackendFallback).Log().Debug($"Skia backend auto-registration fallback failed (the app should register a drawing backend explicitly): {e}");
				}
			}
		}
	}
}
