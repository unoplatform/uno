#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.WinAppSDKSyncGenerator.AttributeGeneration;

internal class ExperimentalAttributeDescription : IAttributeDescription
{
	public string? TryGenerateCodeFromAttributeData(AttributeData attributeData)
		=> attributeData.AttributeClass?.ToString() == "Windows.Foundation.Metadata.ExperimentalAttribute"
			? "[global::Windows.Foundation.Metadata.Experimental]"
			: null;
}
