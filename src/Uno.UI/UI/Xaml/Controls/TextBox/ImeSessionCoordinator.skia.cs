#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
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
		private static ImeSessionActivation _activeActivation;

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
			extension.CompositionStarted += static (_, _) => InvokeActiveHost(static host => host.OnImeCompositionStarted());
			extension.CompositionUpdated += static (_, e) => InvokeActiveHost(host => host.OnImeCompositionUpdated(e.Text, e.CursorPosition, e.ResolvedLength, e.TextAlreadyApplied));
			extension.CompositionCompleted += static (_, e) => InvokeActiveHost(host => host.OnImeCompositionCompleted(e.Text, e.TextAlreadyApplied));
			extension.CompositionPartiallyCommitted += static (_, e) => InvokeActiveHost(host => host.OnImeCompositionPartiallyCommitted(
				e.CommittedText,
				e.CompositionText,
				e.CursorPosition,
				e.ResolvedLength,
				e.TextAlreadyApplied));
			extension.CompositionCanceled += static (_, e) => InvokeActiveHost(host => host.OnImeCompositionCanceled(e.TextAlreadyApplied));
			extension.CompositionEnded += static (_, _) => InvokeActiveHost(static host => host.OnImeCompositionEnded());
			extension.CandidateWindowBoundsChanged += static (_, e) =>
			{
				if (_activeHost is { } host)
				{
					host.OnCandidateWindowBoundsChanged(e.Bounds);
				}
			};
		}

		private static void InvokeActiveHost(Action<IImeSessionHost> callback)
		{
			if (_activeHost is not { } host)
			{
				return;
			}

			try
			{
				callback(host);
			}
			catch (Exception error)
			{
				typeof(ImeSessionCoordinator).LogError()?.Error("A platform IME callback failed.", error);
			}
		}

		/// <summary>Activates an IME session for <paramref name="host"/> (called on focus).</summary>
		internal static void StartSession(IImeSessionHost host)
			=> StartSession(host, new ImeSessionActivation(FocusState.Programmatic, IsSoftwareKeyboardSuppressed: false));

		/// <summary>Activates an IME session for <paramref name="host"/> (called on focus).</summary>
		internal static void StartSession(IImeSessionHost host, ImeSessionActivation activation)
		{
			EnsureInitialized();
			if (ReferenceEquals(_activeHost, host))
			{
				if (_activeActivation != activation)
				{
					_activeActivation = activation;
					_extension?.StartImeSession(host, activation);
				}
				return;
			}

			if (_activeHost is not null)
			{
				try
				{
					_extension?.EndImeSession();
				}
				catch (Exception error)
				{
					typeof(ImeSessionCoordinator).LogError()?.Error("Failed to end the previous IME session.", error);
				}
			}

			_activeHost = host;
			_activeActivation = activation;
			try
			{
				_extension?.StartImeSession(host, activation);
			}
			catch (Exception error)
			{
				_activeHost = null;
				_activeActivation = default;
				typeof(ImeSessionCoordinator).LogError()?.Error("Failed to start the IME session.", error);
			}
		}

		/// <summary>
		/// Ends the IME session for <paramref name="host"/> (called on blur). Clears the active host
		/// only if it still points at <paramref name="host"/>, so a focus transition that already
		/// activated a different control is not disturbed.
		/// </summary>
		internal static void EndSession(IImeSessionHost host)
		{
			if (!ReferenceEquals(_activeHost, host))
			{
				return;
			}

			EnsureInitialized();
			try
			{
				_extension?.EndImeSession();
			}
			catch (Exception error)
			{
				typeof(ImeSessionCoordinator).LogError()?.Error("Failed to end the IME session.", error);
			}
			finally
			{
				if (ReferenceEquals(_activeHost, host))
				{
					_activeHost = null;
					_activeActivation = default;
				}
			}
		}

		internal static void UpdateSession(IImeSessionHost host, ImeSessionUpdate update)
		{
			if (update != ImeSessionUpdate.None && ReferenceEquals(_activeHost, host))
			{
				_extension?.UpdateImeSession(host, update);
			}
		}

		internal static async Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(
			IImeSessionHost host,
			string compositionText,
			CancellationToken cancellationToken)
		{
			EnsureInitialized();
			if (!ReferenceEquals(_activeHost, host) || _extension is not { } extension)
			{
				return Array.Empty<string>();
			}

			try
			{
				return await extension.GetLinguisticAlternativesAsync(compositionText, cancellationToken)
					?? Array.Empty<string>();
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception error)
			{
				typeof(ImeSessionCoordinator).LogError()?.Error("The platform IME failed to get linguistic alternatives.", error);
				return Array.Empty<string>();
			}
		}

		internal static void RestartSession(IImeSessionHost host)
		{
			if (!ReferenceEquals(_activeHost, host))
			{
				return;
			}

			try
			{
				_extension?.EndImeSession();
			}
			catch (Exception error)
			{
				typeof(ImeSessionCoordinator).LogError()?.Error("Failed to restart the IME session while ending it.", error);
			}

			try
			{
				_extension?.StartImeSession(host, _activeActivation);
			}
			catch (Exception error)
			{
				typeof(ImeSessionCoordinator).LogError()?.Error("Failed to restart the IME session while starting it.", error);
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

			EventHandler onStarted = (_, _) => InvokeActiveHost(static host => host.OnImeCompositionStarted());
			EventHandler<ImeCompositionEventArgs> onUpdated = (_, e) => InvokeActiveHost(host => host.OnImeCompositionUpdated(e.Text, e.CursorPosition, e.ResolvedLength, e.TextAlreadyApplied));
			EventHandler<ImeCompositionEventArgs> onCompleted = (_, e) => InvokeActiveHost(host => host.OnImeCompositionCompleted(e.Text, e.TextAlreadyApplied));
			EventHandler<ImePartialCompositionEventArgs> onPartiallyCommitted = (_, e) => InvokeActiveHost(host => host.OnImeCompositionPartiallyCommitted(
				e.CommittedText,
				e.CompositionText,
				e.CursorPosition,
				e.ResolvedLength,
				e.TextAlreadyApplied));
			EventHandler<ImeCompositionEventArgs> onCanceled = (_, e) => InvokeActiveHost(host => host.OnImeCompositionCanceled(e.TextAlreadyApplied));
			EventHandler onEnded = (_, _) => InvokeActiveHost(static host => host.OnImeCompositionEnded());
			EventHandler<ImeCandidateWindowBoundsChangedEventArgs> onCandidateWindowBoundsChanged = (_, e) =>
			{
				if (_activeHost is { } host)
				{
					host.OnCandidateWindowBoundsChanged(e.Bounds);
				}
			};

			extension.CompositionStarted += onStarted;
			extension.CompositionUpdated += onUpdated;
			extension.CompositionCompleted += onCompleted;
			extension.CompositionPartiallyCommitted += onPartiallyCommitted;
			extension.CompositionCanceled += onCanceled;
			extension.CompositionEnded += onEnded;
			extension.CandidateWindowBoundsChanged += onCandidateWindowBoundsChanged;

			return Disposable.Create(() =>
			{
				extension.CompositionStarted -= onStarted;
				extension.CompositionUpdated -= onUpdated;
				extension.CompositionCompleted -= onCompleted;
				extension.CompositionPartiallyCommitted -= onPartiallyCommitted;
				extension.CompositionCanceled -= onCanceled;
				extension.CompositionEnded -= onEnded;
				extension.CandidateWindowBoundsChanged -= onCandidateWindowBoundsChanged;
				_extension = originalExtension;
				_initialized = originalInitialized;
			});
		}
	}
}
