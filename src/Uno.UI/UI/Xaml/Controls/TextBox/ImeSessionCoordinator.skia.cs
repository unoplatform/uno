#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
	/// Routes IME composition events to the control owning the native input session.
	/// Platforms with independent native responders register <see cref="IHostScopedImeTextBoxExtension"/>;
	/// other platforms retain a shared extension and active host.
	/// </summary>
	internal sealed class ImeSessionCoordinator
	{
		private static readonly ImeSessionCoordinator _shared = new();
		private static ConditionalWeakTable<IImeSessionHost, ImeSessionCoordinator> _hostSessions = new();
		private static Func<IImeSessionHost, IHostScopedImeTextBoxExtension>? _extensionFactoryForTesting;
		private static bool _initialized;
		private static bool _useHostScopedExtensions;

		private readonly bool _isHostScoped;
		private readonly XamlRoot? _xamlRoot;
		private IImeTextBoxExtension? _extension;
		private IDisposable? _subscriptions;
		private IImeSessionHost? _activeHost;
		private ImeSessionActivation _activeActivation;

		private ImeSessionCoordinator()
		{
		}

		private ImeSessionCoordinator(IHostScopedImeTextBoxExtension extension, XamlRoot? xamlRoot)
		{
			_isHostScoped = true;
			_xamlRoot = xamlRoot;
			_extension = extension;
			_subscriptions = WireExtensionEvents(extension);
		}

		/// <summary>The shared platform IME extension, or null when the platform uses host-scoped sessions.</summary>
		internal static IImeTextBoxExtension? Extension
		{
			get
			{
				EnsureInitialized();
				return _shared._extension;
			}
		}

		/// <summary>The control currently owning the shared IME session.</summary>
		internal static IImeSessionHost? ActiveHost => _shared._activeHost;

		internal static IImeTextBoxExtension? GetExtension(IImeSessionHost host)
		{
			var coordinator = GetCoordinator(host, create: false);
			return coordinator?.IsActiveHost(host) == true ? coordinator._extension : null;
		}

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

			_useHostScopedExtensions = ApiExtensibility.IsRegistered<IHostScopedImeTextBoxExtension>();
			if (_useHostScopedExtensions)
			{
				return;
			}

			if (!ApiExtensibility.CreateInstance<IImeTextBoxExtension>(typeof(TextBox), out var extension))
			{
				typeof(ImeSessionCoordinator).LogDebug()?.Debug("No IME extension registered or registration returned null, IME composition will not be supported.");
				return;
			}

			_shared._extension = extension;
			_shared._subscriptions = _shared.WireExtensionEvents(extension);
		}

		private static ImeSessionCoordinator? GetCoordinator(IImeSessionHost host, bool create)
		{
			EnsureInitialized();
			if (!_useHostScopedExtensions)
			{
				return _shared;
			}

			if (_hostSessions.TryGetValue(host, out var coordinator))
			{
				if (!create || ReferenceEquals(coordinator._xamlRoot, host.XamlRoot))
				{
					return coordinator;
				}

				ReleaseHostSession(host, coordinator);
				return GetCoordinator(host, create: true);
			}

			if (!create)
			{
				return null;
			}

			IHostScopedImeTextBoxExtension? extension;
			if (_extensionFactoryForTesting is { } factory)
			{
				extension = factory(host);
			}
			else if (!ApiExtensibility.CreateInstance(host, out extension))
			{
				return null;
			}

			coordinator = new ImeSessionCoordinator(extension, host.XamlRoot);
			_hostSessions.Add(host, coordinator);
			return coordinator;
		}

		private IImeSessionHost? GetCallbackHost()
			=> _activeHost is { } host && (!_isHostScoped || ReferenceEquals(_xamlRoot, host.XamlRoot))
				? host
				: null;

		private bool IsActiveHost(IImeSessionHost host) => ReferenceEquals(GetCallbackHost(), host);

		private IDisposable WireExtensionEvents(IImeTextBoxExtension extension)
		{
			EventHandler onStarted = (_, _) => InvokeActiveHost(extension, static host => host.OnImeCompositionStarted());
			EventHandler<ImeCompositionEventArgs> onUpdated = (_, e) => InvokeActiveHost(extension, host => host.OnImeCompositionUpdated(e.Text, e.CursorPosition, e.ResolvedLength, e.TextAlreadyApplied));
			EventHandler<ImeCompositionEventArgs> onCompleted = (_, e) => InvokeActiveHost(extension, host => host.OnImeCompositionCompleted(e.Text, e.TextAlreadyApplied));
			EventHandler<ImePartialCompositionEventArgs> onPartiallyCommitted = (_, e) => InvokeActiveHost(extension, host => host.OnImeCompositionPartiallyCommitted(
				e.CommittedText,
				e.CompositionText,
				e.CursorPosition,
				e.ResolvedLength,
				e.TextAlreadyApplied));
			EventHandler<ImeCompositionEventArgs> onCanceled = (_, e) => InvokeActiveHost(extension, host => host.OnImeCompositionCanceled(e.TextAlreadyApplied));
			EventHandler onEnded = (_, _) => InvokeActiveHost(extension, static host => host.OnImeCompositionEnded());
			EventHandler<ImeCandidateWindowBoundsChangedEventArgs> onCandidateWindowBoundsChanged = (_, e) =>
			{
				if (ReferenceEquals(_extension, extension) && GetCallbackHost() is { } host)
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
			});
		}

		private void InvokeActiveHost(IImeTextBoxExtension extension, Action<IImeSessionHost> callback)
		{
			if (!ReferenceEquals(_extension, extension) || GetCallbackHost() is not { } host)
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
			=> GetCoordinator(host, create: true)?.Start(host, activation);

		private void Start(IImeSessionHost host, ImeSessionActivation activation)
		{
			if (ReferenceEquals(_activeHost, host))
			{
				if (_activeActivation != activation)
				{
					try
					{
						_extension?.StartImeSession(host, activation);
						_activeActivation = activation;
					}
					catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
					{
						RecoverFailedSession(host, "Failed to reactivate the IME session.", error);
					}
				}
				return;
			}

			if (_activeHost is not null)
			{
				try
				{
					_extension?.EndImeSession();
				}
				catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
				{
					typeof(ImeSessionCoordinator).LogError()?.Error("Failed to end the previous IME session.", error);
				}
			}

			_activeHost = null;
			_activeActivation = default;
			try
			{
				_extension?.StartImeSession(host, activation);
				_activeHost = host;
				_activeActivation = activation;
			}
			catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
			{
				RecoverFailedSession(host, "Failed to start the IME session.", error);
			}
		}

		/// <summary>
		/// Ends the IME session for <paramref name="host"/> (called on blur). Clears the active host
		/// only if it still points at <paramref name="host"/>, so a focus transition that already
		/// activated a different control is not disturbed.
		/// </summary>
		internal static void EndSession(IImeSessionHost host)
		{
			var coordinator = GetCoordinator(host, create: false);
			if (coordinator?._isHostScoped == true)
			{
				ReleaseHostSession(host, coordinator);
			}
			else
			{
				coordinator?.End(host);
			}
		}

		private static void ReleaseHostSession(IImeSessionHost host, ImeSessionCoordinator coordinator)
		{
			_hostSessions.Remove(host);
			try
			{
				coordinator.End(host);
			}
			finally
			{
				coordinator.Detach();
			}
		}

		private void End(IImeSessionHost host)
		{
			if (!ReferenceEquals(_activeHost, host))
			{
				return;
			}

			try
			{
				_extension?.EndImeSession();
			}
			catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
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
			=> GetCoordinator(host, create: false)?.Update(host, update);

		private void Update(IImeSessionHost host, ImeSessionUpdate update)
		{
			if (update != ImeSessionUpdate.None && IsActiveHost(host))
			{
				try
				{
					_extension?.UpdateImeSession(host, update);
				}
				catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
				{
					RecoverFailedSession(host, "Failed to update the IME session.", error);
				}
			}
		}

		private void RecoverFailedSession(IImeSessionHost host, string message, Exception error)
		{
			typeof(ImeSessionCoordinator).LogError()?.Error(message, error);
			try
			{
				_extension?.EndImeSession();
			}
			catch (Exception cleanupError) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(cleanupError) is null)
			{
				typeof(ImeSessionCoordinator).LogError()?.Error("Failed to clean up the IME session.", cleanupError);
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

		internal static Task<IReadOnlyList<string>> GetLinguisticAlternativesAsync(
			IImeSessionHost host,
			string compositionText,
			CancellationToken cancellationToken)
			=> GetCoordinator(host, create: false)?.GetLinguisticAlternatives(host, compositionText, cancellationToken)
				?? Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

		private async Task<IReadOnlyList<string>> GetLinguisticAlternatives(
			IImeSessionHost host,
			string compositionText,
			CancellationToken cancellationToken)
		{
			if (!IsActiveHost(host) || _extension is not { } extension)
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
			=> GetCoordinator(host, create: false)?.Restart(host);

		private void Restart(IImeSessionHost host)
		{
			if (!IsActiveHost(host))
			{
				return;
			}

			try
			{
				_extension?.EndImeSession();
			}
			catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
			{
				typeof(ImeSessionCoordinator).LogError()?.Error("Failed to restart the IME session while ending it.", error);
			}

			try
			{
				_extension?.StartImeSession(host, _activeActivation);
			}
			catch (Exception error) when (global::Microsoft.UI.Text.RichEditTextDocument.FindFatalException(error) is null)
			{
				RecoverFailedSession(host, "Failed to restart the IME session while starting it.", error);
			}
		}

		/// <summary>
		/// Installs a fake IME extension for testing, wiring its composition events to the active host.
		/// Returns a disposable that unwires it and restores the previous extension.
		/// </summary>
		internal static IDisposable SetExtensionForTesting(IImeTextBoxExtension extension)
		{
			var originalExtension = _shared._extension;
			var originalInitialized = _initialized;
			var originalUseHostScopedExtensions = _useHostScopedExtensions;

			// Mark initialized so a later focus doesn't create/wire the real extension over the fake.
			_shared._extension = extension;
			_initialized = true;
			_useHostScopedExtensions = false;
			var subscription = _shared.WireExtensionEvents(extension);

			return Disposable.Create(() =>
			{
				subscription.Dispose();
				_shared._extension = originalExtension;
				_initialized = originalInitialized;
				_useHostScopedExtensions = originalUseHostScopedExtensions;
			});
		}

		internal static IDisposable SetExtensionFactoryForTesting(Func<IImeSessionHost, IHostScopedImeTextBoxExtension> factory)
		{
			var originalExtension = _shared._extension;
			var originalInitialized = _initialized;
			var originalUseHostScopedExtensions = _useHostScopedExtensions;
			var originalFactory = _extensionFactoryForTesting;
			var originalSessions = _hostSessions;
			var sessions = new ConditionalWeakTable<IImeSessionHost, ImeSessionCoordinator>();

			_shared._extension = null;
			_initialized = true;
			_useHostScopedExtensions = true;
			_extensionFactoryForTesting = factory;
			_hostSessions = sessions;

			return Disposable.Create(() =>
			{
				foreach (var session in sessions)
				{
					session.Value.Detach();
				}

				_shared._extension = originalExtension;
				_initialized = originalInitialized;
				_useHostScopedExtensions = originalUseHostScopedExtensions;
				_extensionFactoryForTesting = originalFactory;
				_hostSessions = originalSessions;
			});
		}

		private void Detach()
		{
			_subscriptions?.Dispose();
			_subscriptions = null;
			_extension = null;
			_activeHost = null;
			_activeActivation = default;
		}
	}
}
