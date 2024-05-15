#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.AttributeGeneration;

internal sealed class FlagsAttributeDescription : AttributeDescriptionBase
{
	protected override string? TryGenerateAttributeParameters(AttributeData attributeData)
	{
		// This attribute doesn't have any constructor/named arguments.
		// It was already handled by TryGenerateAttributeParametersCommon in AttributeDescriptionBase.
		// We should never hit this code path, but if we did, just don't handle the attribute.
		return null;
	}

	private protected override bool CanHandle(string fullyQualifiedAttributeName) => fullyQualifiedAttributeName == "System.FlagsAttribute";
}
