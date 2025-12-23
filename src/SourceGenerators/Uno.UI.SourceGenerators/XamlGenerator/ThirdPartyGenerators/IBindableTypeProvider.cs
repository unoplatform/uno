#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators;

/// <summary>
/// Provides information about types that should be considered bindable.
/// This interface can be implemented by <see cref="ITypeProvider"/> implementations
/// to indicate which types should have bindable metadata generated.
/// </summary>
internal interface IBindableTypeProvider
{
	/// <summary>
	/// Determines whether a type should be considered bindable and included in the generated metadata.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type should be considered bindable; otherwise, false.</returns>
	bool IsBindableType(INamedTypeSymbol type);
}
