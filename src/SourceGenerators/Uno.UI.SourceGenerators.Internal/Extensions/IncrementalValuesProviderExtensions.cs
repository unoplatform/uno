#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.Internal.Extensions;

internal static class IncrementalValuesProviderExtensions
{
	public static IncrementalValuesProvider<ImmutableArray<TValue>> GroupBy<TKey, TValue>(
		this IncrementalValuesProvider<TValue> source,
		Func<TValue, TKey> valueToKeyTransform,
		IEqualityComparer<TKey> comparer)
	{
		return source.Collect().SelectMany((values, _) =>
		{
			Dictionary<TKey, ImmutableArray<TValue>.Builder> map = new(comparer);

			foreach (var value in values)
			{
				var key = valueToKeyTransform(value);
				if (!map.TryGetValue(key, out ImmutableArray<TValue>.Builder builder))
				{
					builder = ImmutableArray.CreateBuilder<TValue>();

					map.Add(key, builder);
				}

				builder.Add(value);
			}

			var result = ImmutableArray.CreateBuilder<ImmutableArray<TValue>>();

			foreach (var kvp in map)
			{
				result.Add(kvp.Value.ToImmutable());
			}

			return result;
		});
	}

}
