#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.AttributeGeneration;

internal sealed class AttributeDataClassComparer : IEqualityComparer<AttributeData>
{
	public static AttributeDataClassComparer Instance { get; } = new();

	private AttributeDataClassComparer()
	{
	}

	private static string? Coerce(string? attributeClass)
	{
		if (attributeClass == "Windows" + ".UI.Xaml.Markup.ContentPropertyAttribute")
		{
			// For the purpose of attribute equality, we want to consider both attributes as equal.
			return "Microsoft.UI.Xaml.Markup.ContentPropertyAttribute";
		}

		return attributeClass;
	}

	public bool Equals(AttributeData? x, AttributeData? y)
	{
		return Coerce(x?.AttributeClass?.ToString()) == Coerce(y?.AttributeClass?.ToString());
	}

	public int GetHashCode([DisallowNull] AttributeData obj) => Coerce(obj.AttributeClass?.ToString())?.GetHashCode() ?? 0;
}
