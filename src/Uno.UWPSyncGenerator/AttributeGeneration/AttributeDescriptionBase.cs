#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.AttributeGeneration;

internal abstract class AttributeDescriptionBase : IAttributeDescription
{
	private protected abstract bool CanHandle(string fullyQualifiedAttributeName);

	public string? TryGenerateCodeFromAttributeData(AttributeData attributeData)
	{
		var attributeName = attributeData.AttributeClass?.ToString();
		if (attributeName is null || !CanHandle(attributeName))
		{
			return null;
		}

		var parameters = TryGenerateAttributeParametersCommon(attributeData);
		if (parameters is null)
		{
			return null;
		}
		else if (parameters.Length == 0)
		{
			return $"[global::{attributeName}]";
		}
		else
		{
			// Special case ContentPropertyAttribute.
			// In WinUI, we get attributeName As Windows.UI.Xaml.Markup.ContentPropertyAttribute,
			// But we only have Microsoft.UI.Xaml.Markup.ContentPropertyAttribute
			if (attributeName == "Windows" + ".UI.Xaml.Markup.ContentPropertyAttribute")
			{
				attributeName = "Microsoft.UI.Xaml.Markup.ContentPropertyAttribute";
			}

			return $"[global::{attributeName}({parameters})]";
		}
	}

	private string? TryGenerateAttributeParametersCommon(AttributeData attributeData)
	{
		if (attributeData.ConstructorArguments.Length == 0 && attributeData.NamedArguments.Length == 0)
		{
			return string.Empty;
		}

		return TryGenerateAttributeParameters(attributeData);
	}

	/// <summary>
	/// Given <see cref="AttributeData"/>, try to generate parameters. If the current instance doesn't handle
	/// the given <paramref name="attributeData"/>, return null.
	/// This method is not called when both ConstructorArguments and NamedArguments are empty.
	/// </summary>
	protected abstract string? TryGenerateAttributeParameters(AttributeData attributeData);
}
