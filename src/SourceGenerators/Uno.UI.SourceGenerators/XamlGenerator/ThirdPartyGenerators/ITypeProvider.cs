#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators;

internal interface ITypeProvider
{
	ITypeSymbol? TryGetType(ITypeSymbol symbol, string memberName);
}
