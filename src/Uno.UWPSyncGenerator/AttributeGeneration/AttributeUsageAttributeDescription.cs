#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.AttributeGeneration;

internal class AttributeUsageAttributeDescription : AttributeDescriptionBase
{
	protected override string? TryGenerateAttributeParameters(AttributeData attributeData)
	{
		// The .Single() below is to assert we have a single constructor arguments, ie, AttributeTargets.
		// This adds confidence that we are not ignoring any constructor arguments.
		var attributeTargets = GetAttributeTargetsString((AttributeTargets)attributeData.ConstructorArguments.Single().Value!);

		// For some reason, attribute.ConstructorArguments.Single().Value can be "zero" for some attributes, e.g, OverridableAttribute.
		// From documentation: https://learn.microsoft.com/en-us/uwp/api/windows.foundation.metadata.overridableattribute?view=winrt-16299
		// This is `System.AttributeTargets.InterfaceImpl`, but there is no such member in AttributeTargets, so it's probably not .NET-related.
		// We ignore adding the attribute for this case.
		if (attributeTargets.Length > 1)
		{
			var allowMultiple = false;
			var isInherited = true;
			foreach (var namedArgument in attributeData.NamedArguments)
			{
				if (namedArgument.Key.Equals("AllowMultiple", StringComparison.Ordinal))
				{
					allowMultiple = (bool)namedArgument.Value.Value!;
				}
				else if (namedArgument.Key.Equals("Inherited", StringComparison.Ordinal))
				{
					isInherited = (bool)namedArgument.Value.Value!;
				}
				else
				{
					throw new InvalidOperationException($"Unexpected named argument '{namedArgument.Key}' for 'AttributeUsageAttribute'");
				}
			}

			return $"{attributeTargets}, Inherited = {isInherited.ToString().ToLowerInvariant()}, AllowMultiple = {allowMultiple.ToString().ToLowerInvariant()}";
		}

		return null;
	}

	private static string GetAttributeTargetsString(AttributeTargets valueToConvert)
	{
		var isFirst = true;
		var result = "";
		var values = Enum.GetValues<AttributeTargets>().OrderByDescending(a => (int)a);
		foreach (var value in values)
		{
			if ((value & valueToConvert) == value)
			{
				if (isFirst)
				{
					result = $"global::System.AttributeTargets.{value}";
				}
				else
				{
					result += $" | global::System.AttributeTargets.{value}";
				}

				isFirst = false;
				valueToConvert = (AttributeTargets)(valueToConvert - value);
			}
		}

		if (valueToConvert != 0)
		{
			throw new InvalidOperationException("Something went wrong..");
		}

		return result;
	}

	private protected override bool CanHandle(string fullyQualifiedAttributeName)
		=> fullyQualifiedAttributeName == "System.AttributeUsageAttribute";
}
