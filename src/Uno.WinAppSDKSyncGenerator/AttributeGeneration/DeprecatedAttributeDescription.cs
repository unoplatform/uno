#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.AttributeGeneration;

internal class DeprecatedAttributeDescription : IAttributeDescription
{
	public string? TryGenerateCodeFromAttributeData(AttributeData attributeData)
	{
		if (attributeData.AttributeClass?.ToString() == "Windows.Foundation.Metadata.DeprecatedAttribute")
		{
			return "// This type is deprecated. Consider not implementing it.";
		}

		return null;
	}
}
