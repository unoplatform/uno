#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.AttributeGeneration;

internal sealed class ContentPropertyAttributeDescription : AttributeDescriptionBase
{
	protected override string? TryGenerateAttributeParameters(AttributeData attributeData)
	{
		if (attributeData.ConstructorArguments.IsEmpty && !attributeData.NamedArguments.IsEmpty)
		{
			return $"Name = \"{(string)attributeData.NamedArguments.Single().Value.Value!}\"";
		}

		return null;
	}

	private protected override bool CanHandle(string fullyQualifiedAttributeName)
	{
		return fullyQualifiedAttributeName is "Windows" + ".UI.Xaml.Markup.ContentPropertyAttribute" or "Microsoft.UI.Xaml.Markup.ContentPropertyAttribute";
	}
}
