#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Uno.UI.Helpers;

/// <summary>
/// Creates view instances honoring the current hot-reload replacement type, preferring the
/// bindable metadata provider's generated activator when one is registered.
/// </summary>
/// <remarks>
/// Kept out of <see cref="TypeMappings"/> on purpose: that file is linked into
/// <c>Uno.UI.Toolkit.Windows</c>, where <c>Uno.UI.DataBinding</c> does not exist.
/// </remarks>
internal static class TypeActivator
{
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
	internal static object CreateInstance([DynamicallyAccessedMembers(TypeMappings.TypeRequirements)] Type type)
	{
		var replacementType = type.GetReplacementType();

		// Validated lookup: a full-name match from another AssemblyLoadContext would otherwise
		// instantiate the other context's identically-named type.
		if (Uno.UI.DataBinding.BindingPropertyHelper.GetValidatedBindableType(replacementType)?.CreateInstance() is { } factory)
		{
			return factory();
		}

		return Activator.CreateInstance(replacementType)!;
	}
}
