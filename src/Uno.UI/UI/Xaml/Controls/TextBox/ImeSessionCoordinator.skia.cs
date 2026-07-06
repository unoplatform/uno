#nullable enable

using System;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls.Extensions;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Shared, control-agnostic coordinator for IME composition sessions on Skia. It owns the single
	/// platform <see cref="IImeTextBoxExtension"/> (a per-platform singleton) and the currently-active
	/// <see cref="IImeSessionHost"/>, routing the OS composition events to whichever control
	/// (<see cref="TextBox"/> or RichEditBox) currently holds the session. Both controls
	/// activate/deactivate through here on focus/blur, so the one global OS IME is arbitrated by a
	/// single active-host reference.
	/// </summary>
	internal static class ImeSessionCoordinator
	{
		private static IImeTextBoxExtension? _extension;
		private static bool _initialized;
		private static IImeSessionHost? _activeHost;

		/// <summary>The active platform IME extension, or null when none is registered.</summary>
		internal static IImeTextBoxExtension? Extension
		{
			get
			{
				EnsureInitialized();
				return _extension;
			}
		}

		/// <summary>The control currently owning the IME session, or null when none is focused.</summary>
		internal static IImeSessionHost? ActiveHost => _activeHost;

		/// <summary>
		/// Creates and wires the platform IME extension. Idempotent — safe to call from any control's
		/// static initializer as well as lazily on first use.
		/// </summary>
		internal static void Initialize() => EnsureInitialized();

		private static void EnsureInitialized()
		{
			if (_initialized)
			{
				return;
			}

			_initialized = true;

			if (!ApiExtensibility.CreateInstance<IImeTextBoxExtension>(typeof(TextBox), out var extension))
			{
				typeof(ImeSessionCoordinator).LogDebug()?.Debug("No IME extension registered or registration returned null, IME composition will not be supported.");
				return;
			}

			WireExtensionEvents(extension);
			_extension = extension;
		}

		private static void WireExtensionEvents(IImeTextBoxExtension extension)
		{
			extension.CompositionStarted += static (_, _) => _activeHost?.OnImeCompositionStarted();
			extension.CompositionUpdated += static (_, e) => _activeHost?.OnImeCompositionUpdated(e.Text, e.CursorPosition, e.ResolvedLength, e.TextAlreadyApplied);
			extension.CompositionCompleted += static (_, e) => _activeHost?.OnImeCompositionCompleted(e.Text, e.TextAlreadyApplied);
			extension.CompositionEnded += static (_, _) => _activeHost?.OnImeCompositionEnded();
		}

		/// <summary>Activates an IME session for <paramref name="host"/> (called on focus).</summary>
		internal static void StartSession(IImeSessionHost host)
		{
			EnsureInitialized();
			_activeHost = host;
			_extension?.StartImeSession(host);
		}

		/// <summary>
		/// Ends the IME session for <paramref name="host"/> (called on blur). Clears the active host
		/// only if it still points at <paramref name="host"/>, so a focus transition that already
		/// activated a different control is not disturbed.
		/// </summary>
		internal static void EndSession(IImeSessionHost host)
		{
			EnsureInitialized();
			_extension?.EndImeSession();
			if (ReferenceEquals(_activeHost, host))
			{
				_activeHost = null;
			}
		}

		/// <summary>
		/// Installs a fake IME extension for testing, wiring its composition events to the active host.
		/// Returns a disposable that unwires it and restores the previous extension.
		/// </summary>
		internal static IDisposable SetExtensionForTesting(IImeTextBoxExtension extension)
		{
			var originalExtension = _extension;
			var originalInitialized = _initialized;

			// Mark initialized so a later focus doesn't create/wire the real extension over the fake.
			_extension = extension;
			_initialized = true;

			EventHandler onStarted = (_, _) => _activeHost?.OnImeCompositionStarted();
			EventHandler<ImeCompositionEventArgs> onUpdated = (_, e) => _activeHost?.OnImeCompositionUpdated(e.Text, e.CursorPosition, e.ResolvedLength, e.TextAlreadyApplied);
			EventHandler<ImeCompositionEventArgs> onCompleted = (_, e) => _activeHost?.OnImeCompositionCompleted(e.Text, e.TextAlreadyApplied);
			EventHandler onEnded = (_, _) => _activeHost?.OnImeCompositionEnded();

			extension.CompositionStarted += onStarted;
			extension.CompositionUpdated += onUpdated;
			extension.CompositionCompleted += onCompleted;
			extension.CompositionEnded += onEnded;

			return Disposable.Create(() =>
			{
				extension.CompositionStarted -= onStarted;
				extension.CompositionUpdated -= onUpdated;
				extension.CompositionCompleted -= onCompleted;
				extension.CompositionEnded -= onEnded;
				_extension = originalExtension;
				_initialized = originalInitialized;
			});
		}
	}
}
