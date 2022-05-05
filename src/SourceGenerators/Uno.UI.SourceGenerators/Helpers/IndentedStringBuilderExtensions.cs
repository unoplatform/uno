#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.Helpers;

internal static class IndentedStringBuilderExtensions
{
	internal static IDisposable GenerateNestingContainers(this IIndentedStringBuilder builder, INamedTypeSymbol? typeSymbol)
	{
		var disposables = new List<IDisposable>();

		while (typeSymbol?.ContainingType != null)
		{
			disposables.Add(builder.BlockInvariant($"partial class {typeSymbol?.ContainingType.Name}"));

			typeSymbol = typeSymbol?.ContainingType;
		}

		return new DisposableAction(() => disposables.ForEach(d => d.Dispose()));
	}
}
