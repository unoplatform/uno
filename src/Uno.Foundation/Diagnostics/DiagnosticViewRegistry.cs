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

	/// <summary>
	/// Removes registrations whose <see cref="IDiagnosticView"/> implementation type
	/// is from a non-default (collectible) <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.
	/// Called during ALC teardown to release delegates that pin the ALC's LoaderAllocator.
	/// </summary>
	internal static void ClearNonDefaultAlcRegistrations()
	{
		var defaultAlc = System.Runtime.Loader.AssemblyLoadContext.Default;
		ImmutableInterlocked.Update(
			ref _registrations,
			static (regs, defAlc) => regs.RemoveAll(r => IsFromNonDefaultAlc(r.View.GetType(), defAlc)),
			defaultAlc);
	}

	private static bool IsFromNonDefaultAlc(Type type, System.Runtime.Loader.AssemblyLoadContext defaultAlc)
	{
		// Check the type itself
		var alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(type.Assembly);
		if (alc is not null && alc != defaultAlc)
		{
			return true;
		}

		// For generic types like DiagnosticView<HotReloadStatusView, Status>,
		// the open generic is in the default ALC but the type arguments may be
		// from a collectible ALC. Check each generic argument.
		if (type.IsGenericType)
		{
			foreach (var arg in type.GetGenericArguments())
			{
				var argAlc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(arg.Assembly);
				if (argAlc is not null && argAlc != defaultAlc)
				{
					return true;
				}
			}
		}

		return false;
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
