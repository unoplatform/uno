#nullable enable
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Diagnostics.UI;

/// <summary>
/// Registry for view that can be displayed by the DiagnosticOverlay.
/// </summary>
internal static class DiagnosticViewRegistry
{
	internal static EventHandler<ImmutableList<DiagnosticViewRegistration>>? Added;

	private static ImmutableList<DiagnosticViewRegistration> _registrations = ImmutableList<DiagnosticViewRegistration>.Empty;

	/// <summary>
	/// Gets the list of registered diagnostic providers.
	/// </summary>
	internal static ImmutableList<DiagnosticViewRegistration> Registrations => _registrations;

	/// <summary>
	/// Register a global diagnostic provider that can be displayed on any window.
	/// </summary>
	/// <param name="provider">A diagnostic provider to display.</param>
	public static void Register(IDiagnosticViewProvider provider)
	{
		ImmutableInterlocked.Update(
			ref _registrations,
			static (providers, provider) => providers.Add(provider),
			new DiagnosticViewRegistration(GlobalProviderMode.One, provider));

		Added?.Invoke(null, _registrations);
	}
}

internal record DiagnosticViewRegistration(GlobalProviderMode Mode, IDiagnosticViewProvider Provider);

internal enum GlobalProviderMode
{
	/// <summary>
	/// Diagnostic is being rendered as overlay on each window.
	/// </summary>
	All,

	/// <summary>
	/// Diagnostic is being display on at least one window.
	/// I.e. only the main/first opened but move to the next one if the current window is closed.
	/// </summary>
	One,

	/// <summary>
	/// Only registers the diagnostic provider but does not display it.
	/// </summary>
	OnDemand
}
