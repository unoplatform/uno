#nullable enable

using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Reads <see cref="AssemblyLoadContext"/> unload state. The runtime exposes no public unload-state
/// API, so this reads the private <c>_state</c> field. Verified against .NET 9 and .NET 10: the field
/// exists and <c>0 == Alive</c> (any other value means Unloading/Unloaded). If a future runtime renames
/// the field this resolves to <c>null</c> and <see cref="IsUnloadInitiated"/> returns the caller-supplied
/// fallback, so each call site keeps its own leak-safe default.
/// </summary>
internal static class AlcStateHelper
{
	private static readonly FieldInfo? _alcStateField =
		typeof(AssemblyLoadContext).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);

	/// <summary>
	/// Whether <paramref name="alc"/>'s unload has been initiated (private state != Alive). Returns
	/// <paramref name="valueIfUnknown"/> when the state cannot be read (field absent on a future runtime,
	/// or the read throws).
	/// </summary>
	internal static bool IsUnloadInitiated(AssemblyLoadContext alc, bool valueIfUnknown)
	{
		if (_alcStateField is null)
		{
			return valueIfUnknown;
		}

		try
		{
			return (int)_alcStateField.GetValue(alc)! != 0;
		}
		catch (Exception)
		{
			return valueIfUnknown;
		}
	}
}
