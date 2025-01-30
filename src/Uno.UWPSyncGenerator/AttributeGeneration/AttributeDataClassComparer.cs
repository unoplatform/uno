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

	public bool Equals(AttributeData? x, AttributeData? y)
	{
		return x?.AttributeClass?.Name == y?.AttributeClass?.Name;
	}

	public int GetHashCode([DisallowNull] AttributeData obj) => obj.AttributeClass?.Name?.GetHashCode() ?? 0;
}
