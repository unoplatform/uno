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
	internal static EventHandler<IImmutableList<DiagnosticViewRegistration>>? Added;

	private static ImmutableArray<DiagnosticViewRegistration> _registrations = [];

	/// <summary>
	/// Gets the list of registered diagnostic providers.
	/// </summary>
	internal static IImmutableList<DiagnosticViewRegistration> Registrations => _registrations;

	/// <summary>
	/// Register a global diagnostic view that can be displayed on any window.
	/// </summary>
	/// <param name="view">A diagnostic view to display.</param>
	/// <param name="mode">Defines when the registered diagnostic view should be displayed.</param>
	public static void Register(IDiagnosticView view, DiagnosticViewRegistrationMode mode = default)
	{
		ImmutableInterlocked.Update(
			ref _registrations,
			static (providers, provider) => providers.Add(provider),
			new DiagnosticViewRegistration(mode, view));

		Added?.Invoke(null, _registrations);
	}
}

internal sealed record DiagnosticViewRegistration(
	DiagnosticViewRegistrationMode Mode,
	IDiagnosticView View);

public enum DiagnosticViewRegistrationMode
{
	/// <summary>
	/// Diagnostic is being display on at least one window.
	/// I.e. only the main/first opened but move to the next one if the current window is closed.
	/// </summary>
	One, // Default

	/// <summary>
	/// Diagnostic is being rendered as overlay on each window.
	/// </summary>
	All,

	/// <summary>
	/// Only registers the diagnostic provider but does not display it.
	/// </summary>
	OnDemand
}

public enum DiagnosticViewRegistrationPosition
{
	Normal = 0, // Default

	/// <summary>
	/// Register as the first diagnostic view, ensuring it is displayed first.
	/// </summary>
	First = -1,

	/// <summary>
	/// Register as the last diagnostic view, ensuring it is displayed last.
	///	</summary>
	Last = 1,
}
